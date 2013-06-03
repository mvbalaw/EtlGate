using JetBrains.Annotations;

namespace EtlGate
{
	public class StringFieldComparer : IFieldComparer
	{
		public StringFieldComparer([NotNull] string fieldName)
		{
			FieldName = fieldName;
		}

		public string FieldName { [Pure] get; private set; }

		[Pure]
		public int Compare([NotNull] string field1Value, [NotNull] string field2Value)
		{
			return string.CompareOrdinal(field1Value, field2Value);
		}
	}
}