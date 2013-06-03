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
		public int Compare([NotNull] Record record1, [NotNull] Record record2)
		{
			var result = _fieldComparersInOrder
				.Select(x => x.Compare(record1.GetField(x.FieldName), record2.GetField(x.FieldName)))
				.FirstOrDefault(x => x != 0);

			return result;
		}
	}
}