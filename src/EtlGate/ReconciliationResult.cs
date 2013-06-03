using JetBrains.Annotations;

namespace EtlGate
{
	public class ReconciliationResult<T>
	{
		public ReconciliationResult(T item, [NotNull] ReconciliationStatus state)
		{
			Item = item;
			Status = state;
		}

		public T Item { [Pure] get; private set; }
		public ReconciliationStatus Status { [Pure] get; private set; }
	}
}