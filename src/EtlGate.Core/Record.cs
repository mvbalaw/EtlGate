using System;
using System.Collections.Generic;
using System.Linq;

namespace EtlGate.Core
{
	public class Record
	{
		public const string ErrorFieldIndexMustBeNonNegativeMessage = "field index must be >= 0.";
		public const string ErrorFieldNameIsNotAValidHeaderForThisRecordMessage = " is not a valid header for this record.";
		private readonly IList<string> _fields;
		private readonly IDictionary<string, int> _headings;

		public Record(params string[] fields)
			: this(fields, new Dictionary<string, int>())
		{
		}

		public Record(IList<string> fields, IDictionary<string, int> headings)
		{
			_fields = fields;
			_headings = headings;
		}

		public int FieldCount
		{
			get { return _fields.Count; }
		}
		public IList<string> HeadingFieldNames
		{
			get { return _headings.Keys.ToList(); }
		}

		public string GetField(string name)
		{
			return GetField(GetFieldIndex(name));
		}

		public string GetField(int zeroBasedIndex)
		{
			return HasField(zeroBasedIndex) ? _fields[zeroBasedIndex] : null;
		}

		private int GetFieldIndex(string name)
		{
			int zeroBasedIndex;
			if (!_headings.TryGetValue(name, out zeroBasedIndex))
			{
				throw new ArgumentException(name + ErrorFieldNameIsNotAValidHeaderForThisRecordMessage, "name");
			}
			return zeroBasedIndex;
		}

		public bool HasField(int zeroBasedIndex)
		{
			if (zeroBasedIndex < 0)
			{
				throw new ArgumentOutOfRangeException("zeroBasedIndex", ErrorFieldIndexMustBeNonNegativeMessage);
			}
			return zeroBasedIndex < _fields.Count;
		}

		public bool HasField(string name)
		{
			var zeroBasedIndex = GetFieldIndex(name);
			return HasField(zeroBasedIndex);
		}
	}
}