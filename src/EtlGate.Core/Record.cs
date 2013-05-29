using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace EtlGate.Core
{
	public class Record
	{
		public const string ErrorFieldIndexMustBeNonNegativeMessage = "field index must be >= 0.";
		public const string ErrorFieldNameIsNotAValidHeaderForThisRecordMessage = " is not a valid header for this record.";
		private readonly IList<string> _fields;
		private readonly IDictionary<string, int> _headings;

		public Record([NotNull] params string[] fields)
			: this(fields, new Dictionary<string, int>())
		{
		}

		public Record([NotNull] IList<string> fields, [NotNull] IDictionary<string, int> headings)
		{
			_fields = fields;
			_headings = headings;
		}

		public int FieldCount
		{
			[Pure] get { return _fields.Count; }
		}
		public IList<string> HeadingFieldNames
		{
			[Pure] get { return _headings.Keys.ToList(); }
		}

		[CanBeNull]
		public string this[[NotNull] string name]
		{
			[Pure] get { return GetField(name); }
		}

		[CanBeNull]
		public string this[int zeroBasedIndex]
		{
			[Pure] get { return GetField(zeroBasedIndex); }
		}

		[CanBeNull]
		[Pure]
		public string GetField([NotNull] string name)
		{
			return GetField(GetFieldIndex(name));
		}

		[CanBeNull]
		[Pure]
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

		[Pure]
		public bool HasField(int zeroBasedIndex)
		{
			if (zeroBasedIndex < 0)
			{
				throw new ArgumentOutOfRangeException("zeroBasedIndex", ErrorFieldIndexMustBeNonNegativeMessage);
			}
			return zeroBasedIndex < _fields.Count;
		}

		[Pure]
		public bool HasField([NotNull] string name)
		{
			var zeroBasedIndex = GetFieldIndex(name);
			return HasField(zeroBasedIndex);
		}
	}
}