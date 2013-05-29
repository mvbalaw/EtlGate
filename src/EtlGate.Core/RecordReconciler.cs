using System;
using System.Linq;

namespace EtlGate.Core
{
	public interface IRecordReconciler
	{
		ReconciliationStatus ReconcileRecords(Record left, Record right, IRecordKeyComparer recordKeyComparer);
	}

	public class RecordReconciler : IRecordReconciler
	{
		public ReconciliationStatus ReconcileRecords(Record left, Record right, IRecordKeyComparer recordKeyComparer)
		{
			var fieldComparisonResult = recordKeyComparer.Compare(left, right);
			if (fieldComparisonResult == 0)
			{
				return DictionaryAllEntriesMatch(left, right) ? ReconciliationStatus.Same : ReconciliationStatus.Updated;
			}

			return fieldComparisonResult < 0 ? ReconciliationStatus.Deleted : ReconciliationStatus.Added;
		}

		private static bool DictionaryAllEntriesMatch(Record oldItem, Record newItem)
		{
			var oldItemKeys = oldItem.HeadingFieldNames;
			var newItemKeys = newItem.HeadingFieldNames;
			if (oldItem.FieldCount != newItem.FieldCount)
			{
				return false;
			}

			if (oldItemKeys.Any(x => !newItemKeys.Contains(x)))
			{
				return false;
			}

			return oldItemKeys.All(k => String.CompareOrdinal(oldItem.GetField(k), newItem.GetField(k)) == 0);
		}
	}
}