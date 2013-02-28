using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EtlGate.Core
{
	public class CsvReader
	{
		private readonly IStreamTokenizer _streamTokenizer;

		public CsvReader(IStreamTokenizer streamTokenizer)
		{
			_streamTokenizer = streamTokenizer;
		}

		private static void AddFieldValue(Dictionary<string, string> row, ParseContext parseContext)
		{
			var key = "Field" + parseContext.FieldNumber;
			AddFieldValue(row, parseContext, key);
		}

		private static void AddFieldValue(Dictionary<string, string> row, ParseContext parseContext, string key)
		{
			var value = parseContext.Capture.ToString();
			row.Add(key, value);
			if (parseContext.HasHeaderRow && parseContext.HeaderRow != null)
			{
				string headerKey;
				if (parseContext.HeaderRow.TryGetValue("Field" + parseContext.FieldNumber, out headerKey))
				{
					row.Add(headerKey, value);
				}
			}
			parseContext.Capture.Length = 0;
		}

		private static bool ContinueReadRecordSeparator(Token token, Dictionary<string, string> row, ParseContext parseContext)
		{
			if (token.TokenType != TokenType.Special)
			{
				parseContext.Capture.Append(parseContext.RecordSeparator.Substring(0, parseContext.RecordSeparatorIndex));
				if (parseContext.ReadingQuotedField)
				{
					ThrowUnescapedQuoteException(parseContext);
				}
				parseContext.Handle = ReadUnquotedField;
				return parseContext.Handle(token, row, parseContext);
			}
			if (token.Value[0] != parseContext.RecordSeparator[parseContext.RecordSeparatorIndex])
			{
				parseContext.Capture.Append(parseContext.RecordSeparator.Substring(0, parseContext.RecordSeparatorIndex));
				if (parseContext.ReadingQuotedField)
				{
					ThrowUnescapedQuoteException(parseContext);
				}

				if (token.Value[0] == ',')
				{
					parseContext.Handle = ReadFieldSeparator;
				}
				else
				{
					parseContext.Handle = ReadUnquotedField;
				}
				return parseContext.Handle(token, row, parseContext);
			}
			parseContext.RecordSeparatorIndex++;
			if (parseContext.RecordSeparatorIndex == parseContext.RecordSeparator.Length)
			{
				// reached end of record separator
				AddFieldValue(row, parseContext);
				parseContext.FieldNumber = 0;
				parseContext.Handle = StartReadField;
				if (parseContext.HasHeaderRow && parseContext.ReadingHeaderRow)
				{
					parseContext.ReadingHeaderRow = false;
					parseContext.HeaderRow = new Dictionary<string, string>(row);
					row.Clear();
					return false;
				}
				parseContext.RecordNumber++;
				return true;
			}
			parseContext.Handle = ContinueReadRecordSeparator;
			return false;
		}

		private static bool ReadEscapedQuoteOrEndQuotedField(Token token, Dictionary<string, string> row, ParseContext parseContext)
		{
			switch (token.TokenType)
			{
				case TokenType.Data:
					ThrowUnescapedQuoteException(parseContext);
					break;
				case TokenType.Special:
					switch (token.Value[0])
					{
						case '"':
							parseContext.Capture.Append('"');
							parseContext.Handle = ReadQuotedField;
							break;
						case ',': // end of field, more fields follow
							return ReadFieldSeparator(token, row, parseContext);
						default:
							if (token.Value[0] == parseContext.RecordSeparator[0])
							{
								// end of field, no records follow
								return StartReadRecordSeparator(token, row, parseContext);
							}
							ThrowUnescapedQuoteException(parseContext);
							break;
					}
					break;
			}
			return false;
		}

		private static bool ReadFieldSeparator(Token token, Dictionary<string, string> row, ParseContext parseContext)
		{
			if (token.TokenType == TokenType.Special && token.Value[0] == ',')
			{
				AddFieldValue(row, parseContext);
				parseContext.Handle = StartReadField;
				return false;
			}

			throw new ParseException("Expected ',' but received '" + token.Value + "' on line " + parseContext.RecordNumber + " field " + parseContext.FieldNumber);
		}

		public IEnumerable<Dictionary<string, string>> ReadFrom(Stream stream, string recordSeparator = "\r\n", bool hasHeaderRow = false)
		{
			if (stream == null)
			{
				throw new ArgumentException();
			}

			if (recordSeparator == null)
			{
				recordSeparator = "";
			}

			var row = new Dictionary<string, string>();
			var parseContext = new ParseContext
				                   {
					                   HeaderRow = new Dictionary<string, string>(),
					                   HasHeaderRow = hasHeaderRow,
					                   RecordSeparator = recordSeparator,
					                   ReadingHeaderRow = hasHeaderRow,
					                   Handle = StartReadField,
					                   RecordNumber = 1,
					                   Capture = new StringBuilder()
				                   };
			var specials = (",\"" + recordSeparator).Distinct().ToArray();
			foreach (var token in _streamTokenizer.Tokenize(stream, specials))
			{
				var yieldIt = parseContext.Handle(token, row, parseContext);
				if (yieldIt)
				{
					yield return row;
					row = new Dictionary<string, string>();
					parseContext.Capture = new StringBuilder();
				}
			}
			if (parseContext.Handle == ReadQuotedField)
			{
				throw new ParseException("Quoted field does not have a close quote on line " + parseContext.RecordNumber + " field " + parseContext.FieldNumber);
			}
			if (parseContext.Handle == ContinueReadRecordSeparator)
			{
				parseContext.Capture.Append(recordSeparator.Substring(0, parseContext.RecordSeparatorIndex));
			}
			if (parseContext.Capture.Length > 0 ||
			    parseContext.FieldNumber > 0 && (parseContext.Handle == StartReadField || parseContext.Handle == ReadEscapedQuoteOrEndQuotedField))
			{
				if (parseContext.Handle == StartReadField)
				{
					parseContext.FieldNumber++;
				}
				AddFieldValue(row, parseContext);
			}
			if (row.Count > 0)
			{
				yield return row;
			}
		}

		private static bool ReadQuotedField(Token token, Dictionary<string, string> row, ParseContext parseContext)
		{
			switch (token.TokenType)
			{
				case TokenType.Data:
					parseContext.Capture.Append(token.Value);
					break;
				case TokenType.Special:
					if (token.Value[0] != '"')
					{
						// treat it like data
						parseContext.Capture.Append(token.Value);
					}
					else
					{
						parseContext.Handle = ReadEscapedQuoteOrEndQuotedField;
					}
					break;
			}
			return false;
		}

		private static bool ReadUnquotedField(Token token, Dictionary<string, string> row, ParseContext parseContext)
		{
			switch (token.TokenType)
			{
				case TokenType.Data:
					parseContext.Capture.Append(token.Value);
					break;
				case TokenType.Special:
					if (token.Value[0] == ',')
					{
						return ReadFieldSeparator(token, row, parseContext);
					}
					if (token.Value[0] == parseContext.RecordSeparator[0])
					{
						return StartReadRecordSeparator(token, row, parseContext);
					}
					parseContext.Capture.Append(token.Value);
					break;
			}
			return false;
		}

		private static bool StartReadField(Token token, Dictionary<string, string> row, ParseContext parseContext)
		{
			parseContext.Capture = new StringBuilder();
			parseContext.FieldNumber++;
			if (token.TokenType == TokenType.Special && token.Value[0] == '"')
			{
				parseContext.ReadingQuotedField = true;
				parseContext.Handle = ReadQuotedField;
			}
			else
			{
				parseContext.ReadingQuotedField = false;
				parseContext.Handle = ReadUnquotedField;
				return ReadUnquotedField(token, row, parseContext);
			}
			return false;
		}

		private static bool StartReadRecordSeparator(Token token, Dictionary<string, string> row, ParseContext parseContext)
		{
			parseContext.RecordSeparatorIndex = 0;
			return ContinueReadRecordSeparator(token, row, parseContext);
		}

		private static void ThrowUnescapedQuoteException(ParseContext parseContext)
		{
			throw new ParseException("Unescaped '\"' on line " + parseContext.RecordNumber + " field " + parseContext.FieldNumber);
		}
	}

	public class ParseContext
	{
		public StringBuilder Capture { get; set; }
		public int FieldNumber { get; set; }
		public Func<Token, Dictionary<string, string>, ParseContext, bool> Handle { get; set; }
		public bool HasHeaderRow { get; set; }
		public Dictionary<string, string> HeaderRow { get; set; }
		public bool ReadingHeaderRow { get; set; }
		public bool ReadingQuotedField { get; set; }
		public int RecordNumber { get; set; }
		public string RecordSeparator { get; set; }
		public int RecordSeparatorIndex { get; set; }
	}
}