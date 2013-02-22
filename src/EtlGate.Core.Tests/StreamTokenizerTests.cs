using System;
using System.IO;
using System.Linq;
using System.Text;

using FluentAssert;

using NUnit.Framework;

namespace EtlGate.Core.Tests
{
// ReSharper disable ClassNeverInstantiated.Global
	public class StreamTokenizerTests
// ReSharper restore ClassNeverInstantiated.Global
	{
		[TestFixture]
		public class When_asked_to_tokenize_a_stream
		{
			private StreamTokenizer _tokenizer;

			[SetUp]
			public void Before_each_test()
			{
				_tokenizer = new StreamTokenizer();
			}

			[Test]
			[ExpectedException(typeof(ArgumentException))]
			public void Given_a_null_array_of_specials__should_throw_an_argument_exception()
			{
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
				_tokenizer.Tokenize(new MemoryStream(), null).ToList();
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
			}

			[Test]
			[ExpectedException(typeof(ArgumentException))]
			public void Given_a_null_stream__should_throw_an_argument_exception()
			{
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
				_tokenizer.Tokenize(null, new char[] { }).ToList();
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
			}

			[Test]
			public void Given_a_stream_containing__abc__and_specials__a__should_return__a_special__bc_data()
			{
				const string input = "abc";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _tokenizer.Tokenize(memoryStream, new[] { 'a' }).ToList();
				result.Count.ShouldBeEqualTo(2);
				var first = result.First();
				first.TokenType.ShouldBeEqualTo(TokenType.Special);
				first.Value.ShouldBeEqualTo("a");
				var second = result.Last();
				second.TokenType.ShouldBeEqualTo(TokenType.Data);
				second.Value.ShouldBeEqualTo("bc");
			}

			[Test]
			public void Given_a_stream_that_contains_only_1_special__should_return_the_entire_stream_as_a_special_data_token()
			{
				const string input = "h";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _tokenizer.Tokenize(memoryStream, new[] { 'h' }).ToList();
				result.Count.ShouldBeEqualTo(1);
				var token = result.First();
				token.TokenType.ShouldBeEqualTo(TokenType.Special);
				token.Value.ShouldBeEqualTo(input);
			}

			[Test]
			public void Given_an_empty_array_of_specials__should_return_the_entire_stream_as_a_single_data_token()
			{
				const string input = "hello";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _tokenizer.Tokenize(memoryStream, new char[] { }).ToList();
				result.Count.ShouldBeEqualTo(1);
				var token = result.First();
				token.TokenType.ShouldBeEqualTo(TokenType.Data);
				token.Value.ShouldBeEqualTo(input);
			}

			[Test]
			public void Given_an_empty_stream__should_return_an_empty_IEnumerable()
			{
				var input = String.Empty;
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _tokenizer.Tokenize(memoryStream, new char[] { }).ToList();
				result.Count.ShouldBeEqualTo(0);
			}
		}
	}
}