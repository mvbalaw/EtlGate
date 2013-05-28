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
					                   Capture = new StringBuilder(),
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
			var specialChars = specialTokens.SelectMany(x => x).Distinct().ToArray();
			foreach (var token in _streamTokenizer.Tokenize(bufferedStream, specialChars))
			{
				var yieldIt = parseContext.Handle(token, fieldValues, parseContext);
				if (yieldIt)
				{
					yield return new Record(fieldValues, parseContext.HeaderRowFieldIndexes);
					fieldValues = parseContext.HasHeaderRow
						              ? new List<string>(parseContext.HeaderRowFieldIndexes.Count)
						              : new List<string>(fieldValues.Count);
					parseContext.Capture.Length = 0;
				}
			}
			if (parseContext.Handle == ReadQuotedField)
			{
				throw new ParseException("Quoted field does not have a close quote on line " + parseContext.RecordNumber + " field " + parseContext.FieldNumber);
			}
			if (parseContext.ReadingQuotedField &&
			    (parseContext.Handle == ContinueCollectFieldSeparator || parseContext.Handle == ContinueCollectRecordSeparator))
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
				AddFieldValue(fieldValues, parseContext.Capture);
			}
			if (fieldValues.Count > 0)
			{
				yield return new Record(fieldValues, parseContext.HeaderRowFieldIndexes);
			}
		}

		private static void AddFieldValue(ICollection<string> fieldValues, StringBuilder capture)
		{
			var value = capture.ToString();
			fieldValues.Add(value);
			capture.Length = 0;
		}

		private bool BranchReadEscapedQuoteOrStartCollectingSeparator(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			if (token is SpecialToken)
			{
				switch (token.Value)
				{
					case "\"":
						parseContext.Capture.Append('"');
						parseContext.Handle = ReadQuotedField;
						break;
					default:
						var ch = token.Value[0];
						var fieldSeparator = parseContext.FieldSeparator;
						if (fieldSeparator.Length > 0 && ch.Equals(fieldSeparator[0]))
						{
							return StartCollectingFieldSeparator(token, fieldValues, parseContext);
						}
						var recordSeparator = parseContext.RecordSeparator;
						if (recordSeparator.Length > 0 && ch.Equals(recordSeparator[0]))
						{
							return StartCollectingRecordSeparator(token, fieldValues, parseContext);
						}

						ThrowUnescapedQuoteException(parseContext);
						break;
				}
			}
			else if (token is DataToken)
			{
				ThrowUnescapedQuoteException(parseContext);
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

		private bool ContinueCollectFieldSeparator(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			var capture = parseContext.Capture;
			capture.Append(token.Value);
			if (!(token is SpecialToken))
			{
				return HandleFailureToMatchExpectedSeparator(token, fieldValues, parseContext, capture, parseContext.RecordSeparator, HandleRecordSeparator);
			}

			var fieldSeparator = parseContext.FieldSeparator;
			if (fieldSeparator.Length.Equals(parseContext.SeparatorLength + 1))
			{
				var separatorStartIndex = capture.Length - fieldSeparator.Length;
				var potentialSeparator = capture.ToString(separatorStartIndex, fieldSeparator.Length);

				if (potentialSeparator.Equals(fieldSeparator))
				{
					capture.Length = separatorStartIndex;
					return HandleFieldSeparator(fieldValues, parseContext);
				}
				return HandleFailureToMatchExpectedSeparator(token, fieldValues, parseContext, capture, parseContext.RecordSeparator, HandleRecordSeparator);
			}

			parseContext.SeparatorLength++;

			return false;
		}

		private bool ContinueCollectRecordSeparator(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			var capture = parseContext.Capture;
			capture.Append(token.Value);
			if (!(token is SpecialToken))
			{
				return HandleFailureToMatchExpectedSeparator(token, fieldValues, parseContext, capture, parseContext.FieldSeparator, HandleFieldSeparator);
			}

			var recordSeparator = parseContext.RecordSeparator;
			if (recordSeparator.Length.Equals(parseContext.SeparatorLength + 1))
			{
				var separatorStartIndex = capture.Length - recordSeparator.Length;
				var potentialSeparator = capture.ToString(separatorStartIndex, recordSeparator.Length);

				if (potentialSeparator.Equals(recordSeparator))
				{
					capture.Length = separatorStartIndex;
					return HandleRecordSeparator(fieldValues, parseContext);
				}
				return HandleFailureToMatchExpectedSeparator(token, fieldValues, parseContext, capture, parseContext.FieldSeparator, HandleFieldSeparator);
			}

			parseContext.SeparatorLength++;

			return false;
		}

		private bool HandleFailureToMatchEitherSeparator(ParseContext parseContext, StringBuilder capture, int startIndex)
		{
			if (parseContext.ReadingQuotedField)
			{
				ThrowUnescapedQuoteException(parseContext);
			}

			// unquoted field, first character of this potential separator was actually data
			// push back everything that came after

			startIndex++;
			var contentLength = capture.Length - startIndex;
			if (contentLength > 0)
			{
				_streamTokenizer.PushBack(capture.ToString(startIndex, contentLength));
			}
			capture.Length = startIndex;
			parseContext.Handle = ReadUnquotedField;
			return false;
		}

		private bool HandleFailureToMatchExpectedSeparator(Token token, List<string> fieldValues, ParseContext parseContext, StringBuilder capture, string otherSeparator, Func<List<string>, ParseContext, bool> otherSeparatorHandler)
		{
			var startIndex = capture.Length - parseContext.SeparatorLength - token.Value.Length;

			if (otherSeparator.Length > 0)
			{
				if (capture.Length - startIndex < otherSeparator.Length)
				{
					return HandleFailureToMatchEitherSeparator(parseContext, capture, startIndex);
				}

				var potentialSeparator = capture.ToString(startIndex, otherSeparator.Length);
				if (potentialSeparator.Equals(otherSeparator))
				{
					// pushback to the start of the separator
					var contentLength = capture.Length - startIndex - otherSeparator.Length;
					if (contentLength > 0)
					{
						_streamTokenizer.PushBack(capture.ToString(startIndex + otherSeparator.Length, contentLength));
					}
					capture.Length = startIndex;
					return otherSeparatorHandler(fieldValues, parseContext);
				}
			}

			return HandleFailureToMatchEitherSeparator(parseContext, capture, startIndex);
		}

		private bool HandleFieldSeparator(ICollection<string> fieldValues, ParseContext parseContext)
		{
			AddFieldValue(fieldValues, parseContext.Capture);
			parseContext.Handle = StartReadField;
			return false;
		}

		private bool HandleRecordSeparator(IList<string> fieldValues, ParseContext parseContext)
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

		private bool ReadQuotedField(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			if (!(token is SpecialToken) || token.Value[0] != '"')
			{
				parseContext.Capture.Append(token.Value);
			}
			else
			{
				parseContext.Handle = BranchReadEscapedQuoteOrStartCollectingSeparator;
			}
			return false;
		}

		private bool ReadUnquotedField(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			if (token is SpecialToken)
			{
				var ch = token.Value[0];
				var fieldSeparator = parseContext.FieldSeparator;
				if (fieldSeparator.Length > 0 && ch.Equals(fieldSeparator[0]))
				{
					return StartCollectingFieldSeparator(token, fieldValues, parseContext);
				}
				var recordSeparator = parseContext.RecordSeparator;
				if (recordSeparator.Length > 0 && ch.Equals(recordSeparator[0]))
				{
					return StartCollectingRecordSeparator(token, fieldValues, parseContext);
				}
			}
			parseContext.Capture.Append(token.Value);
			return false;
		}

		private bool StartCollectingFieldSeparator(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			parseContext.SeparatorLength = 0;
			var fieldSeparator = parseContext.FieldSeparator;
			var recordSeparator = parseContext.RecordSeparator;

			if (recordSeparator.Length > 0 && fieldSeparator[0].Equals(recordSeparator[0]) && recordSeparator.Length > fieldSeparator.Length)
			{
				parseContext.Handle = ContinueCollectRecordSeparator;
			}
			else
			{
				parseContext.Handle = ContinueCollectFieldSeparator;
			}

			return parseContext.Handle(token, fieldValues, parseContext);
		}

		private bool StartCollectingRecordSeparator(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			parseContext.SeparatorLength = 0;
			parseContext.Handle = ContinueCollectRecordSeparator;

			return ContinueCollectRecordSeparator(token, fieldValues, parseContext);
		}

		private bool StartReadField(Token token, List<string> fieldValues, ParseContext parseContext)
		{
			if (!(token is EndOfStreamToken))
			{
				parseContext.IncrementFieldNumber();
				if (parseContext.SupportQuotedFields && token.Value.Equals("\"") && token is SpecialToken)
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