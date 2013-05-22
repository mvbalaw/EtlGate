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
					                   IndexOfRecordSeparatorInFieldSeparator = fieldSeparator.IndexOf(recordSeparator, StringComparison.Ordinal),
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
					fieldValues = parseContext.HasHeaderRow
						              ? new List<string>(parseContext.HeaderRowFieldIndexes.Count)
						              : new List<string>(fieldValues.Count);
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
			    (parseContext.Handle == StartReadField || parseContext.Handle == BranchReadEscapedQuoteOrStartCollectingSeparator))
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
			var capture = parseContext.Capture;
			var value = capture.ToString();
			fieldValues.Add(value);
			capture.Length = 0;
		}

		private static bool BranchReadEscapedQuoteOrStartCollectingSeparator(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			if (token is DataToken)
			{
				ThrowUnescapedQuoteException(parseContext);
			}

			switch (token.Value)
			{
				case "\"":
					parseContext.Capture.Append('"');
					parseContext.Handle = ReadQuotedField;
					break;
				default:
					if (parseContext.FieldSeparator.Length > 0 && token.Value[0] == parseContext.FieldSeparator[0] ||
					    parseContext.RecordSeparator.Length > 0 && token.Value[0] == parseContext.RecordSeparator[0])
					{
						// possible end of field, more fields follow
						return StartCollectingSeparator(token, fieldValues, parseContext);
					}

					ThrowUnescapedQuoteException(parseContext);
					break;
			}
			return false;
		}

		private static void CheckHeaderRowForDuplicateFieldNames(IEnumerable<string> fieldValues)
		{
			var uniqueHeaderLookup = new HashSet<string>();
			foreach (var field in fieldValues.Where(field => !uniqueHeaderLookup.Add(field)))
			{
				throw new ParseException(String.Format("Header row must not have more than one field with the same name. '{0}' appears more than once in the header row.", field));
			}
		}

		private static bool ContinueCollectSeparator(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			var capture = parseContext.Capture;
			capture.Append(token.Value);
			var potentialSeparator = capture.ToString(capture.Length - parseContext.SeparatorLength, parseContext.SeparatorLength);
			var fieldSeparatorIndex = parseContext.FieldSeparator.Length > 0 && 
				potentialSeparator.EndsWith(parseContext.FieldSeparator, StringComparison.Ordinal) 
				? potentialSeparator.Length - parseContext.FieldSeparator.Length : -1;
			var recordSeparatorIndex = parseContext.RecordSeparator.Length > 0 && 
				potentialSeparator.EndsWith(parseContext.RecordSeparator, StringComparison.Ordinal) 
				? potentialSeparator.Length - parseContext.RecordSeparator.Length : -1;

			if (fieldSeparatorIndex != -1 && recordSeparatorIndex != -1)
			{
				return ReadFirstSeparator(fieldValues, parseContext, fieldSeparatorIndex, recordSeparatorIndex, potentialSeparator);
			}

			if (fieldSeparatorIndex != -1)
			{
				return TryReadFieldSeparator(fieldValues, parseContext, fieldSeparatorIndex, potentialSeparator);
			}

			if (recordSeparatorIndex != -1)
			{
				if (parseContext.IndexOfRecordSeparatorInFieldSeparator <= 0)
				{
					return TryReadRecordSeparator(fieldValues, parseContext, recordSeparatorIndex, potentialSeparator);
				}

				// implicit:	parseContext.IndexOfRecordSeparatorInFieldSeparator > 0
				var potentialFieldSeparator = potentialSeparator.Substring(recordSeparatorIndex - parseContext.IndexOfRecordSeparatorInFieldSeparator);
				if (potentialFieldSeparator.Length >= parseContext.FieldSeparator.Length)
				{
					return TryReadRecordSeparator(fieldValues, parseContext, recordSeparatorIndex, potentialSeparator);
				}
			}

			parseContext.SeparatorLength++;

			return false;
		}

		private static bool HandleFieldSeparator(ICollection<string> fieldValues, ParseContext parseContext)
		{
			AddFieldValue(fieldValues, parseContext);
			parseContext.Handle = StartReadField;
			return false;
		}

		private static bool HandleRecordSeparator(IList<string> fieldValues, ParseContext parseContext)
		{
			HandleFieldSeparator(fieldValues, parseContext);
			parseContext.ResetFieldNumber();
			if (parseContext.ReadingHeaderRow)
			{
				CheckHeaderRowForDuplicateFieldNames(fieldValues);
				parseContext.ReadingHeaderRow = false;
				var headerRowFieldIndexes = new Dictionary<string, int>();
				for (var i = 0; i < fieldValues.Count; i++)
				{
					var key = fieldValues[i];
					headerRowFieldIndexes[key] = i;
				}
				parseContext.HeaderRowFieldIndexes = headerRowFieldIndexes;
				fieldValues.Clear();
				return false;
			}
			parseContext.RecordNumber++;
			return true;
		}

		private static bool ReadFirstSeparator(IList<string> fieldValues, ParseContext parseContext, int fieldSeparatorIndex, int recordSeparatorIndex, string potentialSeparator)
		{
			return fieldSeparatorIndex < recordSeparatorIndex
				       ? ReadLeadingSeparatorFromCapture(fieldValues, parseContext, fieldSeparatorIndex, potentialSeparator, HandleFieldSeparator)
				       : ReadLeadingSeparatorFromCapture(fieldValues, parseContext, recordSeparatorIndex, potentialSeparator, HandleRecordSeparator);
		}

		private static bool ReadLeadingSeparatorFromCapture(IList<string> fieldValues, ParseContext parseContext, int separatorIndex, string potentialSeparator, Func<IList<string>, ParseContext, bool> readSeparatorFunc)
		{
			var capture = parseContext.Capture;
			capture.Length = capture.Length - potentialSeparator.Length + separatorIndex;
			return readSeparatorFunc(fieldValues, parseContext);
		}

		private static bool ReadQuotedField(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			if (token.Value[0] != '"' || token is DataToken)
			{
				parseContext.Capture.Append(token.Value);
			}
			else
			{
				parseContext.Handle = BranchReadEscapedQuoteOrStartCollectingSeparator;
			}
			return false;
		}

		private static bool ReadUnquotedField(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			if (token is SpecialToken)
			{
				if (parseContext.FieldSeparator.Length > 0 && token.Value[0] == parseContext.FieldSeparator[0] ||
				    parseContext.RecordSeparator.Length > 0 && token.Value[0] == parseContext.RecordSeparator[0])
				{
					return StartCollectingSeparator(token, fieldValues, parseContext);
				}
			}
			parseContext.Capture.Append(token.Value);
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
			parseContext.IncrementFieldNumber();
			if (parseContext.SupportQuotedFields && token.Value == "\"" && token is SpecialToken)
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

		private static void ThrowIfReadingQuotedFieldAndSeparatorIsNotFirst(ParseContext parseContext, int separatorIndex)
		{
			if (parseContext.ReadingQuotedField && separatorIndex != 0)
			{
				ThrowUnescapedQuoteException(parseContext);
			}
		}

		private static void ThrowUnescapedQuoteException(ParseContext parseContext)
		{
			throw new ParseException("Unescaped '\"' on line " + parseContext.RecordNumber + " field " + parseContext.FieldNumber);
		}

		private static bool TryReadFieldSeparator(IList<string> fieldValues, ParseContext parseContext, int fieldSeparatorIndex, string potentialSeparator)
		{
			ThrowIfReadingQuotedFieldAndSeparatorIsNotFirst(parseContext, fieldSeparatorIndex);
			return ReadLeadingSeparatorFromCapture(fieldValues, parseContext, fieldSeparatorIndex, potentialSeparator, HandleFieldSeparator);
		}

		private static bool TryReadRecordSeparator(IList<string> fieldValues, ParseContext parseContext, int recordSeparatorIndex, string potentialSeparator)
		{
			ThrowIfReadingQuotedFieldAndSeparatorIsNotFirst(parseContext, recordSeparatorIndex);
			var remainder = parseContext.Capture.ToString(parseContext.Capture.Length - potentialSeparator.Length + parseContext.RecordSeparator.Length, potentialSeparator.Length - parseContext.RecordSeparator.Length);
			var toReturn = ReadLeadingSeparatorFromCapture(fieldValues, parseContext, recordSeparatorIndex, potentialSeparator, HandleRecordSeparator);
			if (remainder.Length > 0)
			{
				parseContext.Capture.Append(remainder);
			}
			return toReturn;
		}

		private class ParseContext
		{
			public StringBuilder Capture { get; set; }
			public int FieldNumber { get; private set; }
			public string FieldSeparator { get; set; }
			public Func<Token, List<string>, ParseContext, bool> Handle { get; set; }
			public bool HasHeaderRow { get; set; }
			public Dictionary<string, int> HeaderRowFieldIndexes { get; set; }
			public int IndexOfRecordSeparatorInFieldSeparator { get; set; }
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