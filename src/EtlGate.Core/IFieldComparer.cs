using System.Collections.Generic;

using JetBrains.Annotations;

namespace EtlGate.Core
{
	public interface IFieldComparer : IComparer<string> // todo -- add IntFieldComparer, BoolFieldComparer
	{
		[NotNull]
		string FieldName { [Pure] get; }
	}
}