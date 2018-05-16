using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using FluentAssert;

using JetBrains.Annotations;

using NUnit.Framework;

namespace EtlGate.Tests
{
	[UsedImplicitly]
	public class StreamTokenizerTests
	{
		[TestFixture]
		public class When_asked_to_tokenize_a_stream_with_character_tokens
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
				var random = new Random();

				for (var i = 0; i < 10000; i++)
				{
					var input = Enumerable.Range(0, 100).Select(x => characters[random.Next(characters.Length)]).ToArray();
					var commands = Enumerable.Range(0, 1000).Select(x => "hsp"[random.Next(3)]).ToArray();
					Check(input, commands);
				}
			}

			[Test]
			[ExpectedException(typeof(ArgumentException))]
			public void Given_a_null_array_of_special_characters__should_throw_an_argument_exception()
			{
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable AssignNullToNotNullAttribute
				_tokenizer.Tokenize(new MemoryStream(), (char[])null).ToList();
// ReSharper restore AssignNullToNotNullAttribute
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
			}

			[Test]
			[ExpectedException(typeof(ArgumentException))]
			public void Given_a_null_stream__should_throw_an_argument_exception()
			{
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable AssignNullToNotNullAttribute
				_tokenizer.Tokenize(null, new char[] { }).ToList();
// ReSharper restore AssignNullToNotNullAttribute
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
			}

			[Test]
			public void Given_a_stream_containing__abbbbbbbbbbb__and_specials__a__and_pushback_is_called_on_the_2nd_token__should_return__a_special__bbbbbbbbb_data__bb_data()
			{
				const string input = "abbbbbbbbbbb";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = new List<Token>();
				var tokenCount = 0;
				foreach (var token in _tokenizer.Tokenize(memoryStream, 'a'))
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
				second.Value.ShouldBeEqualTo("bbbbbbbbb");

				var third = result[2];
				third.GetType().ShouldBeEqualTo(typeof(DataToken));
				third.Value.ShouldBeEqualTo("bb");

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_a_stream_containing__abbbbbbbbbbba__and_specials__a__and_pushback_is_called_on_the_2nd_token__should_return__a_special__bbbbbbbbb_data__bb_data__a_special()
			{
				const string input = "abbbbbbbbbbba";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = new List<Token>();
				var tokenCount = 0;
				foreach (var token in _tokenizer.Tokenize(memoryStream, 'a'))
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
				result.Count.ShouldBeEqualTo(5);
				var first = result[0];
				first.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				first.Value.ShouldBeEqualTo("a");

				var second = result[1];
				second.GetType().ShouldBeEqualTo(typeof(DataToken));
				second.Value.ShouldBeEqualTo("bbbbbbbbb");

				var third = result[2];
				third.GetType().ShouldBeEqualTo(typeof(DataToken));
				third.Value.ShouldBeEqualTo("bb");

				var fourth = result[3];
				fourth.GetType().ShouldBeEqualTo(typeof(SpecialToken));
				fourth.Value.ShouldBeEqualTo("a");

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			[Test]
			public void Given_a_stream_containing__abc__and_specials__a__and_pushback_is_called_on_the_1st_token__should_return__a_special__bc_data()
			{
				const string input = "abc";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = new List<Token>();
				var tokenCount = 0;
				foreach (var token in _tokenizer.Tokenize(memoryStream, 'a'))
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
				foreach (var token in _tokenizer.Tokenize(memoryStream, 'a'))
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
				foreach (var token in _tokenizer.Tokenize(memoryStream, 'a'))
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
				foreach (var token in _tokenizer.Tokenize(memoryStream, 'a'))
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
				var result = _tokenizer.Tokenize(memoryStream, 'a').ToList();
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
				foreach (var token in _tokenizer.Tokenize(memoryStream, 'a'))
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
				foreach (var token in _tokenizer.Tokenize(memoryStream, 'a'))
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
			public void Given_a_stream_containing__eaebaebcddcacbb__and_commands__hpshpspssshhpppshpsphsh__should_capture_the_entire_stream()
			{
				const string input = "eaebaebcddcacbb";
				const string commands = "phshhsphphshshppss";
				Check(input.ToCharArray(), commands.ToCharArray());
			}

			[Test]
			public void Given_a_stream_containing__ecdeebecadbcaeecded__and_commands__hpshpspssshhpppshpsphsh__should_capture_the_entire_stream()
			{
				const string input = "ecdeebecadbcaeecded";
				const string commands = "hpshpspssshhpppshpsphshs";
				Check(input.ToCharArray(), commands.ToCharArray());
			}

			[Test]
			public void Given_a_stream_that_contains_only_1_special__should_return_the_entire_stream_as_a_special_data_token()
			{
				const string input = "h";
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _tokenizer.Tokenize(memoryStream, 'h').ToList();
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
				var input = string.Empty;
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _tokenizer.Tokenize(memoryStream, new char[] { }).ToList();
				result.Count.ShouldBeEqualTo(1);

				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
			}

			private void Check(char[] input, char[] commands)
			{
				var specials = new[] { 'a' };
				var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));

				var saved = new StringBuilder();
				var holding = new StringBuilder();
				var commandIndex = 0;
				try
				{
					foreach (var token in _tokenizer.Tokenize(memoryStream, specials))
					{
						if (token is DataToken)
						{
							token.Value.IndexOfAny(specials).ShouldBeEqualTo(-1);
						}
						switch (commands[commandIndex++])
						{
							case 'h': // hold
								holding.Append(token.Value);
								break;
							case 's': // save
								if (holding.Length > 0)
								{
									saved.Append(holding);
									holding.Length = 0;
								}
								holding.Append(token.Value);
								if (saved.Length > 0)
								{
									saved.ToString().ShouldBeEqualTo(new string(input).Substring(0, saved.Length));
								}
								break;
							case 'p': // pushback
								holding.Append(token.Value);
								_tokenizer.PushBack(holding);
								holding.Length = 0;
								break;
						}
					}
				}
				catch (Exception exception)
				{
					Console.WriteLine("input: " + new string(input).Substring(0, Math.Min(input.Length, saved.Length + holding.Length + 10)));
					Console.WriteLine("commands: " + new string(commands).Substring(0, Math.Min(commands.Length, commandIndex + 10)));
					Console.WriteLine(exception);
					Assert.Fail(exception.Message);
				}
				saved.Append(holding);
				var expected = new string(input);
				saved.ToString().ShouldBeEqualTo(expected);
			}
		}

		[TestFixture]
		public class When_asked_to_tokenize_a_stream_with_string_tokens
		{
			private Random _random;
			private IStreamTokenizer _tokenizer;

			[TestFixtureSetUp]
			public void Before_first_test()
			{
				_tokenizer = new StreamTokenizer();
				_random = new Random();
			}

			[Test]
			public void FuzzTestIt()
			{
				var anyBreakage = false;
				const string values = "abcde";
				for (var i = 0; i < 10000; i++)
				{
					var input = GetRandomString(10, values);

					var specials = Enumerable.Range(0, _random.Next(3, 10))
					                         .Select(x => GetRandomString(_random.Next(1, 5), values))
					                         .Distinct()
					                         .ToArray();

					var expect = GetExpected(input, specials).Where(x => !(x is EndOfStreamToken)).ToList();
					try
					{
						Check(input, specials, expect);
					}
					catch (Exception e)
					{
						anyBreakage = true;
						Console.WriteLine("const string input = \"" + input + "\";");
						Console.WriteLine("var specialTokens = new[] { \"" + string.Join("\", \"", specials) + "\" };");
						Console.WriteLine("var expected = new Token[] { " + string.Join(", ", expect.Select(x => "new " + x.GetType() + "( \"" + x.Value + "\")")) + " };");
						Console.WriteLine("exception: " + e.Message);
					}
				}
				anyBreakage.ShouldBeFalse();
			}

			[Test]
			[ExpectedException(typeof(ArgumentException))]
			public void Given_a_null_array_of_special_strings__should_throw_an_argument_exception()
			{
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable AssignNullToNotNullAttribute
				_tokenizer.Tokenize(new MemoryStream(), (string[])null).ToList();
// ReSharper restore AssignNullToNotNullAttribute
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
			}

			[Test]
			public void Given_stream_containing__aaa__and_special_tokens__a__should_return_tokens__a_Special__a_Special__a_Special()
			{
				const string input = "aaa";
				var specialTokens = new[] { "a" };
				var expected = new Token[]
					               {
						               new SpecialToken("a"),
						               new SpecialToken("a"),
						               new SpecialToken("a")
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__aaa__and_special_tokens__aa__ab__should_return_tokens__aa_Special__a_Data()
			{
				const string input = "aaa";
				var specialTokens = new[] { "aa", "ab" };
				var expected = new Token[]
					               {
						               new SpecialToken("aa"),
						               new DataToken("a")
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__aaa__and_special_tokens__aa__should_return_tokens__aa_Special__a_Data()
			{
				const string input = "aaa";
				var specialTokens = new[] { "aa" };
				var expected = new Token[]
					               {
						               new SpecialToken("aa"),
						               new DataToken("a")
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__ababc__and_special_tokens__a__aba__ababc__should_return_tokens__a_Special__b_Data__a_Special__bc_Data()
			{
				const string input = "ababc";
				var specialTokens = new[] { "a", "aba", "ababc" };
				var expected = new Token[]
					               {
						               new SpecialToken("ababc")
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__ababc__and_special_tokens__aa__ab__abc__should_return_tokens__ab_Special__ab_Special__c_Data()
			{
				const string input = "ababc";
				var specialTokens = new[] { "aa", "ab", "abc" };
				var expected = new Token[]
					               {
						               new SpecialToken("ab"),
						               new SpecialToken("abc")
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__abe__and_special_tokens__beab__a__eaec__b__should_return_tokens__a_Special__b_Special__e_Data()
			{
				const string input = "abe";
				var specialTokens = new[] { "beab", "a", "eaec", "b" };
				var expected = new Token[]
					               {
						               new SpecialToken("a"),
						               new SpecialToken("b"),
						               new DataToken("e")
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__bcabb__and_special_tokens__b__babe__a__should_return_tokens__b_Special__c_Data__a_Special()
			{
				const string input = "bca";
				var specialTokens = new[] { "b", "babe", "a" };
				var expected = new Token[]
					               {
						               new SpecialToken("b"),
						               new DataToken("c"),
						               new SpecialToken("a")
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__bddc__and_special_tokens__bc_d_dd_bdbc__should_return_tokens__b_Data__d_Special__d_Special__c_Data()
			{
				const string input = "bddc";
				var specialTokens = new[] { "bc", "d", "dd", "bdbc" };
				var expected = new Token[]
					               {
						               new DataToken("b"),
						               new SpecialToken("dd"),
						               new DataToken("c")
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__beabeedaed__and_special_tokens__eab__eb__bbe__eaad__should_return_tokens__b_Data__eab_Special__eadaed_Data()
			{
				const string input = "beabeedaed";
				var specialTokens = new[] { "eab", "eb", "bbe", "eaad" };
				var expected = new Token[]
					               {
						               new DataToken("b"),
						               new SpecialToken("eab"),
						               new DataToken("e"),
						               new DataToken("e"),
						               new DataToken("d"),
						               new DataToken("a"),
						               new DataToken("e"),
						               new DataToken("d")
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__beae__and_special_tokens__bb__e__ea__should_return_tokens__b_Data__e_Special__a_Data__e_Special()
			{
				const string input = "beae";
				var specialTokens = new[] { "bb", "e", "ea" };
				var expected = new Token[]
					               {
						               new DataToken("b"),
						               new SpecialToken("ea"),
						               new SpecialToken("e")
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__beeeb__and_special_tokens__b_e_eeb__should_return_tokens__b_Special__e_Special__eeb_Specia()
			{
				const string input = "beeeb";
				var specialTokens = new[] { "b", "e", "eeb" };
				var expected = new Token[]
					               {
						               new SpecialToken("b"),
						               new SpecialToken("e"),
						               new SpecialToken("eeb")
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__cdbdbbdbdc__and_special_tokens__d__cb__a__bdc__should_return_tokens__c_Data__d_Special__b_Data__d_Special__bb_Data__d_Special__bdc_Special()
			{
				const string input = "cdbdbbdbdc";
				var specialTokens = new[] { "d", "cb", "a", "bdc" };
				var expected = new Token[]
					               {
						               new DataToken("c"),
						               new SpecialToken("d"),
						               new DataToken("b"),
						               new SpecialToken("d"),
						               new DataToken("b"),
						               new DataToken("b"),
						               new SpecialToken("d"),
						               new SpecialToken("bdc")
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__cdbeaebaba__and_special_tokens__acdc__e__should_return_tokens__cdb_Data__e_Special__a_Data__e_Special__baba_Data()
			{
				const string input = "cdbeaebaba";
				var specialTokens = new[] { "acdc", "e" };
				var expected = new Token[]
					               {
						               new DataToken("c"),
						               new DataToken("d"),
						               new DataToken("b"),
						               new SpecialToken("e"),
						               new DataToken("a"),
						               new SpecialToken("e"),
						               new DataToken("b"),
						               new DataToken("a"),
						               new DataToken("b"),
						               new DataToken("a")
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__ceaada__and_special_tokens__ea_c_e_cab__should_return_tokens__c_Special__e_Special__aada_Data()
			{
				const string input = "ceaada";
				var specialTokens = new[] { "ea", "c", "e", "cab" };
				var expected = new Token[]
					               {
						               new SpecialToken("c"),
						               new SpecialToken("ea"),
						               new DataToken("a"),
						               new DataToken("d"),
						               new DataToken("a")
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__daebbcaabd__and_special_tokens__cacd__d__ded__cab__a__b__ea__cbc__should_return_tokens__d_Special__a_Special__e_Data__b_Special__b_Special__c_Data__a_Special__a_Special__b_Special__d_Special()
			{
				const string input = "daebbcaabd";
				var specialTokens = new[] { "cacd", "d", "ded", "cab", "a", "b", "ea", "cbc" };
				var expected = new Token[]
					               {
						               new SpecialToken("d"),
						               new SpecialToken("a"),
						               new DataToken("e"),
						               new SpecialToken("b"),
						               new SpecialToken("b"),
						               new DataToken("c"),
						               new SpecialToken("a"),
						               new SpecialToken("a"),
						               new SpecialToken("b"),
						               new SpecialToken("d")
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__dcad__and_special_tokens__ad_dcac_c_a__should_return_tokens__d_Data__c_Special__a_Special__d_Special()
			{
				const string input = "dcad";
				var specialTokens = new[] { "ad", "dcac", "c", "a" };
				var expected = new Token[]
					               {
						               new DataToken("d"),
						               new SpecialToken("c"),
						               new SpecialToken("ad")
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__ebebbb__and_special_tokens__ebe__should_return_tokens__cdb_Data__ebe_Special__bbb_Data()
			{
				const string input = "ebebbb";
				var specialTokens = new[] { "ebe" };
				var expected = new Token[]
					               {
						               new SpecialToken("ebe"),
						               new DataToken("b"),
						               new DataToken("b"),
						               new DataToken("b")
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__edbb__and_special_tokens__bb_ecc_dc_b_bd_d__should_return_tokens__e_Data__d_Special__b_Special__b_Special()
			{
				const string input = "edbb";
				var specialTokens = new[] { "bb", "ecc", "dc", "b", "bd", "d" };
				var expected = new Token[]
					               {
						               new DataToken("e"),
						               new SpecialToken("d"),
						               new SpecialToken("bb")
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void TestGetExpected_1()
			{
				const string input = "cdbdbbdbdc";
				var specialTokens = new[] { "d", "aad", "cb", "a", "ebbb", "bdc", "ebb" };
				var expected = new Token[]
					               {
						               new DataToken("c"),
						               new SpecialToken("d"),
						               new DataToken("b"),
						               new SpecialToken("d"),
						               new DataToken("b"),
						               new DataToken("b"),
						               new SpecialToken("d"),
						               new SpecialToken("bdc")
					               };
				var result = GetExpected(input, specialTokens).ToList();
				CompareResultWithExpected(result, expected);
			}

			[Test]
			public void TestGetExpected_2()
			{
				const string input = "beeaeddabc";
				var specialTokens = new[] { "eeba", "eeaa", "abad", "c", "cd" };
				var expected = new Token[]
					               {
						               new DataToken("b"),
						               new DataToken("e"),
						               new DataToken("e"),
						               new DataToken("a"),
						               new DataToken("e"),
						               new DataToken("d"),
						               new DataToken("d"),
						               new DataToken("a"),
						               new DataToken("b"),
						               new SpecialToken("c")
					               };
				var result = GetExpected(input, specialTokens).ToList();
				CompareResultWithExpected(result, expected);
			}

			[Test]
			public void TestGetExpected_3()
			{
				const string input = "cccbdacdce";
				var specialTokens = new[] { "a", "ccc", "bead" };
				var expected = new Token[]
					               {
						               new SpecialToken("ccc"),
						               new DataToken("b"),
						               new DataToken("d"),
						               new SpecialToken("a"),
						               new DataToken("c"),
						               new DataToken("d"),
						               new DataToken("c"),
						               new DataToken("e")
					               };

				var result = GetExpected(input, specialTokens).ToList();
				CompareResultWithExpected(result, expected);
			}

			[Test]
			public void TestGetExpected_4()
			{
				const string input = "bbaccedbde";
				var specialTokens = new[] { "cea", "eded", "bcb" };
				var expected = new[]
					               {
						               new DataToken("b"),
						               new DataToken("b"),
						               new DataToken("a"),
						               new DataToken("c"),
						               new DataToken("c"),
						               new DataToken("e"),
						               new DataToken("d"),
						               new DataToken("b"),
						               new DataToken("d"),
						               new DataToken("e")
					               };

				var result = GetExpected(input, specialTokens).ToList();
				CompareResultWithExpected(result, expected);
			}

			[Test]
			public void TestGetExpected_5()
			{
				const string input = "cdbeaebaba";
				var specialTokens = new[] { "acdc", "e" };
				var expected = new Token[]
					               {
						               new DataToken("c"),
						               new DataToken("d"),
						               new DataToken("b"),
						               new SpecialToken("e"),
						               new DataToken("a"),
						               new SpecialToken("e"),
						               new DataToken("b"),
						               new DataToken("a"),
						               new DataToken("b"),
						               new DataToken("a")
					               };

				var result = GetExpected(input, specialTokens).ToList();
				CompareResultWithExpected(result, expected);
			}

			[Test]
			public void TestGetExpected_6()
			{
				const string input = "aeabbbdaeb";
				var specialTokens = new[] { "eeda", "ea", "eabb", "eed" };
				var expected = new Token[]
					               {
						               new DataToken("a"),
						               new SpecialToken("eabb"),
						               new DataToken("b"),
						               new DataToken("d"),
						               new DataToken("a"),
						               new DataToken("e"),
						               new DataToken("b")
					               };
				var result = GetExpected(input, specialTokens).ToList();
				CompareResultWithExpected(result, expected);
			}

			private void Check(string input, string[] specialTokens, IList<Token> expect)
			{
				var stream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _tokenizer.Tokenize(stream, specialTokens).ToList();
				CompareResultWithExpected(result, expect);
			}

			private static void CompareResultWithExpected(IList<Token> result, IList<Token> expect)
			{
				result.Count.ShouldBeEqualTo(expect.Count + 1, "expected " + (1 + expect.Count) + " tokens but received " + result.Count + ": \"" + string.Join("\", \"", result.Select(x => x.Value).ToArray()) + "\"");
				result.Last().GetType().ShouldBeEqualTo(typeof(EndOfStreamToken));
				for (var i = 0; i < result.Count - 1; i++)
				{
					var actual = result[i];
					var expected = expect[i];
					actual.GetType().ShouldBeEqualTo(expected.GetType(), "Token " + (1 + i) + " had token type " + actual.GetType() + " but should have been " + expected.GetType());
					actual.Value.ShouldBeEqualTo(expected.Value, "Token " + (1 + i) + " had value '" + actual.Value + "' but should have been: " + expected.Value);
				}
			}

			private static IEnumerable<Token> GetExpected(string input, IList<string> specials)
			{
				var data = new StringBuilder();
				var specialsOrderedByLengthDescending = specials.OrderByDescending(x => x.Length).ToList();
				var specialCharacters = new HashSet<char>(specials.SelectMany(x => x));
				while (input.Length > 0)
				{
					var match = specialsOrderedByLengthDescending.FirstOrDefault(input.StartsWith);
					if (match != null)
					{
						if (data.Length > 0)
						{
							yield return new DataToken(data.ToString());
							data = new StringBuilder();
						}
						yield return new SpecialToken(match);
						input = input.Substring(match.Length);
					}
					else
					{
						var firstChar = input[0];
						if (specialCharacters.Contains(firstChar))
						{
							if (data.Length > 0)
							{
								yield return new DataToken(data.ToString());
								data.Clear();
							}
							yield return new DataToken(firstChar.ToString(CultureInfo.InvariantCulture));
						}
						else
						{
							data.Append(firstChar);
						}
						input = input.Substring(1);
					}
				}
				if (data.Length > 0)
				{
					yield return new DataToken(data.ToString());
				}
				yield return new EndOfStreamToken();
			}

			private string GetRandomString(int length, string values)
			{
				var input = new StringBuilder();
				for (var j = 0; j < length; j++)
				{
					var index = _random.Next(values.Length);
					input.Append(values[index]);
				}
				return input.ToString();
			}
		}
	}
}