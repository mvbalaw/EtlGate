using System;
using System.Collections.Generic;

using FluentAssert;

using JetBrains.Annotations;

using NUnit.Framework;

namespace EtlGate.Tests
{
	[UsedImplicitly]
	public class RecordTests
	{
		[TestFixture]
		public class When_asked_if_the_Record_has_a_value_for_a_field
		{
			[Test]
			public void Given_a_field_name_and_the_field_exists_and_is_not_null__should_return_true()
			{
				var record = new Record(new[] { "1", "2" }, "a", "b");
				var result = record.HasValueForField("a");
				result.ShouldBeTrue();
			}

			[Test]
			public void Given_a_field_name_and_the_field_exists_and_is_null__should_return_false()
			{
				var record = new Record(new[] { null, "2" }, "a", "b");
				var result = record.HasValueForField("a");
				result.ShouldBeFalse();
			}

			[Test]
			public void Given_a_field_name_and_the_field_does_not_exist_in_the_record__should_return_false()
			{
				var record = new Record(new[] { null, "2" }, "a", "b");
				var result = record.HasValueForField("c");
				result.ShouldBeFalse();
			}
		}

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
			public void Given_the_record_does_not_contain_a_value_for_the_heading__should_return_null()
			{
				var record = new Record(new string[] { }, "a");
				var field = record.GetField("a");
				field.ShouldBeNull();
			}

			[Test]
			public void Given_a_Type_to_which_to_cast_the_value_that_matches_the_type_of_the_value__Should_return_the_value_cast_to_the_requested_type()
			{
				var record = Record.For(new object[] { 1 }, "a");
				var field = record.GetField<int>("a");
				field.ShouldBeEqualTo(1);
			}

			[Test]
			public void Given_a_reference_Type_to_which_to_cast_the_value_and_the_value_is_null___Should_return_the_value_cast_to_the_requested_type()
			{
				var record = Record.For(new object[] { null }, "a");
				var field = record.GetField<string>("a");
				field.ShouldBeNull();
			}

			[Test]
			[ExpectedException(typeof(ArgumentException), ExpectedMessage = "a" + Record.ErrorFieldNameIsNotAValidHeaderForThisRecordMessage + "\r\n" + "Parameter name: name")]
			public void Given_a_reference_Type_to_which_to_cast_the_value_and_the_requested_field_does_not_exist__should_throw_an_exception()
			{
				var record = new Record(new string[] { }, new Dictionary<string, int>());
				// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
				record.GetField<string>("a");
			}

			[Test]
			[ExpectedException(typeof(ArgumentException), ExpectedMessage = "a" + Record.ErrorFieldNameIsNotAValidHeaderForThisRecordMessage + "\r\n" + "Parameter name: name")]
			public void Given_a_value_Type_to_which_to_cast_the_value_and_the_requested_field_does_not_exist__should_throw_an_exception()
			{
				var record = new Record(new string[] { }, new Dictionary<string, int>());
				// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
				record.GetField<int>("a");
			}

			[Test]
			public void Given_a_value_Type_to_which_to_cast_the_value_and_the_value_is_null___Should_return_the_default_value_of_the_requested_type()
			{
				var record = Record.For(new object[] { null }, "a");
				var field = record.GetField<KeyValuePair<int, int>>("a");
				field.ShouldBeEqualTo(new KeyValuePair<int, int>());
			}

			[Test]
			[ExpectedException(typeof(InvalidCastException), ExpectedMessage = "Field 'a' value cannot be cast to Int32: 1.12")]
			public void Given_a_Type_to_which_to_cast_the_value_that_does_not_match_the_type_of_the_value__Should_throw_an_exception()
			{
				var record = Record.For(new object[] { 1.12 }, "a");
				// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
				record.GetField<int>("a");
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
			[ExpectedException(typeof(ArgumentException), ExpectedMessage = "10" + Record.ErrorFieldNumberIsNotAValidFieldForThisRecordMessage)]
			public void Given_index_value_exceeds_available_number_of_fields__should_throw_an_exception()
			{
				var record = new Record(new string[] { }, "a", "b");
				// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
				record.GetField(10);
			}

			[Test]
			public void Given_the_record_does_not_contain_a_value_for_the_heading__should_return_null()
			{
				var record = new Record(new string[] { null }, new Dictionary<string, int>());
				var field = record.GetField(0);
				field.ShouldBeNull();
			}

			[Test]
			public void Given_a_Type_to_which_to_cast_the_value_that_matches_the_type_of_the_value__Should_return_the_value_cast_to_the_requested_type()
			{
				var record = Record.For(new object[] { 1, 2, 3 });
				var field = record.GetField<int>(0);
				field.ShouldBeEqualTo(1);
			}

			[Test]
			public void Given_a_reference_Type_to_which_to_cast_the_value_and_the_value_is_null___Should_return_the_value_cast_to_the_requested_type()
			{
				var record = Record.For(new object[] { null, 2, 3 });
				var field = record.GetField<string>(0);
				field.ShouldBeNull();
			}

			[Test]
			[ExpectedException(typeof(ArgumentException), ExpectedMessage = "10"+Record.ErrorFieldNumberIsNotAValidFieldForThisRecordMessage)]
			public void Given_a_reference_Type_to_which_to_cast_the_value_and_the_index_value_exceeds_available_number_of_fields__should_throw_an_exception()
			{
				var record = new Record(new string[] { }, new Dictionary<string, int>());
				// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
				record.GetField<string>(10);
			}

			[Test]
			public void Given_a_value_Type_to_which_to_cast_the_value_and_the_value_is_null___Should_return_the_default_value_of_the_requested_type()
			{
				var record = Record.For(new object[] { null, 2, 3 });
				var field = record.GetField<KeyValuePair<int, int>>(0);
				field.ShouldBeEqualTo(new KeyValuePair<int, int>());
			}

			[Test]
			[ExpectedException(typeof(ArgumentException), ExpectedMessage = "10" + Record.ErrorFieldNumberIsNotAValidFieldForThisRecordMessage)]
			public void Given_a_value_Type_to_which_to_cast_the_value_and_the_index_value_exceeds_available_number_of_fields__should_throw_an_exception()
			{
				var record = new Record(new string[] { }, new Dictionary<string, int>());
				var field = record.GetField<int>(10);
				field.ShouldBeEqualTo(0);
			}

			[Test]
			[ExpectedException(typeof(InvalidCastException), ExpectedMessage = "Field 0 value cannot be cast to Int32: 1.12")]
			public void Given_a_Type_to_which_to_cast_the_value_that_does_not_match_the_value_type__Should_throw_an_exception()
			{
				var record = Record.For(new object[] { 1.12m });
				// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
				record.GetField<int>(0);
			}
		}

		[TestFixture]
		public class When_asked_if_the_record_has_a_field_by_index
		{
			[Test]
			[ExpectedException(typeof(ArgumentOutOfRangeException), ExpectedMessage = Record.ErrorFieldIndexMustBeNonNegativeMessage + "\r\nParameter name: zeroBasedIndex")]
			public void Given_a_negative_field_number_is_requested__should_throw_ArgumentOutOfRangeException()
			{
				var record = new Record("x");
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
				record.HasField(-1);
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
			}

			[Test]
			public void Given_a_record_with_1_field_and_field__0__is_requested__should_return_true()
			{
				var record = new Record("x");
				var result = record.HasField(0);
				result.ShouldBeTrue();
			}

			[Test]
			public void Given_a_record_with_1_field_and_field__1__is_requested__should_return_false()
			{
				var record = new Record("x");
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