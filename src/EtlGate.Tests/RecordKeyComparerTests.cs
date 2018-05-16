using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using FluentAssert;

using JetBrains.Annotations;

using NUnit.Framework;

namespace EtlGate.Tests
{
	[UsedImplicitly]
	public class RecordKeyComparerTests
	{
		[TestFixture]
		public class When_asked_to_compare
		{
			[Test]
			public void FuzzTestIt()
			{
				const string possibleFieldNames = "abcdefghijklm";

				var random = new Random();

				for (var i = 0; i < 50000; i++)
				{
					var numberOfFields = random.Next(10);

					var fieldNames = Enumerable.Range(0, numberOfFields).Select(x => possibleFieldNames[x]).ToArray();
					var comparerKeys = Enumerable.Range(0, numberOfFields).Select(x => "slr"[random.Next(3)]).ToArray();
					var comparerers = new List<IFieldComparer>();
					for (var j = 0; j < numberOfFields; j++)
					{
						var fieldName = fieldNames[j].ToString(CultureInfo.InvariantCulture);
						var comparerKey = comparerKeys[j];
						switch (comparerKey)
						{
							case 's':
								comparerers.Add(new SameResultComparer(fieldName));
								break;
							case 'l':
								comparerers.Add(new LeftBeforeRightResultComparer(fieldName));
								break;
							case 'r':
								comparerers.Add(new RightBeforeLeftResultComparer(fieldName));
								break;
						}
					}

					var record1 = CreateRecord(random, possibleFieldNames);
					var record2 = CreateRecord(random, possibleFieldNames);

					var recordKeyComparer = new RecordKeyComparer(comparerers.ToArray());
					var expected = GetExpected(record1, record2, comparerers);
					try
					{
						var actual = recordKeyComparer.Compare(record1, record2);
						actual.ShouldBeEqualTo(expected.Value);
						if (expected.ShouldThrowDueToInvalidHeader)
						{
							Assert.Fail("Should have thrown invalid header exception.");
						}
					}
					catch (Exception exception)
					{
						if (expected.ShouldThrowDueToInvalidHeader && exception.Message.Contains(Record.ErrorFieldNameIsNotAValidHeaderForThisRecordMessage))
						{
							continue;
						}

						Console.WriteLine("fields:	  " + new string(fieldNames));
						Console.WriteLine("comparers: " + new string(fieldNames));
						Console.WriteLine(exception);
					}
				}
			}

			private static Record CreateRecord(Random random, string possibleFieldNames)
			{
				var fields = Enumerable.Range(0, random.Next(10)).Select(x => possibleFieldNames[x].ToString(CultureInfo.InvariantCulture)).Distinct().ToList();
				var record = new Record(fields, fields.ToDictionary(x => x, fields.IndexOf));
				return record;
			}

			private static ResultInfo GetExpected(Record left, Record right, IEnumerable<IFieldComparer> comparerers)
			{
				foreach (var comparer in comparerers)
				{
					var fieldName = comparer.FieldName;
					if (!left.HasField(fieldName) || !right.HasField(fieldName))
					{
						return new ResultInfo
							       {
								       ShouldThrowDueToInvalidHeader = true
							       };
					}
					var leftValue = left[fieldName];
					var rightValue = right[fieldName];
					var result = comparer.Compare(leftValue, rightValue);
					if (result != 0)
					{
						return new ResultInfo
							       {
								       Value = result
							       };
					}
				}
				return new ResultInfo();
			}

			private class LeftBeforeRightResultComparer : IFieldComparer
			{
				public LeftBeforeRightResultComparer([NotNull] string fieldName)
				{
					FieldName = fieldName;
				}

				public int Compare(string x, string y)
				{
					return -1;
				}

				public string FieldName { get; private set; }
			}

			private class ResultInfo
			{
				public bool ShouldThrowDueToInvalidHeader;
				public int Value;
			}

			private class RightBeforeLeftResultComparer : IFieldComparer
			{
				public RightBeforeLeftResultComparer([NotNull] string fieldName)
				{
					FieldName = fieldName;
				}

				public int Compare(string x, string y)
				{
					return 1;
				}

				public string FieldName { get; private set; }
			}

			private class SameResultComparer : IFieldComparer
			{
				public SameResultComparer([NotNull] string fieldName)
				{
					FieldName = fieldName;
				}

				public int Compare(string x, string y)
				{
					return 0;
				}

				public string FieldName { get; private set; }
			}
		}
	}
}