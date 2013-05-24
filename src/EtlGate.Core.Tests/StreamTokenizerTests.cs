using System;
using System.Collections.Generic;
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
				_tokenizer = new StreamTokenizer
					             {
						             ReadBufferSize = 10
					             };
			}

			[Test]
			public void FuzzTestIt()
			{
				const string characters = "abcde";
				var specials = new[] { 'a' };
				var random = new Random();

				for (var i = 0; i < 10000; i++)
				{
					var input = Enumerable.Range(0, 100).Select(x => characters[random.Next(characters.Length)]).ToArray();
					var commands = Enumerable.Range(0, 1000).Select(x => "hsp"[random.Next(3)]).ToArray();
					var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
					var saved = new StringBuilder();
					var holding = new StringBuilder();
					var command = 0;
					foreach (var token in _tokenizer.Tokenize(memoryStream, specials))
					{
						if (token is DataToken)
						{
							token.Value.IndexOfAny(specials).ShouldBeEqualTo(-1);
						}
						switch (commands[command++])
						{
							case 'h': // hold
								holding.Append(token.Value);
								break;
							case 's': // save
								saved.Append(holding);
								holding.Length = 0;
								holding.Append(token.Value);
								break;
							case 'p': // pushback
								holding.Append(token.Value);
								_tokenizer.PushBack(holding.ToString().ToCharArray());
								holding.Length = 0;
								break;
						}
					}
					saved.Append(holding);
					var expected = new String(input);
					saved.ToString().ShouldBeEqualTo(expected);
				}
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
			public void Given_a_stream_containing__abbbbbbbbbbb__and_specials__a__and_pushback_is_called_on_the_2nd_token__should_return__a_special__bbbbbbbbbbb_data()
			{
				const string input = "abbbbbbbbbbb";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = new List<Token>();
				var tokenCount = 0;
				foreach (var token in _tokenizer.Tokenize(memoryStream, new[] { 'a' }))
				{
					if (tokenCount == 1)
					{
						_tokenizer.PushBack(token.Value.ToCharArray());
					}
					else
					{
						result.Add(token);
					}
					tokenCount++;
				}
				result.Count.ShouldBeEqualTo(3);
				var first = result[0];
				first.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				first.Value.ShouldBeEqualTo("a");

				var second = result[1];
				second.GetType().ShouldBeEqualTo(typeof(DataToken));
				second.Value.ShouldBeEqualTo("bbbbbbbbbbb");

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_a_stream_containing__abbbbbbbbbbba__and_specials__a__and_pushback_is_called_on_the_2nd_token__should_return__a_special__bbbbbbbbbbb_data__a_special()
			{
				const string input = "abbbbbbbbbbba";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = new List<Token>();
				var tokenCount = 0;
				foreach (var token in _tokenizer.Tokenize(memoryStream, new[] { 'a' }))
				{
					if (tokenCount == 1)
					{
						_tokenizer.PushBack(token.Value.ToCharArray());
					}
					else
					{
						result.Add(token);
					}
					tokenCount++;
				}
				result.Count.ShouldBeEqualTo(4);
				var first = result[0];
				first.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				first.Value.ShouldBeEqualTo("a");

				var second = result[1];
				second.GetType().ShouldBeEqualTo(typeof(DataToken));
				second.Value.ShouldBeEqualTo("bbbbbbbbbbb");

				var third = result[2];
				third.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				third.Value.ShouldBeEqualTo("a");

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_a_stream_containing__abc__and_specials__a__and_pushback_is_called_on_the_1st_token__should_return__a_special__bc_data()
			{
				const string input = "abc";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = new List<Token>();
				var tokenCount = 0;
				foreach (var token in _tokenizer.Tokenize(memoryStream, new[] { 'a' }))
				{
					if (tokenCount == 0)
					{
						_tokenizer.PushBack(token.Value.ToCharArray());
					}
					else
					{
						result.Add(token);
					}
					tokenCount++;
				}
				result.Count.ShouldBeEqualTo(3);
				var first = result[0];
				first.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				first.Value.ShouldBeEqualTo("a");

				var second = result[1];
				second.GetType().ShouldBeEqualTo(typeof(DataToken));
				second.Value.ShouldBeEqualTo("bc");

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_a_stream_containing__abc__and_specials__a__and_pushback_is_called_on_the_2nd_token__should_return__a_special__bc_data()
			{
				const string input = "abc";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = new List<Token>();
				var tokenCount = 0;
				foreach (var token in _tokenizer.Tokenize(memoryStream, new[] { 'a' }))
				{
					if (tokenCount == 1)
					{
						_tokenizer.PushBack(token.Value.ToCharArray());
					}
					else
					{
						result.Add(token);
					}
					tokenCount++;
				}
				result.Count.ShouldBeEqualTo(3);
				var first = result[0];
				first.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				first.Value.ShouldBeEqualTo("a");

				var second = result[1];
				second.GetType().ShouldBeEqualTo(typeof(DataToken));
				second.Value.ShouldBeEqualTo("bc");

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_a_stream_containing__abc__and_specials__a__and_pushback_is_called_on_the_EndOfStreamToken__should_return__a_special__bc_data()
			{
				const string input = "abc";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = new List<Token>();
				var tokenCount = 0;
				foreach (var token in _tokenizer.Tokenize(memoryStream, new[] { 'a' }))
				{
					if (tokenCount == 2)
					{
						result.Clear();
						_tokenizer.PushBack(input.ToCharArray());
					}
					else
					{
						result.Add(token);
					}
					tokenCount++;
				}
				result.Count.ShouldBeEqualTo(3);
				var first = result[0];
				first.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				first.Value.ShouldBeEqualTo("a");

				var second = result[1];
				second.GetType().ShouldBeEqualTo(typeof(DataToken));
				second.Value.ShouldBeEqualTo("bc");

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_a_stream_containing__abc__and_specials__a__and_pushback_is_called_with__abc__should_return__a_special__bc_data()
			{
				const string input = "abc";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = new List<Token>();
				var tokenCount = 0;
				foreach (var token in _tokenizer.Tokenize(memoryStream, new[] { 'a' }))
				{
					if (tokenCount == 1)
					{
						_tokenizer.PushBack(input.ToCharArray());
						result.Clear();
					}
					else
					{
						result.Add(token);
					}
					tokenCount++;
				}
				result.Count.ShouldBeEqualTo(3);
				var first = result[0];
				first.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				first.Value.ShouldBeEqualTo("a");

				var second = result[1];
				second.GetType().ShouldBeEqualTo(typeof(DataToken));
				second.Value.ShouldBeEqualTo("bc");

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_a_stream_containing__abc__and_specials__a__should_return__a_special__bc_data()
			{
				const string input = "abc";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _tokenizer.Tokenize(memoryStream, new[] { 'a' }).ToList();
				result.Count.ShouldBeEqualTo(3);
				var first = result[0];
				first.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				first.Value.ShouldBeEqualTo("a");

				var second = result[1];
				second.GetType().ShouldBeEqualTo(typeof(DataToken));
				second.Value.ShouldBeEqualTo("bc");

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_a_stream_containing__abca__and_specials__a__and_pushback_is_called_on_the_2nd_token__should_return__a_special__bc_data__a_special()
			{
				const string input = "abca";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = new List<Token>();
				var tokenCount = 0;
				foreach (var token in _tokenizer.Tokenize(memoryStream, new[] { 'a' }))
				{
					if (tokenCount == 1)
					{
						_tokenizer.PushBack(token.Value.ToCharArray());
					}
					else
					{
						result.Add(token);
					}
					tokenCount++;
				}
				result.Count.ShouldBeEqualTo(4);
				var first = result[0];
				first.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				first.Value.ShouldBeEqualTo("a");

				var second = result[1];
				second.GetType().ShouldBeEqualTo(typeof(DataToken));
				second.Value.ShouldBeEqualTo("bc");

				var third = result[2];
				third.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				third.Value.ShouldBeEqualTo("a");

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_a_stream_containing__abca__and_specials__a__and_pushback_is_called_on_the_3rd_token__should_return__a_special__bc_data__a_special()
			{
				const string input = "abca";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = new List<Token>();
				var tokenCount = 0;
				foreach (var token in _tokenizer.Tokenize(memoryStream, new[] { 'a' }))
				{
					if (tokenCount == 2)
					{
						_tokenizer.PushBack(token.Value.ToCharArray());
					}
					else
					{
						result.Add(token);
					}
					tokenCount++;
				}
				result.Count.ShouldBeEqualTo(4);
				var first = result[0];
				first.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				first.Value.ShouldBeEqualTo("a");

				var second = result[1];
				second.GetType().ShouldBeEqualTo(typeof(DataToken));
				second.Value.ShouldBeEqualTo("bc");

				var third = result[2];
				third.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				third.Value.ShouldBeEqualTo("a");

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_a_stream_that_contains_only_1_special__should_return_the_entire_stream_as_a_special_data_token()
			{
				const string input = "h";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _tokenizer.Tokenize(memoryStream, new[] { 'h' }).ToList();
				result.Count.ShouldBeEqualTo(2);
				var token = result.First();
				token.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				token.Value.ShouldBeEqualTo(input);

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_an_empty_array_of_specials__should_return_the_entire_stream_as_a_single_data_token()
			{
				const string input = "hello";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _tokenizer.Tokenize(memoryStream, new char[] { }).ToList();
				result.Count.ShouldBeEqualTo(2);
				var token = result.First();
				token.GetType().ShouldBeEqualTo(typeof(DataToken));
				token.Value.ShouldBeEqualTo(input);

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_an_empty_stream__should_return_an_empty_IEnumerable()
			{
				var input = String.Empty;
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _tokenizer.Tokenize(memoryStream, new char[] { }).ToList();
				result.Count.ShouldBeEqualTo(1);

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}
		}
	}
}