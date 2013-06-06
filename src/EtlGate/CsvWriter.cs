using System;
using System.Collections.Generic;
using System.IO;

namespace EtlGate
{
	public static class CsvWriter
	{

		public static void WriteTo(StreamWriter writer, IEnumerable<Record> records, bool includeHeaders)
		{
			const string fieldDelimiter = ",";
			const string recordDelimiter = "\r\n";

			var firstRecord = true;
			foreach (var record in records)
			{
				if (firstRecord && includeHeaders)
				{
					firstRecord = false;
					WriteList(record.HeadingFieldNames, writer, fieldDelimiter, recordDelimiter);
				}

				var rowValues = new List<string>();
				for (var i = 0; i < record.FieldCount; i++)
				{
					rowValues.Add(record.GetField(i));
				}
				WriteList(rowValues, writer, fieldDelimiter, recordDelimiter);
			}
		}

		public static void WriteTo(string fileName, IEnumerable<Record> records, bool includeHeaders)
		{
			if ((fileName == null) || (records == null))
			{
				throw new ArgumentException();
			}

			var stream = new FileStream(fileName, FileMode.Create);
			using (var writer = new StreamWriter(stream))
			{
				WriteTo(writer, records, includeHeaders);
			}
		}

		private static void WriteList(IEnumerable<string> values, TextWriter writer, string fieldDelimiter, string recordDelimiter)
		{
			var firstField = true;
			foreach (var field in values)
			{
				if (!firstField)
				{
					writer.Write(fieldDelimiter);
				}
				writer.Write(Csv.Escape(field));
				firstField = false;
			}
			writer.Write(recordDelimiter);
		}
	}

	// http://stackoverflow.com/questions/4685705/good-csv-writer-for-c

	public static class Csv
	{
		public static string Escape(string s)
		{
			if (s.Contains(Quote))
				s = s.Replace(Quote, EscapedQuote);

			if (s.IndexOfAny(CharactersThatMustBeQuoted) > -1)
				s = Quote + s + Quote;

			return s;
		}

		private const string Quote = "\"";
		private const string EscapedQuote = "\"\"";
		private static readonly char[] CharactersThatMustBeQuoted = { ',', '"', '\n' };
	}

}