using System.Collections.Generic;
using System.Linq;

namespace EtlGate
{
	public static class RecordExtensions
	{
		public static IEnumerable<Record> Sort(this IEnumerable<Record> input, IRecordKeyComparer comparer)
		{
			return input.OrderBy(x => x, comparer);
		}
	}
}