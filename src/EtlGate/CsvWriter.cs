using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EtlGate
{
	public interface ICsvWriter
	{
		void WriteTo(StreamWriter writer, IEnumerable<Record> records, bool includeHeaders);
		void WriteTo(string fileName, IEnumerable<Record> records, bool includeHeaders);
	}

	public class CsvWriter : ICsvWriter
	{
		private static void WriteList(IEnumerable<string> values, TextWriter writer, string fieldDelimiter, string recordDelimiter)
		{
			bool first = true;
			foreach (var field in values)
			{
				if (!first)
				{
					writer.Write(fieldDelimiter);
				}
				else
				{
					first = false;
				}
				writer.Write(Csv.Escape(field));
			}
			writer.Write(recordDelimiter);
		}

		public void WriteTo(StreamWriter writer, IEnumerable<Record> records, bool includeHeaders)
		{
			if (writer == null)
			{
				throw new ArgumentException("Please provide a writer.", "writer");
			}
			if (records == null)
			{
				throw new ArgumentException("No records provided.", "records");
			}

			const string fieldDelimiter = ",";
			const string recordDelimiter = "\r\n";

			foreach (var record in records)
			{
				if (includeHeaders)
				{
					WriteList(record.HeadingFieldNames, writer, fieldDelimiter, recordDelimiter);
					includeHeaders = false;
				}
				WriteList(Enumerable
					.Range(0, record.FieldCount)
					.Select(record.GetField), writer, fieldDelimiter, recordDelimiter);
			}
		}

		public void WriteTo(string fileName, IEnumerable<Record> records, bool includeHeaders)
		{
			if (fileName == null)
			{
				throw new ArgumentException("Please provide a file name.", "fileName");
			}
			if (records == null)
			{
				throw new ArgumentException("No records provided.", "records");
			}

			var stream = new FileStream(fileName, FileMode.Create);
			using (var writer = new StreamWriter(stream))
			{
				WriteTo(writer, records, includeHeaders);
			}
		}
	}
}