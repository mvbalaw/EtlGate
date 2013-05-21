using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EtlGate.Core
{
	public interface IDelimitedDataReader
	{
		IEnumerable<Record> ReadFrom(Stream stream, string fieldSeparator = ",", string recordSeparator = "\r\n", bool supportQuotedFields = true, bool hasHeaderRow = false);
	}

	public class DelimitedDataReader : IDelimitedDataReader
	{
		private readonly IStreamTokenizer _streamTokenizer;

		public DelimitedDataReader(IStreamTokenizer streamTokenizer)
		{
			_streamTokenizer = streamTokenizer;
		}

		public IEnumerable<Record> ReadFrom(Stream stream, string fieldSeparator = ",", string recordSeparator = "\r\n", bool supportQuotedFields = true, bool hasHeaderRow = false)
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

			var bufferedStream = new BufferedStream(stream, 131072);

			var fieldValues = new List<string>();
			var parseContext = new ParseContext
				                   {
					                   HeaderRowFieldIndexes = new Dictionary<string, int>(),
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
			foreach (var token in _streamTokenizer.Tokenize(bufferedStream, specialChars))
			{
				var yieldIt = parseContext.Handle(token, fieldValues, parseContext);
				if (yieldIt)
				{
					yield return new Record(fieldValues, parseContext.HeaderRowFieldIndexes);
					fieldValues = new List<string>();
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
				AddFieldValue(fieldValues, parseContext);
			}
			if (fieldValues.Count > 0)
			{
				yield return new Record(fieldValues, parseContext.HeaderRowFieldIndexes);
			}
		}

		private static void AddFieldValue(ICollection<string> fieldValues, ParseContext parseContext)
		{
			var value = parseContext.Capture.ToString();
			fieldValues.Add(value);
			parseContext.Capture.Clear();
		}

		private static bool ContinueCollectSeparator(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			parseContext.Capture.Append(token.Value);
			var potentialSeparator = parseContext.Capture.ToString(parseContext.Capture.Length - parseContext.SeparatorLength, parseContext.SeparatorLength);
			var fieldSeparatorIndex = parseContext.FieldSeparator.Length > 0 ? potentialSeparator.IndexOf(parseContext.FieldSeparator, StringComparison.Ordinal) : -1;
			var recordSeparatorIndex = parseContext.RecordSeparator.Length > 0 ? potentialSeparator.IndexOf(parseContext.RecordSeparator, StringComparison.Ordinal) : -1;
			if (fieldSeparatorIndex != -1 || recordSeparatorIndex != -1)
			{
				if (fieldSeparatorIndex != -1 && recordSeparatorIndex != -1)
				{
					if (fieldSeparatorIndex < recordSeparatorIndex)
					{
						parseContext.Capture.Length = parseContext.Capture.Length - potentialSeparator.Length + fieldSeparatorIndex;
						return ReadFieldSeparator(fieldValues, parseContext);
					}
					parseContext.Capture.Length = parseContext.Capture.Length - potentialSeparator.Length + recordSeparatorIndex;
					return ReadRecordSeparator(fieldValues, parseContext);
				}
				if (fieldSeparatorIndex != -1 && fieldSeparatorIndex < recordSeparatorIndex || recordSeparatorIndex == -1)
				{
					if (parseContext.ReadingQuotedField && fieldSeparatorIndex != 0)
					{
						parseContext.Handle = ReadQuotedField;
						ThrowUnescapedQuoteException(parseContext);
					}
					parseContext.Capture.Length = parseContext.Capture.Length - potentialSeparator.Length + fieldSeparatorIndex;
					return ReadFieldSeparator(fieldValues, parseContext);
				}

				var index = parseContext.FieldSeparator.IndexOf(parseContext.RecordSeparator, StringComparison.Ordinal);
				var potentialFieldSeparator = potentialSeparator.Substring(recordSeparatorIndex - index);
				if (index <= 0 || index >= 0 && potentialFieldSeparator.Length >= parseContext.FieldSeparator.Length && !potentialFieldSeparator.Contains(parseContext.FieldSeparator))
				{
					if (parseContext.ReadingQuotedField && recordSeparatorIndex != 0)
					{
						parseContext.Handle = ReadQuotedField;
						ThrowUnescapedQuoteException(parseContext);
					}
					var remainder = parseContext.Capture.ToString(recordSeparatorIndex + parseContext.RecordSeparator.Length, parseContext.Capture.Length - (recordSeparatorIndex + parseContext.RecordSeparator.Length));
					parseContext.Capture.Length = parseContext.Capture.Length - potentialSeparator.Length + recordSeparatorIndex;
					var toReturn = ReadRecordSeparator(fieldValues, parseContext);
					if (remainder.Length > 0)
					{
						parseContext.Capture.Append(remainder);
					}
					return toReturn;
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
							  .Select(x => parseContext.FieldSeparator.Substring(0, x))
							  .All(x => potentialSeparator.IndexOf(x, StringComparison.Ordinal) == -1) &&
				    Enumerable.Range(0, parseContext.RecordSeparator.Length)
				              .Where(x => x > 0)
							  .Select(x => parseContext.RecordSeparator.Substring(0, x))
							  .All(x => potentialSeparator.IndexOf(x, StringComparison.Ordinal) == -1))
				{
					parseContext.Handle = ReadUnquotedField;
				}
			}

			return false;
		}

		private static bool ReadEscapedQuoteOrEndQuotedField(Token token, List<string> fieldValues, ParseContext parseContext)
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
									return StartCollectingSeparator(token, fieldValues, parseContext);
								}
							}
							if (parseContext.RecordSeparator.Length > 0)
							{
								// possible end of field, more fields follow
								if (token.Value[0] == parseContext.RecordSeparator[0])
								{
									return StartCollectingSeparator(token, fieldValues, parseContext);
								}
							}

							ThrowUnescapedQuoteException(parseContext);
							break;
					}
					break;
			}
			return false;
		}

		private static bool ReadFieldSeparator(ICollection<string> fieldValues, ParseContext parseContext)
		{
			AddFieldValue(fieldValues, parseContext);
			parseContext.Handle = StartReadField;
			return false;
		}

		private static bool ReadQuotedField(Token token, List<string> fieldValues, ParseContext parseContext)
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

		private static bool ReadRecordSeparator(IList<string> fieldValues, ParseContext parseContext)
		{
			AddFieldValue(fieldValues, parseContext);
			parseContext.ResetFieldNumber();
			parseContext.Handle = StartReadField;
			if (parseContext.HasHeaderRow && parseContext.ReadingHeaderRow)
			{
				parseContext.ReadingHeaderRow = false;
				var headerRowFieldIndexes = new Dictionary<string, int>();
				for (int i = 0; i < fieldValues.Count; i++)
				{
					var key = fieldValues[i];
					headerRowFieldIndexes[key] = i;
				}
				parseContext.HeaderRowFieldIndexes = headerRowFieldIndexes;
				CheckHeaderRowForDuplicateFieldNames(fieldValues);
				fieldValues.Clear();
				return false;
			}
			parseContext.RecordNumber++;
			return true;
		}

		private static void CheckHeaderRowForDuplicateFieldNames(IEnumerable<string> fieldValues)
		{
			var uniqueHeaderLookup = new HashSet<string>();
			foreach (var field in fieldValues)
			{
				if (!uniqueHeaderLookup.Add(field))
				{
					throw new ParseException(String.Format("Header row must not have more than one field with the same name. '{0}' appears more than once in the header row.", field));
				}
			}
		}

		private static bool ReadUnquotedField(Token token, List<string> fieldValues, ParseContext parseContext)
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
						return StartCollectingSeparator(token, fieldValues, parseContext);
					}
					parseContext.Capture.Append(token.Value);
					break;
			}
			return false;
		}

		private static bool StartCollectingSeparator(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			parseContext.SeparatorLength = 1;
			parseContext.Handle = ContinueCollectSeparator;
			return ContinueCollectSeparator(token, fieldValues, parseContext);
		}

		private static bool StartReadField(Token token, List<string> fieldValues, ParseContext parseContext)
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
				return ReadUnquotedField(token, fieldValues, parseContext);
			}
			return false;
		}

		private static void ThrowUnescapedQuoteException(ParseContext parseContext)
		{
			throw new ParseException("Unescaped '\"' on line " + parseContext.RecordNumber + " field " + parseContext.FieldNumber);
		}

		private class ParseContext
		{
			public StringBuilder Capture { get; set; }
			public int FieldNumber { get; private set; }
			public string FieldSeparator { get; set; }
			public Func<Token, List<string>, ParseContext, bool> Handle { get; set; }
			public bool HasHeaderRow { get; set; }
			public Dictionary<string, int> HeaderRowFieldIndexes { get; set; }
			public bool ReadingHeaderRow { get; set; }
			public bool ReadingQuotedField { get; set; }
			public int RecordNumber { get; set; }
			public string RecordSeparator { get; set; }
			public int SeparatorLength { get; set; }
			public bool SupportQuotedFields { get; set; }

			public void IncrementFieldNumber()
			{
				FieldNumber++;
			}

			public void ResetFieldNumber()
			{
				FieldNumber = 0;
			}
		}
	}
}