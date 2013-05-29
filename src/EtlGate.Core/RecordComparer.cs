using System.Collections.Generic;
using System.Linq;

namespace EtlGate.Core
{
	public interface IRecordKeyComparer : IComparer<Record>
	{
	}

	public class RecordKeyComparer : IRecordKeyComparer
	{
		private readonly IFieldComparer[] _fieldComparersInOrder;

		public RecordKeyComparer(params IFieldComparer[] fieldComparersInOrder)
		{
			_fieldComparersInOrder = fieldComparersInOrder;
		}

		public int Compare(Record record1, Record record2)
		{
			var result = _fieldComparersInOrder
				.Select(x => x.Compare(record1.GetField(x.FieldName), record2.GetField(x.FieldName)))
				.FirstOrDefault(x => x != 0);

			return result;
		}
	}
}