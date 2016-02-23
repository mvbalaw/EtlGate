using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using JetBrains.Annotations;

namespace EtlGate
{
	public interface IDelimitedDataReader
	{
		[NotNull]
		IEnumerable<Record> ReadFrom([NotNull] Stream stream, [CanBeNull] string fieldSeparator = ",", [CanBeNull] string recordSeparator = "\r\n", bool supportQuotedFields = true, bool hasHeaderRow = false, [CanBeNull] IDictionary<string, Func<string, object>> namedFieldConverters = null, [CanBeNull] IDictionary<int, Func<string, object>> indexedFieldConverters = null);

		[NotNull]
		IEnumerable<Record> ReadFromWithHeaders([NotNull] Stream stream, [CanBeNull] string fieldSeparator = ",", [CanBeNull] string recordSeparator = "\r\n", bool supportQuotedFields = true, [CanBeNull] IDictionary<string, Func<string, object>> namedFieldConverters = null);

		[NotNull]
		IEnumerable<Record> ReadFromWithoutHeaders([NotNull] Stream stream, [CanBeNull] string fieldSeparator = ",", [CanBeNull] string recordSeparator = "\r\n", bool supportQuotedFields = true, [CanBeNull] IDictionary<int, Func<string, object>> indexedFieldConverters = null);
	}

	public class DelimitedDataReader : IDelimitedDataReader
	{
		private readonly IStreamTokenizer _streamTokenizer;

		public DelimitedDataReader([NotNull] IStreamTokenizer streamTokenizer)
		{
			_streamTokenizer = streamTokenizer;
		}

		public const string ErrorNamedFieldConverters = "Can only use named field converters with files that have a header row.";
		public const string ErrorBothNamedAndIndexedFieldConverters = "Use either named field converters or indexed field converters but not both.";
		public const string ErrorFieldSeparatorAndRecordSeparator = "field separator and record separator must be different.";
		public const string ErrorQuotedFieldDoesNotHaveACloseQuoteOnLineField = "Quoted field does not have a close quote on line {0} field {1}";
		public const string ErrorDuplicateHeaderFieldName = "Header row must not have more than one field with the same name. '{0}' appears more than once in the header row.";
		public const string ErrorUnescapedDoubleQuote = "Unescaped '\"' on line {0} field {1}";
		public const string ErrorNoStream = "Stream is required.";

		public IEnumerable<Record> ReadFromWithHeaders(Stream stream, string fieldSeparator = ",", string recordSeparator = "\r\n", bool supportQuotedFields = true, IDictionary<string, Func<string, object>> namedFieldConverters = null)
		{
			return ReadFrom(stream, fieldSeparator, recordSeparator, supportQuotedFields, true, namedFieldConverters);
		}

		public IEnumerable<Record> ReadFromWithoutHeaders(Stream stream, string fieldSeparator = ",", string recordSeparator = "\r\n", bool supportQuotedFields = true, IDictionary<int, Func<string, object>> indexedFieldConverters = null)
		{
			return ReadFrom(stream, fieldSeparator, recordSeparator, supportQuotedFields, false, indexedFieldConverters:indexedFieldConverters);
		}

		public IEnumerable<Record> ReadFrom(Stream stream, string fieldSeparator = ",", string recordSeparator = "\r\n", bool supportQuotedFields = true, bool hasHeaderRow = false, IDictionary<string, Func<string, object>> namedFieldConverters = null, IDictionary<int, Func<string, object>> indexedFieldConverters = null)
		{
			if (stream == null)
			{
				throw new ArgumentException(ErrorNoStream);
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
				throw new ParseException(ErrorFieldSeparatorAndRecordSeparator);
			}

			if (namedFieldConverters != null && indexedFieldConverters != null)
			{
				throw new ArgumentException(ErrorBothNamedAndIndexedFieldConverters);
			}

			if (namedFieldConverters != null && !hasHeaderRow)
			{
				throw new ArgumentException(ErrorNamedFieldConverters);
			}

			var bufferedStream = new BufferedStream(stream, 131072);

			var fieldValues = new List<object>();
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
				                   NamedFieldConverters = namedFieldConverters,
				                   IndexedFieldConverters = indexedFieldConverters,
				                   StringCache = new Dictionary<string, string>()
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
					fieldValues = new List<object>(parseContext.HasHeaderRow
						? parseContext.HeaderRowFieldIndexes.Count
						: fieldValues.Count);
					parseContext.Capture.Length = 0;
				}
			}
			if (parseContext.Handle == ReadQuotedField)
			{
				throw new ParseException(string.Format(ErrorQuotedFieldDoesNotHaveACloseQuoteOnLineField, parseContext.RecordNumber, parseContext.FieldNumber));
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
				AddFieldValue(fieldValues, parseContext);
			}
			parseContext.StringCache = null;
			if (fieldValues.Count > 0)
			{
				yield return new Record(fieldValues, parseContext.HeaderRowFieldIndexes);
			}
		}

		private static void AddFieldValue(ICollection<object> fieldValues, ParseContext parseContext)
		{
			var strValue = parseContext.Capture.ToString();
			string cachedStrValue;
			if (!parseContext.StringCache.TryGetValue(strValue, out cachedStrValue))
			{
				parseContext.StringCache.Add(strValue, strValue);
				cachedStrValue = strValue;
			}

			object convertedValue = cachedStrValue;
			if (!parseContext.ReadingHeaderRow && parseContext.IndexedFieldConverters != null)
			{
				Func<string, object> converter;
				if (parseContext.IndexedFieldConverters.TryGetValue(fieldValues.Count, out converter))
				{
					convertedValue = converter(cachedStrValue);
				}
			}
			fieldValues.Add(convertedValue);
			parseContext.Capture.Length = 0;
		}

		private bool BranchReadEscapedQuoteOrStartCollectingSeparator(Token token, List<object> fieldValues, ParseContext parseContext)
		{
			if (token is SpecialToken)
			{
				var ch = token.First;
				if (token.Length.Equals(1) && ch.Equals('"'))
				{
					parseContext.Capture.Append(ch);
					parseContext.Handle = ReadQuotedField;
				}
				else
				{
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
				throw new ParseException(String.Format(ErrorDuplicateHeaderFieldName, field));
			}
		}

		private bool ContinueCollectFieldSeparator(Token token, List<object> fieldValues, ParseContext parseContext)
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

		private bool ContinueCollectRecordSeparator(Token token, List<object> fieldValues, ParseContext parseContext)
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

		private bool HandleFailureToMatchExpectedSeparator(Token token, List<object> fieldValues, ParseContext parseContext, StringBuilder capture, string otherSeparator, Func<List<object>, ParseContext, bool> otherSeparatorHandler)
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

		private bool HandleFieldSeparator(IList<object> fieldValues, ParseContext parseContext)
		{
			AddFieldValue(fieldValues, parseContext);
			parseContext.Handle = StartReadField;
			return false;
		}

		private bool HandleRecordSeparator(IList<object> fieldValues, ParseContext parseContext)
		{
			HandleFieldSeparator(fieldValues, parseContext);
			parseContext.ResetFieldNumber();
			if (parseContext.ReadingHeaderRow)
			{
				CheckHeaderRowForDuplicateFieldNames(fieldValues.Cast<string>());
				parseContext.ReadingHeaderRow = false;
				var headerRowFieldIndexes = new Dictionary<string, int>();
				for (var i = 0; i < fieldValues.Count; i++)
				{
					var key = (string)fieldValues[i];
					headerRowFieldIndexes[key] = i;
				}
				parseContext.HeaderRowFieldIndexes = headerRowFieldIndexes;
				if (parseContext.NamedFieldConverters != null)
				{
					var indexedFieldConverters = new Dictionary<int, Func<string, object>>();
					foreach (var kvp in parseContext.HeaderRowFieldIndexes)
					{
						Func<string, object> converter;
						if (parseContext.NamedFieldConverters.TryGetValue(kvp.Key, out converter))
						{
							indexedFieldConverters[kvp.Value] = converter;
						}
					}
					parseContext.IndexedFieldConverters = indexedFieldConverters;
				}
				fieldValues.Clear();
				return false;
			}
			parseContext.RecordNumber++;
			return true;
		}

		private bool ReadQuotedField(Token token, List<object> fieldValues, ParseContext parseContext)
		{
			if (!(token is SpecialToken) || !token.First.Equals('"'))
			{
				parseContext.Capture.Append(token.Value);
			}
			else
			{
				parseContext.Handle = BranchReadEscapedQuoteOrStartCollectingSeparator;
			}
			return false;
		}

		private bool ReadUnquotedField(Token token, List<object> fieldValues, ParseContext parseContext)
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

		private bool StartCollectingFieldSeparator(Token token, List<object> fieldValues, ParseContext parseContext)
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

		private bool StartCollectingRecordSeparator(Token token, List<object> fieldValues, ParseContext parseContext)
		{
			parseContext.SeparatorLength = 0;
			parseContext.Handle = ContinueCollectRecordSeparator;

			return ContinueCollectRecordSeparator(token, fieldValues, parseContext);
		}

		private bool StartReadField(Token token, List<object> fieldValues, ParseContext parseContext)
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
			throw new ParseException(string.Format(ErrorUnescapedDoubleQuote, parseContext.RecordNumber, parseContext.FieldNumber));
		}

		private class ParseContext
		{
			public StringBuilder Capture { get; set; }
			public int FieldNumber { get; private set; }
			public string FieldSeparator { get; set; }
			public Func<Token, List<object>, ParseContext, bool> Handle { get; set; }
			public bool HasHeaderRow { get; set; }
			public Dictionary<string, int> HeaderRowFieldIndexes { get; set; }
			public bool ReadingHeaderRow { get; set; }
			public bool ReadingQuotedField { get; set; }
			public int RecordNumber { get; set; }
			public string RecordSeparator { get; set; }
			public int SeparatorLength { get; set; }
			public bool SupportQuotedFields { get; set; }
			public IDictionary<string, Func<string, object>> NamedFieldConverters { get; set; }
			public IDictionary<int, Func<string, object>> IndexedFieldConverters { get; set; }
			public Dictionary<string, string> StringCache { get; set; }

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