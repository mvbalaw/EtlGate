using System.Collections.Generic;

using JetBrains.Annotations;

namespace EtlGate
{
	public interface IFieldComparer : IComparer<string> // todo -- add IntFieldComparer, BoolFieldComparer
	{
		[NotNull]
		string FieldName { [Pure] get; }
	}
}