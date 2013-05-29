using System;

namespace EtlGate.Core
{
	public class DateFieldComparer : IFieldComparer
	{
		public const string ErrorInvalidDateValueInDelinquencyDateForFirstRow = "Invalid date value in DelinquencyDate for first row"; //todo
		public const string ErrorInvalidDateValueInDelinquencyDateForSecondRow = "Invalid date value in DelinquencyDate for second row"; // todo

		public DateFieldComparer(string fieldName)
		{
			FieldName = fieldName;
		}

		public string FieldName { get; private set; }
		public int Compare(string field1Value, string field2Value)
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
				throw new InvalidOperationException(ErrorInvalidDateValueInDelinquencyDateForFirstRow);
			}
			DateTime newDate;
			if (!DateTime.TryParse(field2Value, out newDate))
			{
				throw new InvalidOperationException(ErrorInvalidDateValueInDelinquencyDateForSecondRow);
			}

			return oldDate.CompareTo(newDate);
		}
	}
}