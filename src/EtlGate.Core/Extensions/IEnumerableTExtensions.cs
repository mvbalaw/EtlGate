﻿using System.Collections.Generic;

namespace EtlGate.Core.Extensions
{
	public static class IEnumerableTExtensions
	{
		public static IEnumerable<LinkedListNode<T>> ToLinkedList<T>(this IEnumerable<T> items)
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