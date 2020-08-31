using System;
using System.Collections.Generic;

using FluentAssert;

using JetBrains.Annotations;

using NUnit.Framework;

namespace EtlGate.Tests
{
	[UsedImplicitly]
	public class RecordReconcilerTests
	{
		[TestFixture]
		public class When_asked_to_compare_two_records
		{
			
			[Test]
			public void Given_two_Records_with_first_AccountNumber_before_than_second__should_return_ReconcilationStatus_Deleted()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" } };
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "2" } };

				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Deleted);
			}

			[Test]
			public void Given_two_Records_with_first_AccountNumber_after_than_second__should_return_ReconcilationStatus_Added()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "4" } };
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "3" } };
		
				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Added);
			}

			private static ReconciliationStatus CompareResults(Record firstRecord, Record secondRecord)
			{
				var dataLoadRecordComparer = new RecordKeyComparer(new StringFieldComparer("ACCOUNT_NUMBER"), new StringFieldComparer("TAX_YEAR"), new StringFieldComparer("SUBJURISDICTION_CODE"), new DateFieldComparer("DELINQUENCY_DATE"));
				var result = new RecordReconciler().ReconcileRecords(firstRecord, secondRecord, dataLoadRecordComparer);
				return result;
			}


			[Test]
			public void Given_two_Records_with_matching_AccountNumber_and_first_TaxYear_before_than_second__should_return_ReconcilationStatus_Deleted()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2009"} };
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2010"} };

				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Deleted);
			}

			[Test]
			public void Given_two_Records_with_matching_AccountNumber_and_first_TaxYear_after_than_second__should_return_ReconcilationStatus_Added()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2012" } };
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2011" } };

				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Added);
			}


			[Test]
			public void Given_two_Records_with_matching_AccountNumber_TaxYear_and_first_SubjurisdictionCode_before_than_second__should_return_ReconcilationStatus_Deleted()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "000" } };
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" } };

				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Deleted);
			}


			[Test]
			public void Given_two_Records_with_matching_AccountNumber_TaxYear_and_first_SubjurisdictionCode_after_than_second__should_return_ReconcilationStatus_Added()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" } };
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "000" } };

				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Added);
			}


			[Test]
			public void Given_two_Records_with_matching_AccountNumber_TaxYear_SubjurisdictionCode_and_blank_DelinquencyDates__should_return_ReconcilationStatus_Same()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "" } };
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "" } };

				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Same);
			}


			[Test]
			public void Given_two_Records_with_matching_AccountNumber_TaxYear_SubjurisdictionCode_and_first_DelinquencyDate_blank__should_return_ReconcilationStatus_Deleted()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "" } };
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "04/01/2014" } };

				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Deleted);
			}

			[Test]
			public void Given_two_Records_with_matching_AccountNumber_TaxYear_SubjurisdictionCode_and_second_DelinquencyDate_blank__should_return_ReconcilationStatus_Added()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "04/01/2014" } };
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "" } };

				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Added);
			}

			[Test]
			public void Given_two_Records_with_matching_AccountNumber_TaxYear_SubjurisdictionCode_and_DelinquencyDate_earlier_than_second__should_return_ReconcilationStatus_Deleted()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "07/01/2014" } };
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "11/01/2014" } };

				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Deleted);
			}

			[Test]
			public void Given_two_Records_with_matching_AccountNumber_TaxYear_SubjurisdictionCode_and_DelinquencyDate_later_than_second__should_return_ReconcilationStatus_Added()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "04/01/2014" } };
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "02/01/2014" } };

				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Added);
			}

			[Test]
			public void Given_two_Records_with_matching_keys_and_no_data__should_return_ReconcilationStatus_Same()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "02/01/2014" } };
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "02/01/2014" } };

				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);
				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Same);
			}

			[Test]
			public void Given_two_Records_with_matching_keys_and_different_number_of_fields__should_return_ReconcilationStatus_Updated()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "02/01/2014" },
				                                             { "DataField1", "Some Data" }, { "DataField2", "Some More Data" }};
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "02/01/2014" },
								                                             { "DataField1", "Some Data" }};

				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Updated);
			}


			[Test]
			public void Given_two_Records_with_matching_keys_and_different_fields__should_return_ReconcilationStatus_Updated()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "02/01/2014" },
				                                             { "DataField1", "Some Data" }};
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "02/01/2014" },
								                              { "DataField2", "Some Data" }};
				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Updated);
			}

			[Test]
			public void Given_two_Records_with_matching_keys_and_different_field_values__should_return_ReconcilationStatus_Updated()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "02/01/2014" },
				                                             { "DataField", "Some Data" }};
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "02/01/2014" },
								                              { "DataField", "Some Other Data" }};
				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var result = CompareResults(firstRecord, secondRecord);

				result.ShouldBeEqualTo(ReconciliationStatus.Updated);
			}

			[Test]
			public void Given_two_Records_with_first_having_invalid_DelinquencyDate_field__should_throw_InvalidOperationException()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "Invalid date" },
				                                             { "DataField", "Some Data" }};
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "02/01/2014" },
								                              { "DataField", "Some Other Data" }};
				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var exception = Assert.Throws<InvalidOperationException>(() => CompareResults(firstRecord, secondRecord));
				exception.Message.ShouldBeEqualTo(DateFieldComparer.ErrorField1HasInvalidDateValue);
			}


			[Test]
			public void Given_two_Records_with_second_having_invalid_DelinquencyDate_field__should_throw_InvalidOperationException()
			{
				var first = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "02/01/2014" },
				                                             { "DataField", "Some Data" }};
				var second = new Dictionary<string, string> { { "ACCOUNT_NUMBER", "1" }, { "TAX_YEAR", "2013" }, { "SUBJURISDICTION_CODE", "999" }, { "DELINQUENCY_DATE", "Invalid date" },
								                              { "DataField", "Some Other Data" }};
				var firstRecord = CreateRecordFromDictionary(first);
				var secondRecord = CreateRecordFromDictionary(second);

				var exception = Assert.Throws<InvalidOperationException>(() => CompareResults(firstRecord, secondRecord));
				exception.Message.ShouldBeEqualTo(DateFieldComparer.ErrorField2HasInvalidDateValue);
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