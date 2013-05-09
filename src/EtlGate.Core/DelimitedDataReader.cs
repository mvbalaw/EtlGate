using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EtlGate.Core
{
	public interface IDelimitedDataReader
	{
		IEnumerable<Dictionary<string, string>> ReadFrom(Stream stream, string fieldSeparator = ",", string recordSeparator = "\r\n", bool supportQuotedFields = true, bool hasHeaderRow = false);
	}

	public class DelimitedDataReader : IDelimitedDataReader
	{
		private readonly IStreamTokenizer _streamTokenizer;

		public DelimitedDataReader(IStreamTokenizer streamTokenizer)
		{
			_streamTokenizer = streamTokenizer;
		}

		public IEnumerable<Dictionary<string, string>> ReadFrom(Stream stream, string fieldSeparator = ",", string recordSeparator = "\r\n", bool supportQuotedFields = true, bool hasHeaderRow = false)
		{
			if (stream == null)
			{
				throw new ArgumentException();
			}

			if (fieldSeparator == null)
			{
				fieldSeparator = "";
			}

			if (recordSeparator == null)
			{
				recordSeparator = "";
			}

			if (fieldSeparator == recordSeparator)
			{
				throw new ParseException("field separator and record separator must be different.");
			}

			var row = new Dictionary<string, string>();
			var parseContext = new ParseContext
				                   {
					                   HeaderRow = new Dictionary<string, string>(),
					                   HasHeaderRow = hasHeaderRow,
					                   RecordSeparator = recordSeparator,
					                   FieldSeparator = fieldSeparator,
					                   ReadingHeaderRow = hasHeaderRow,
					                   Handle = StartReadField,
					                   RecordNumber = 1,
					                   SupportQuotedFields = supportQuotedFields,
					                   Capture = new StringBuilder()
				                   };
			var specialTokens = new List<string>();
			if (!String.IsNullOrEmpty(fieldSeparator))
			{
				specialTokens.Add(fieldSeparator);
			}
			if (!String.IsNullOrEmpty(recordSeparator))
			{
				specialTokens.Add(recordSeparator);
			}
			if (supportQuotedFields)
			{
				specialTokens.Add("\"");
			}
			var specialChars = (fieldSeparator + recordSeparator + (supportQuotedFields ? "\"" : "")).Distinct().ToArray();
			foreach (var token in _streamTokenizer.Tokenize(stream, specialChars))
			{
				var yieldIt = parseContext.Handle(token, row, parseContext);
				if (yieldIt)
				{
					yield return row;
					row = new Dictionary<string, string>();
					parseContext.Capture.Clear();
				}
			}
			if (parseContext.Handle == ReadQuotedField)
			{
				throw new ParseException("Quoted field does not have a close quote on line " + parseContext.RecordNumber + " field " + parseContext.FieldNumber);
			}
			if (parseContext.ReadingQuotedField && parseContext.Handle == ContinueCollectSeparator)
			{
				ThrowUnescapedQuoteException(parseContext);
			}
			if (parseContext.Capture.Length > 0 ||
			    parseContext.FieldNumber > 0 &&
			    (parseContext.Handle == StartReadField || parseContext.Handle == ReadEscapedQuoteOrEndQuotedField))
			{
				if (parseContext.Handle == StartReadField)
				{
					parseContext.IncrementFieldNumber();
				}
				AddFieldValue(row, parseContext);
			}
			if (row.Count > 0)
			{
				yield return row;
			}
		}

		private static void AddFieldValue(Dictionary<string, string> row, ParseContext parseContext)
		{
			var key = parseContext.FieldNumberNames[parseContext.FieldNumber - 1];
			AddFieldValue(row, parseContext, key);
		}

		private static void AddFieldValue(Dictionary<string, string> row, ParseContext parseContext, string key)
		{
			var value = parseContext.Capture.ToString();
			row.Add(key, value);
			if (parseContext.HasHeaderRow && parseContext.HeaderRow != null)
			{
				string headerKey;
				var fieldNumber = parseContext.FieldNumberNames[parseContext.FieldNumber - 1];
				if (parseContext.HeaderRow.TryGetValue(fieldNumber, out headerKey))
				{
					row.Add(headerKey, value);
				}
			}
			parseContext.Capture.Clear();
		}

		private static bool ContinueCollectSeparator(Token token, Dictionary<string, string> row, ParseContext parseContext)
		{
			parseContext.Capture.Append(token.Value);
			var capture = parseContext.Capture.ToString();
			var potentialSeparator = capture.Substring(parseContext.Capture.Length - parseContext.SeparatorLength);
			var fieldSeparatorIndex = parseContext.FieldSeparator.Length > 0 ? potentialSeparator.IndexOf(parseContext.FieldSeparator) : -1;
			var recordSeparatorIndex = parseContext.RecordSeparator.Length > 0 ? potentialSeparator.IndexOf(parseContext.RecordSeparator) : -1;
			if (fieldSeparatorIndex != -1 || recordSeparatorIndex != -1)
			{
				if (fieldSeparatorIndex != -1 && recordSeparatorIndex != -1)
				{
					if (fieldSeparatorIndex < recordSeparatorIndex)
					{
						parseContext.Capture.Clear();
						parseContext.Capture.Append(capture.Substring(0, capture.Length - potentialSeparator.Length + fieldSeparatorIndex));
						token.Value = parseContext.FieldSeparator;
						return ReadFieldSeparator(row, parseContext);
					}
					parseContext.Capture.Clear();
					parseContext.Capture.Append(capture.Substring(0, capture.Length - potentialSeparator.Length + recordSeparatorIndex));
					token.Value = parseContext.RecordSeparator;
					return ReadRecordSeparator(row, parseContext);
				}
				if (fieldSeparatorIndex != -1 && fieldSeparatorIndex < recordSeparatorIndex || recordSeparatorIndex == -1)
				{
					if (parseContext.ReadingQuotedField && fieldSeparatorIndex != 0)
					{
						parseContext.Handle = ReadQuotedField;
						ThrowUnescapedQuoteException(parseContext);
					}
					parseContext.Capture.Clear();
					parseContext.Capture.Append(capture.Substring(0, capture.Length - potentialSeparator.Length + fieldSeparatorIndex));
					token.Value = parseContext.FieldSeparator;
					return ReadFieldSeparator(row, parseContext);
				}
				if (recordSeparatorIndex != -1)
				{
					var index = parseContext.FieldSeparator.IndexOf(parseContext.RecordSeparator);
					var potentialFieldSeparator = potentialSeparator.Substring(recordSeparatorIndex - index);
					if (index <= 0 || index >= 0 && potentialFieldSeparator.Length >= parseContext.FieldSeparator.Length && !potentialFieldSeparator.Contains(parseContext.FieldSeparator))
					{
						if (parseContext.ReadingQuotedField && recordSeparatorIndex != 0)
						{
							parseContext.Handle = ReadQuotedField;
							ThrowUnescapedQuoteException(parseContext);
						}
						parseContext.Capture.Clear();
						parseContext.Capture.Append(capture.Substring(0, capture.Length - potentialSeparator.Length + recordSeparatorIndex));
						token.Value = parseContext.RecordSeparator;
						var toReturn = ReadRecordSeparator(row, parseContext);
						var remainder = capture.Substring(recordSeparatorIndex + parseContext.RecordSeparator.Length);
						if (remainder.Length > 0)
						{
							parseContext.Capture.Append(remainder);
						}
						return toReturn;
					}
				}
			}
			parseContext.SeparatorLength++;
			if (parseContext.SeparatorLength > parseContext.FieldSeparator.Length &&
			    parseContext.SeparatorLength > parseContext.RecordSeparator.Length)
			{
				if (parseContext.ReadingQuotedField)
				{
					parseContext.Handle = ReadQuotedField;
					ThrowUnescapedQuoteException(parseContext);
				}

				if (Enumerable.Range(0, parseContext.FieldSeparator.Length)
				              .Where(x => x > 0)
				              .Select(x => parseContext.FieldSeparator.Substring(0, x)).All(x => potentialSeparator.IndexOf(x) == -1) &&
				    Enumerable.Range(0, parseContext.RecordSeparator.Length)
				              .Where(x => x > 0)
				              .Select(x => parseContext.RecordSeparator.Substring(0, x)).All(x => potentialSeparator.IndexOf(x) == -1))
				{
					parseContext.Handle = ReadUnquotedField;
				}
			}

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
					switch (token.Value)
					{
						case "\"":
							parseContext.Capture.Append('"');
							parseContext.Handle = ReadQuotedField;
							break;
						default:
							if (parseContext.FieldSeparator.Length > 0)
							{
								// possible end of field, more fields follow
								if (token.Value[0] == parseContext.FieldSeparator[0])
								{
									return StartCollectingSeparator(token, row, parseContext);
								}
							}
							if (parseContext.RecordSeparator.Length > 0)
							{
								// possible end of field, more fields follow
								if (token.Value[0] == parseContext.RecordSeparator[0])
								{
									return StartCollectingSeparator(token, row, parseContext);
								}
							}

							ThrowUnescapedQuoteException(parseContext);
							break;
					}
					break;
			}
			return false;
		}

		private static bool ReadFieldSeparator(Dictionary<string, string> row, ParseContext parseContext)
		{
			AddFieldValue(row, parseContext);
			parseContext.Handle = StartReadField;
			return false;
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

		private static bool ReadRecordSeparator(Dictionary<string, string> row, ParseContext parseContext)
		{
			AddFieldValue(row, parseContext);
			parseContext.ResetFieldNumber();
			parseContext.Handle = StartReadField;
			if (parseContext.HasHeaderRow && parseContext.ReadingHeaderRow)
			{
				parseContext.ReadingHeaderRow = false;
				parseContext.HeaderRow = new Dictionary<string, string>(row);
				CheckHeaderRowForDuplicateFieldNames(row);
				row.Clear();
				return false;
			}
			parseContext.RecordNumber++;
			return true;
		}

		private static void CheckHeaderRowForDuplicateFieldNames(Dictionary<string, string> row)
		{
			var reverseLookup = new Dictionary<string, string>();
			foreach (var field in row)
			{
				if (reverseLookup.ContainsKey(field.Value))
				{
					throw new ParseException(String.Format("Header row must not have more than one field with the same name. '{0}' appears more than once in the header row.", field.Value));
				}
				reverseLookup.Add(field.Value, field.Key);
			}
		}

		private static bool ReadUnquotedField(Token token, Dictionary<string, string> row, ParseContext parseContext)
		{
			switch (token.TokenType)
			{
				case TokenType.Data:
					parseContext.Capture.Append(token.Value);
					break;
				case TokenType.Special:
					if (parseContext.FieldSeparator.Length > 0 && token.Value[0] == parseContext.FieldSeparator[0] ||
					    parseContext.RecordSeparator.Length > 0 && token.Value[0] == parseContext.RecordSeparator[0])
					{
						return StartCollectingSeparator(token, row, parseContext);
					}
					parseContext.Capture.Append(token.Value);
					break;
			}
			return false;
		}

		private static bool StartCollectingSeparator(Token token, Dictionary<string, string> row, ParseContext parseContext)
		{
			parseContext.SeparatorLength = 1;
			parseContext.Handle = ContinueCollectSeparator;
			return ContinueCollectSeparator(token, row, parseContext);
		}

		private static bool StartReadField(Token token, Dictionary<string, string> row, ParseContext parseContext)
		{
			parseContext.Capture = new StringBuilder();
			parseContext.IncrementFieldNumber();
			if (parseContext.SupportQuotedFields && token.TokenType == TokenType.Special && token.Value == "\"")
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

		private static void ThrowUnescapedQuoteException(ParseContext parseContext)
		{
			throw new ParseException("Unescaped '\"' on line " + parseContext.RecordNumber + " field " + parseContext.FieldNumber);
		}

		private class ParseContext
		{
			public ParseContext()
			{
				FieldNumberNames = new List<string>();
			}

			public StringBuilder Capture { get; set; }
			public int FieldNumber { get; private set; }
			public List<string> FieldNumberNames { get; private set; }
			public string FieldSeparator { get; set; }
			public Func<Token, Dictionary<string, string>, ParseContext, bool> Handle { get; set; }
			public bool HasHeaderRow { get; set; }
			public Dictionary<string, string> HeaderRow { get; set; }
			public int MaxFieldNumber { get; set; }
			public bool ReadingHeaderRow { get; set; }
			public bool ReadingQuotedField { get; set; }
			public int RecordNumber { get; set; }
			public string RecordSeparator { get; set; }
			public int SeparatorLength { get; set; }
			public bool SupportQuotedFields { get; set; }

			public void IncrementFieldNumber()
			{
				FieldNumber++;
				if (FieldNumber > MaxFieldNumber)
				{
					FieldNumberNames.Add("Field" + FieldNumber);
					MaxFieldNumber = FieldNumber;
				}
			}

			public void ResetFieldNumber()
			{
				FieldNumber = 0;
			}
		}
	}
}