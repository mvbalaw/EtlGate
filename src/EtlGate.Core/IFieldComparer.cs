using System.Collections.Generic;

namespace EtlGate.Core
{
	public interface IFieldComparer: IComparer<string> // todo -- move these to EtlGate and add IntFieldComparer, BoolFieldComparer
	{
		string FieldName { get; }
	}
}