using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace EtlGate
{
	public interface IRecordKeyComparer : IComparer<Record>
	{
	}

	public class RecordKeyComparer : IRecordKeyComparer
	{
		private readonly IFieldComparer[] _fieldComparersInOrder;

		public RecordKeyComparer([NotNull] params IFieldComparer[] fieldComparersInOrder)
		{
			_fieldComparersInOrder = fieldComparersInOrder;
		}

		[Pure]
		public int Compare(Record record1, Record record2)
		{
			if (record1 == null || record2 == null)
			{
				if (record1 == record2)
				{
					return 0;
				}
				return record1 == null ? 1 : -1;
			}
			var result = _fieldComparersInOrder
				.Select(x => x.Compare(record1.GetField(x.FieldName), record2.GetField(x.FieldName)))
				.FirstOrDefault(x => x != 0);

			return result;
		}
	}
}