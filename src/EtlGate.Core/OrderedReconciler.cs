using System;
using System.Collections.Generic;

using JetBrains.Annotations;

namespace EtlGate.Core
{
	public class OrderedReconciler<T>
	{
		public const string ErrorNotSortedMessage = "Enumerable must be sorted on key";

		private static void CheckOrder(Func<T, T, ReconciliationStatus> reconcileItems, T previousItem, T currentItem)
		{
			var reconciliationStatus = reconcileItems(previousItem, currentItem);
			if (reconciliationStatus == ReconciliationStatus.Added)
			{
				throw new ArgumentException(ErrorNotSortedMessage);
			}
		}

		[NotNull]
		public IEnumerable<ReconciliationResult<T>> Reconcile([NotNull] IEnumerable<T> left, [NotNull] IEnumerable<T> right, [NotNull] Func<T, T, ReconciliationStatus> reconcileItems)
		{
			if (left == null)
			{
				throw new ArgumentNullException("left");
			}
			if (right == null)
			{
				throw new ArgumentNullException("right");
			}

			var leftEnumerator = left.GetEnumerator();
			var rightEnumerator = right.GetEnumerator();

			var moreOnLeft = leftEnumerator.MoveNext();
			var moreOnRight = rightEnumerator.MoveNext();

			var previousLeft = leftEnumerator.Current;
			var previousRight = rightEnumerator.Current;

			while (moreOnLeft && moreOnRight)
			{
				var leftItem = leftEnumerator.Current;
				var rightItem = rightEnumerator.Current;

				CheckOrder(reconcileItems, previousLeft, leftItem);
				previousLeft = leftItem;

				CheckOrder(reconcileItems, previousRight, rightItem);
				previousRight = rightItem;

				var reconciliationStatus = reconcileItems(leftItem, rightItem);

				var item = (T)reconciliationStatus.GetItem(leftItem, rightItem);
				yield return new ReconciliationResult<T>(item, reconciliationStatus);
				moreOnLeft = reconciliationStatus.IncrementLeftIfNecessary(leftEnumerator);
				moreOnRight = reconciliationStatus.IncrementRightIfNecessary(rightEnumerator);
			}

			while (moreOnLeft)
			{
				var leftItem = leftEnumerator.Current;
				CheckOrder(reconcileItems, previousLeft, leftItem);
				previousLeft = leftItem;

				yield return new ReconciliationResult<T>(leftItem, ReconciliationStatus.Deleted);
				moreOnLeft = leftEnumerator.MoveNext();
			}
			while (moreOnRight)
			{
				var rightItem = rightEnumerator.Current;
				CheckOrder(reconcileItems, previousRight, rightItem);
				previousRight = rightItem;

				yield return new ReconciliationResult<T>(rightItem, ReconciliationStatus.Added);
				moreOnRight = rightEnumerator.MoveNext();
			}
		}
	}
}