using NUnit.Framework;

using FluentAssert;

namespace EtlGate.Tests
{
	[TestFixture]
	public class CsvTests
	{
		 [Test]
		 public void Given_an_input_with_no_specials__should_return_the_input()
		 {
			 const string input = "abc";
			 var escaped = Csv.Escape(input);
			 escaped.ShouldBeEqualTo(input);
		 }

		 [Test]
		 public void Given_an_input_containing_a_DOUBLE_QUOTE__should_escape_the_double_quote_and_surround_the_input_with_double_quotes()
		 {
			 const string input = "ab\"c";
			 var escaped = Csv.Escape(input);
			 escaped.ShouldBeEqualTo("\"ab\"\"c\"");
		 }

		 [Test]
		 public void Given_an_input_containing_a_COMMA__should_surround_the_input_with_double_quotes()
		 {
			 const string input = "ab,c";
			 var escaped = Csv.Escape(input);
			 escaped.ShouldBeEqualTo("\"ab,c\"");
		 }

		 [Test]
		 public void Given_an_input_containing_a_NEWLINE__should_surround_the_input_with_double_quotes()
		 {
			 const string input = "ab\nc";
			 var escaped = Csv.Escape(input);
			 escaped.ShouldBeEqualTo("\"ab\nc\"");
		 }

		 [Test]
		 public void Given_an_input_containing_a_RETURN__should_surround_the_input_with_double_quotes()
		 {
			 const string input = "ab\rc";
			 var escaped = Csv.Escape(input);
			 escaped.ShouldBeEqualTo("\"ab\rc\"");
		 }
	}
}