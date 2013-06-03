using System.Collections.Generic;

using JetBrains.Annotations;

namespace EtlGate.Extensions
{
	public static class IEnumerableTExtensions
	{
		[NotNull]
		[Pure]
		public static IEnumerable<LinkedListNode<T>> ToLinkedList<T>([NotNull] this IEnumerable<T> items)
		{
			var list = new LinkedList<T>();
			LinkedListNode<T> current = null;
			foreach (var item in items)
			{
				if (current == null)
				{
					current = list.AddLast(item);
					continue;
				}
				var next = list.AddLast(item);
				yield return current;
				current = next;
			}

			if (current != null)
			{
				yield return current;
			}
		}
	}
}