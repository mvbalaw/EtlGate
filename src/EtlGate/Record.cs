using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace EtlGate
{
	public class Record
	{
		public const string ErrorFieldIndexMustBeNonNegativeMessage = "field index must be >= 0.";
		public const string ErrorFieldNameIsNotAValidHeaderForThisRecordMessage = " is not a valid header for this record.";
		public const string ErrorFieldNumberIsNotAValidFieldForThisRecordMessage = " is not a valid field number for this record.";
		public const string ErrorFieldValueCannotBeCastTo = "Field {0} value cannot be cast to {1}: {2}";
		private readonly object[] _fields;
		private readonly IDictionary<string, int> _headings;

		public Record([NotNull] params string[] fields)
			: this(fields.Cast<object>().ToArray(), new Dictionary<string, int>())
		{
		}

		public Record([NotNull] IEnumerable<string> fields, [CanBeNull] IDictionary<string, int> headings = null)
			: this(fields.Cast<object>().ToArray(), headings ?? new Dictionary<string, int>())
		{
		}

		public Record([NotNull] IEnumerable<string> fields, params string[] headings)
			: this(fields.Cast<object>().ToArray(), HeadingsToDictionary(headings))
		{
		}

		public Record([NotNull] IEnumerable<string> fields, IList<string> headings = null)
			: this(fields.Cast<object>().ToArray(), HeadingsToDictionary(headings))
		{
		}

		internal Record([NotNull] IEnumerable<object> fields, params string[] headings)
			: this(fields.ToArray(), HeadingsToDictionary(headings))
		{
		}

		internal Record([NotNull] IEnumerable<object> fields, [NotNull] IDictionary<string, int> headings)
			: this(fields.ToArray(), headings)
		{
		}

		internal Record([NotNull] object[] fields, [NotNull] IDictionary<string, int> headings)
		{
			_fields = fields;
			_headings = headings;
		}

		public int FieldCount
		{
			[Pure] get { return _fields.Length; }
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

		public bool HasValueForField([NotNull] string name)
		{
			int zeroBasedIndex;
			if (!_headings.TryGetValue(name, out zeroBasedIndex))
			{
				return false;
			}

			if (!HasField(zeroBasedIndex))
			{
				return false;
			}
			var o = _fields[zeroBasedIndex];
			return o != null;
		}

		[CanBeNull]
		[Pure]
		public string GetField(int zeroBasedIndex)
		{
			if (!HasField(zeroBasedIndex))
			{
				if (HasHeaderWithIndex(zeroBasedIndex))
				{
					return null;
				}
				throw new ArgumentException(zeroBasedIndex + ErrorFieldNumberIsNotAValidFieldForThisRecordMessage);
			}
			var o = _fields[zeroBasedIndex];
			var oStr = o as string;
			if (oStr == null && o != null)
			{
				oStr = o.ToString();
			}
			return oStr;
		}

		private bool HasHeaderWithIndex(int zeroBasedIndex)
		{
			return _headings.Any(x => x.Value == zeroBasedIndex);
		}

		[CanBeNull]
		[Pure]
		public T GetField<T>(int zeroBasedIndex)
		{
			if (!HasField(zeroBasedIndex))
			{
				if (HasHeaderWithIndex(zeroBasedIndex))
				{
					return default(T);
				}
				throw new ArgumentException(zeroBasedIndex + ErrorFieldNumberIsNotAValidFieldForThisRecordMessage);
			}
			var o = _fields[zeroBasedIndex];
			if (o == null)
			{
				return default(T);
			}
			if (o == (object)default(T))
			{
				return (T)o;
			}
			if (o is T)
			{
				return (T)o;
			}
			throw new InvalidCastException(string.Format(ErrorFieldValueCannotBeCastTo,
				_headings.Any() ? "'" + _headings.Single(x => x.Value == zeroBasedIndex).Key + "'" : zeroBasedIndex.ToString(),
				typeof(T).Name,
				o));
		}

		[CanBeNull]
		[Pure]
		public T GetField<T>(string name)
		{
			return GetField<T>(GetFieldIndex(name));
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
			return zeroBasedIndex < _fields.Length;
		}

		[Pure]
		public bool HasField([NotNull] string name)
		{
			if (!_headings.ContainsKey(name))
			{
				return false;
			}
			var zeroBasedIndex = GetFieldIndex(name);
			return HasField(zeroBasedIndex);
		}

		public static Record For(IEnumerable<object> values)
		{
			return new Record(values.ToArray(), new Dictionary<string, int>());
		}

		public static Record For(object[] values, params string[] headings)
		{
			return new Record(values, HeadingsToDictionary(headings));
		}

		public static Record For(IEnumerable<object> values, IList<string> headings)
		{
			return new Record(values.ToArray(), HeadingsToDictionary(headings));
		}

		[NotNull]
		private static Dictionary<string, int> HeadingsToDictionary([CanBeNull] IList<string> headings)
		{
			if (headings == null)
			{
				return new Dictionary<string, int>();
			}
			return Enumerable.Range(0, headings.Count).ToDictionary(headingIndex => headings[headingIndex], fieldIndex => fieldIndex);
		}
	}
}