namespace EtlGate.Core
{
	public class ReconciliationResult<T>
	{
		public ReconciliationResult(T item, ReconciliationStatus state)
		{
			Item = item;
			Status = state;
		}

		public T Item { get; private set; }
		public ReconciliationStatus Status { get; private set; }
	}
}