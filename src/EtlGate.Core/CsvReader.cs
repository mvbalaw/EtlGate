using System.Collections.Generic;
using System.IO;

namespace EtlGate.Core
{
	public interface ICsvReader
	{
		IEnumerable<Dictionary<string, string>> ReadFrom(Stream stream, string recordSeparator = "\r\n", bool hasHeaderRow = false);
	}

	public class CsvReader : ICsvReader
	{
		private readonly IDelimitedDataReader _delimitedDataReader;

		public CsvReader(IDelimitedDataReader delimitedDataReader)
		{
			_delimitedDataReader = delimitedDataReader;
		}

		public IEnumerable<Dictionary<string, string>> ReadFrom(Stream stream, string recordSeparator = "\r\n", bool hasHeaderRow = false)
		{
			return _delimitedDataReader.ReadFrom(stream, ",", recordSeparator, true, hasHeaderRow);
		}
	}
}