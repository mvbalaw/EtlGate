using System;
using System.Collections.Generic;
using System.IO;

using JetBrains.Annotations;

namespace EtlGate
{
	public interface ICsvReader
	{
		[NotNull]
		IEnumerable<Record> ReadFrom([NotNull] Stream stream, [CanBeNull] string recordSeparator = "\r\n", bool hasHeaderRow = false, [CanBeNull] IDictionary<string, Func<string, object>> namedFieldConverters = null, [CanBeNull] IDictionary<int, Func<string, object>> indexedFieldConverters = null);

		[NotNull]
		IEnumerable<Record> ReadFromWithHeaders([NotNull] Stream stream, [CanBeNull] string recordSeparator = "\r\n", [CanBeNull] IDictionary<string, Func<string, object>> namedFieldConverters = null);

		[NotNull]
		IEnumerable<Record> ReadFromWithoutHeaders([NotNull] Stream stream, [CanBeNull] string recordSeparator = "\r\n", [CanBeNull] IDictionary<int, Func<string, object>> indexedFieldConverters = null);
	}

	public class CsvReader : ICsvReader
	{
		private readonly IDelimitedDataReader _delimitedDataReader;

		public CsvReader(IDelimitedDataReader delimitedDataReader)
		{
			_delimitedDataReader = delimitedDataReader;
		}

		public IEnumerable<Record> ReadFrom(Stream stream, string recordSeparator = "\r\n", bool hasHeaderRow = false, IDictionary<string, Func<string, object>> namedFieldConverters = null, IDictionary<int, Func<string, object>> indexedFieldConverters = null)
		{
			return _delimitedDataReader.ReadFrom(stream, ",", recordSeparator, true, hasHeaderRow, namedFieldConverters, indexedFieldConverters);
		}

		public IEnumerable<Record> ReadFromWithHeaders(Stream stream, string recordSeparator = "\r\n", IDictionary<string, Func<string, object>> namedFieldConverters = null)
		{
			return _delimitedDataReader.ReadFromWithHeaders(stream, ",", recordSeparator, true, namedFieldConverters);
		}

		public IEnumerable<Record> ReadFromWithoutHeaders(Stream stream, string recordSeparator = "\r\n", IDictionary<int, Func<string, object>> indexedFieldConverters = null)
		{
			return _delimitedDataReader.ReadFromWithoutHeaders(stream, ",", recordSeparator, true, indexedFieldConverters);
		}
	}
}