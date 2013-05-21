using System;
using System.Collections.Generic;

using FluentAssert;

using NUnit.Framework;

namespace EtlGate.Core.Tests
{
//// ReSharper disable ClassNeverInstantiated.Global
	public class RecordTests
//// ReSharper restore ClassNeverInstantiated.Global
	{
		[TestFixture]
		public class When_asked_for_a_field_by_heading
		{
			[Test]
			public void Given_a_valid_heading_whose_index_exceeds_the_number_of_fields__should_return_null()
			{
				var record = new Record(new[] { "aa", "bb" }, new Dictionary<string, int>
				                                              {
					                                              { "c", 2 }
				                                              });
				var field = record.GetField("c");
				field.ShouldBeNull();
			}

			[Test]
			public void Given_a_valid_heading_whose_index_is_valid_for_the_record__should_return_the_corresponding_field_value()
			{
				var record = new Record(new[] { "aa", "bb" }, new Dictionary<string, int>
				                                              {
					                                              { "b", 1 }
				                                              });
				var field = record.GetField("b");
				field.ShouldBeEqualTo("bb");
			}

			[Test]
			[ExpectedException(typeof(ArgumentException), ExpectedMessage = "foo" + Record.ErrorFieldNameIsNotAValidHeaderForThisRecordMessage + "\r\nParameter name: name")]
			public void Given_an_invalid_heading_name__should_throw_an_exception()
			{
				var record = new Record(new string[] { }, new Dictionary<string, int>());
				record.GetField("foo");
			}

			[Test]
			public void Given_the_records_does_not_contain_a_value_for_the_heading__should_return_null()
			{
				var record = new Record(new string[] { }, new Dictionary<string, int>());
				var field = record.GetField(0);
				field.ShouldBeNull();
			}
		}

		[TestFixture]
		public class When_asked_for_a_field_by_index
		{
			[Test]
			[ExpectedException(typeof(ArgumentOutOfRangeException), ExpectedMessage = Record.ErrorFieldIndexMustBeNonNegativeMessage + "\r\nParameter name: zeroBasedIndex")]
			public void Given_a_negative_index_value__should_throw_an_exception()
			{
				var record = new Record(new string[] { }, new Dictionary<string, int>());
				record.GetField(-1);
			}

			[Test]
			public void Given_a_valid_index_value__should_return_the_field_value()
			{
				var record = new Record(new[] { "a", "b", "c" }, new Dictionary<string, int>());
				var field = record.GetField(1);
				field.ShouldBeEqualTo("b");
			}

			[Test]
			public void Given_index_value_exceeds_available_number_of_fields__should_return_null()
			{
				var record = new Record(new string[] { }, new Dictionary<string, int>());
				var field = record.GetField(0);
				field.ShouldBeNull();
			}
		}
	}
}