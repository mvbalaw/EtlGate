using System;
using System.Collections;

using JetBrains.Annotations;

using MvbaCore;

namespace EtlGate
{
	public class ReconciliationStatus : NamedConstant<ReconciliationStatus>
	{
		public static readonly ReconciliationStatus Added = new ReconciliationStatus("Added", (l, r) => r, DoNothingEnumerator, IncrementEnumerator);
		public static readonly ReconciliationStatus Deleted = new ReconciliationStatus("Deleted", (l, r) => l, IncrementEnumerator, DoNothingEnumerator);
		public static readonly ReconciliationStatus Same = new ReconciliationStatus("Same", (l, r) => l, IncrementEnumerator, IncrementEnumerator);
		public static readonly ReconciliationStatus Updated = new ReconciliationStatus("Updated", (l, r) => r, IncrementEnumerator, IncrementEnumerator);

		private ReconciliationStatus(string key, Func<object, object, object> getItem, Func<IEnumerator, bool> incrementLeftIfNecessary, Func<IEnumerator, bool> incrementRightIfNecessary)
		{
			GetItem = getItem;
			IncrementLeftIfNecessary = incrementLeftIfNecessary;
			IncrementRightIfNecessary = incrementRightIfNecessary;
			Add(key, this);
		}

		public Func<object, object, object> GetItem { [Pure] get; private set; }
		public Func<IEnumerator, bool> IncrementLeftIfNecessary { get; private set; }
		public Func<IEnumerator, bool> IncrementRightIfNecessary { get; private set; }

		private static bool DoNothingEnumerator(IEnumerator enumerator)
		{
			return true;
		}

		private static bool IncrementEnumerator(IEnumerator enumerator)
		{
			return enumerator.MoveNext();
		}
	}
}