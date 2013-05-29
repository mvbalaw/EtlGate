using System;

using JetBrains.Annotations;

namespace EtlGate.Core
{
	public class DateFieldComparer : IFieldComparer
	{
		public const string ErrorField1HasInvalidDateValue = "Field 1 has an invalid date value";
		public const string ErrorField2HasInvalidDateValue = "Field 2 has an invalid date value";

		public DateFieldComparer([NotNull] string fieldName)
		{
			FieldName = fieldName;
		}

		public string FieldName { get; private set; }

		[Pure]
		public int Compare([NotNull] string field1Value, [NotNull] string field2Value)
		{
			if ((field1Value == "") && (field2Value == ""))
			{
				return 0;
			}

			if (field1Value == "")
			{
				return -1;
			}

			if (field2Value == "")
			{
				return 1;
			}

			DateTime oldDate;
			if (!DateTime.TryParse(field1Value, out oldDate))
			{
				throw new InvalidOperationException(ErrorField1HasInvalidDateValue);
			}
			DateTime newDate;
			if (!DateTime.TryParse(field2Value, out newDate))
			{
				throw new InvalidOperationException(ErrorField2HasInvalidDateValue);
			}

			return oldDate.CompareTo(newDate);
		}
	}
}