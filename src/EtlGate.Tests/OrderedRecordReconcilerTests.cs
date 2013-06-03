using System.Collections.Generic;
using System.Linq;

using FluentAssert;

using JetBrains.Annotations;

using NUnit.Framework;

namespace EtlGate.Tests
{
	[UsedImplicitly]
	public class OrderedRecordReconcilerTests
	{
		[TestFixture]
		public class When_asked_to_reconcile_two_Ordered_IEnumerable_of_Record
		{

			private OrderedRecordReconciler _orderedRecordReconciler;
			private List<ReconciliationResult<Record>> _expectedList;
			private List<Record> _newList;
			private List<Record> _oldList;

			[Test]
			public void Given_empty_old_IEnumerable_of_Record_and_new_IEnumerable_of_Record__should_return_all_new_IEnumerable_of_Record_with_ReconciliationStatus_Added()
			{
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));

				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Added));
				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
											ReconciliationStatus.Added));

				var result = CompareResults();
				foreach (var entry in result.Zip(_expectedList, (first, second) => new
				{
					result = first,
					expectedOutput = second
				}))
				{
					entry.result.Status.ShouldBeEqualTo(entry.expectedOutput.Status);
					entry.result.Item.GetField("ACCOUNT_NUMBER").ShouldBeEqualTo(entry.expectedOutput.Item.GetField("ACCOUNT_NUMBER"));
					entry.result.Item.GetField("TAX_YEAR").ShouldBeEqualTo(entry.expectedOutput.Item.GetField("TAX_YEAR"));
				}
			}

			private IEnumerable<ReconciliationResult<Record>> CompareResults()
			{
				var dataLoadRecordComparer = new RecordKeyComparer(new StringFieldComparer("ACCOUNT_NUMBER"), new StringFieldComparer("TAX_YEAR"), new StringFieldComparer("SUBJURISDICTION_CODE"), new DateFieldComparer("DELINQUENCY_DATE"));

				var result = _orderedRecordReconciler.Reconcile(_oldList, _newList, new RecordReconciler(), dataLoadRecordComparer);
				return result;
			}

			[Test]
			public void Given_old_IEnumerable_of_Record_and_empty_new_IEnumerable_of_Record__should_return_old_IEnumerable_of_Record_with_ReconciliationStatus_Deleted()
			{
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));

				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Deleted));
				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Deleted));
				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Deleted));
				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Deleted));

				var result = CompareResults();

				foreach (var entry in result.Zip(_expectedList, (first, second) => new
				{
					result = first,
					expected = second
				}))
				{
					entry.result.Status.ShouldBeEqualTo(entry.expected.Status);
					entry.result.Item.GetField("ACCOUNT_NUMBER").ShouldBeEqualTo(entry.expected.Item.GetField("ACCOUNT_NUMBER"));
				}
			}


			[Test]
			public void Given_old_IEnumerable_of_Record_and_new_IEnumerable_of_Record_with_same_records__should_return_empty_list()
			{
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));

				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));

				var result = CompareResults();
				result.Count().ShouldBeEqualTo(0);
			}

			[Test]
			public void Given_old_IEnumerable_of_Record_and_new_IEnumerable_of_Record_with_same_keys_but_different_data__should_return_IEnumerable_of_Record_with_ReconciliationStatus_Updated()
			{
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2009" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2010" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));

				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2010" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));

				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2009" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Deleted));
				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Added));

				var result = CompareResults();

				foreach (var entry in result.Zip(_expectedList, (first, second) => new
				{
					result = first,
					expectedOutput = second
				}))
				{
					entry.result.Status.ShouldBeEqualTo(entry.expectedOutput.Status);
					entry.result.Item.GetField("ACCOUNT_NUMBER").ShouldBeEqualTo(entry.expectedOutput.Item.GetField("ACCOUNT_NUMBER"));
					entry.result.Item.GetField("TAX_YEAR").ShouldBeEqualTo(entry.expectedOutput.Item.GetField("TAX_YEAR"));
				}
			}

			[Test]
			public void Given_old_IEnumerable_of_Record_and_new_IEnumerable_of_Record_with_added_records__should_return_new_IEnumerable_of_Record_with_ReconciliationStatus_Added()
			{
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));

				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));

				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Added));
				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Added));
				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Added));
				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Added));

				var result = CompareResults();

				foreach (var entry in result.Zip(_expectedList, (first, second) => new
				{
					result = first,
					expectedOutput = second
				}))
				{
					entry.result.Status.ShouldBeEqualTo(entry.expectedOutput.Status);
					entry.result.Item.GetField("ACCOUNT_NUMBER").ShouldBeEqualTo(entry.expectedOutput.Item.GetField("ACCOUNT_NUMBER"));
					entry.result.Item.GetField("TAX_YEAR").ShouldBeEqualTo(entry.expectedOutput.Item.GetField("TAX_YEAR"));
				}
			}

			[Test]
			public void Given_old_IEnumerable_of_Record_with_records_not_in_new_IEnumerable_of_Record__should_return_old_IEnumerable_of_Record_with_ReconciliationStatus_Deleted()
			{
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_oldList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));

				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));
				_newList.Add(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }));

				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Deleted));
				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Deleted));
				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2011" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Deleted));
				_expectedList.Add(new ReconciliationResult<Record>(CreateRecordFromDictionary(new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" }, { "TAX_YEAR", "2012" }, { "SUBJURISDICTION_CODE", "000" }, { "DELINQUENCY_DATE", "02/01/2012" } }),
															ReconciliationStatus.Deleted));

				var result = CompareResults();

				foreach (var entry in result.Zip(_expectedList, (first, second) => new
				{
					result = first,
					expectedOutput = second
				}))
				{
					entry.result.Status.ShouldBeEqualTo(entry.expectedOutput.Status);
					entry.result.Item.GetField("ACCOUNT_NUMBER").ShouldBeEqualTo(entry.expectedOutput.Item.GetField("ACCOUNT_NUMBER"));
					entry.result.Item.GetField("TAX_YEAR").ShouldBeEqualTo(entry.expectedOutput.Item.GetField("TAX_YEAR"));
				}
			}

			[Test]
			public void Given_two_empty_lists__should_return_an_empty_list()
			{
				var result = CompareResults();
				result.Count().ShouldBeEqualTo(0);
			}

			[SetUp]
			protected void Before_Each_Test()
			{
				_oldList = new List<Record>();
				_newList = new List<Record>();
				_expectedList = new List<ReconciliationResult<Record>>();
				_orderedRecordReconciler = StructureMap.ObjectFactory.GetInstance<OrderedRecordReconciler>();
			}

			private static Record CreateRecordFromDictionary(Dictionary<string, string> dictionary)
			{
				var heading = new Dictionary<string, int>();
				var row = new List<string>();
				var index = 0;
				foreach (var entry in dictionary)
				{
					heading.Add(entry.Key, index);
					row.Add(entry.Value);
					index++;
				}

				var record = new Record(row, heading);
				return record;
			}
		}
	}
}