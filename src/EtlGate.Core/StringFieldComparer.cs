namespace EtlGate.Core
{
	public class StringFieldComparer : IFieldComparer
	{
		public StringFieldComparer(string fieldName)
		{
			FieldName = fieldName;
		}

		public string FieldName { get; private set; }
		public int Compare(string field1Value, string field2Value)
		{
			return string.CompareOrdinal(field1Value, field2Value);
		}
	}
}