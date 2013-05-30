using System;
using System.Collections.Generic;

using FluentAssert;

using JetBrains.Annotations;

using NUnit.Framework;

namespace EtlGate.Core.Tests
{
	[UsedImplicitly]
	public class RecordTests
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
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
				record.GetField("foo");
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
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
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
				record.GetField(-1);
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
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

		[TestFixture]
		public class When_asked_if_the_record_has_a_field_by_index
		{
			[Test]
			[ExpectedException(typeof(ArgumentOutOfRangeException), ExpectedMessage = Record.ErrorFieldIndexMustBeNonNegativeMessage + "\r\nParameter name: zeroBasedIndex")]
			public void Given_a_negative_field_number_is_requested__should_throw_ArgumentOutOfRangeException()
			{
				var record = new Record(new[] { "x" });
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
				record.HasField(-1);
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
			}

			[Test]
			public void Given_a_record_with_1_field_and_field__0__is_requested__should_return_true()
			{
				var record = new Record(new[] { "x" });
				var result = record.HasField(0);
				result.ShouldBeTrue();
			}

			[Test]
			public void Given_a_record_with_1_field_and_field__1__is_requested__should_return_false()
			{
				var record = new Record(new[] { "x" });
				var result = record.HasField(1);
				result.ShouldBeFalse();
			}
		}

		[TestFixture]
		public class When_asked_if_the_record_has_a_field_by_name
		{
			[Test]
			public void Given_a_record_with_field__a__and_field__a__is_requested__should_return_true()
			{
				var record = new Record(new[] { "x" }, new Dictionary<string, int>
					                                       {
						                                       { "a", 0 }
					                                       });
				var result = record.HasField("a");
				result.ShouldBeTrue();
			}

			[Test]
			public void Given_a_record_with_field__a__and_field__b__is_requested__should_return_false()
			{
				var record = new Record(new[] { "x" }, new Dictionary<string, int>
					                                       {
						                                       { "a", 0 }
					                                       });
				var result = record.HasField("b");
				result.ShouldBeFalse();
			}
		}
	}
}