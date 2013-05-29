using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace EtlGate.Core
{
	public interface IOrderedRecordReconciler
	{
		[NotNull]
		IEnumerable<ReconciliationResult<Record>> Reconcile([NotNull] IEnumerable<Record> left, [NotNull] IEnumerable<Record> right, [NotNull] IRecordReconciler recordReconciler, [NotNull] IRecordKeyComparer recordKeyComparer);
	}

	[UsedImplicitly]
	public class OrderedRecordReconciler : IOrderedRecordReconciler
	{
		public IEnumerable<ReconciliationResult<Record>> Reconcile(IEnumerable<Record> left, IEnumerable<Record> right, IRecordReconciler recordReconciler, IRecordKeyComparer recordKeyComparer)
		{
			if (left == null)
			{
				throw new ArgumentNullException("left");
			}
			if (right == null)
			{
				throw new ArgumentNullException("right");
			}
			if (recordKeyComparer == null)
			{
				throw new ArgumentNullException("recordKeyComparer");
			}

			var comparer = new OrderedReconciler<Record>();
			return comparer
				.Reconcile(left, right, (o, n) => recordReconciler.ReconcileRecords(o, n, recordKeyComparer))
				.Where(result => result.Status != ReconciliationStatus.Same);
		}
	}
}