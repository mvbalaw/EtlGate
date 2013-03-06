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
	public class TokenMergerTests
// ReSharper restore ClassNeverInstantiated.Global
	{
		[TestFixture]
		public class When_asked_to_merge_tokens
		{
			private TokenMerger _merger;
			private Random _random;
			private StreamTokenizer _tokenizer;

			[TestFixtureSetUp]
			public void Before_first_test()
			{
				_merger = new TokenMerger();
				_tokenizer = new StreamTokenizer();
				_random = new Random();
			}

			[Test]
			public void FuzzTestIt()
			{
				var anyBreakage = false;
				const string values = "abcde";
				for (var i = 0; i < 1000; i++)
				{
					var input = GetRandomString(10, values);

					var specials = Enumerable
						.Range(0, _random.Next(3, 10))
						.Select(x => GetRandomString(_random.Next(1, 5), values))
						.Distinct()
						.ToArray();

					var expect = GetExpected(input, specials).ToList();
					try
					{
						Check(input, specials, expect);
					}
					catch (Exception e)
					{
						anyBreakage = true;
						Console.WriteLine("const string input = \"" + input + "\";");
						Console.WriteLine("var specialTokens = new[] { \"" + String.Join("\", \"", specials) + "\" };");
						Console.WriteLine("var expected = new[] { " + String.Join(", ", expect.Select(x => "new Token { TokenType = TokenType." + x.TokenType + ", Value = \"" + x.Value + "\"}")) + " };");
						Console.WriteLine("exception: " + e.Message);
					}
				}
				anyBreakage.ShouldBeFalse();
			}

			[Test]
			public void Given_stream_containing__aaa__and_special_tokens__a__should_return_tokens__a_Special__a_Special__a_Special()
			{
				const string input = "aaa";
				var specialTokens = new[] { "a" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "a"
							               }
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__aaa__and_special_tokens__aa__ab__should_return_tokens__aa_Special__a_Data()
			{
				const string input = "aaa";
				var specialTokens = new[] { "aa", "ab" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "aa"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "a"
							               }
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__aaa__and_special_tokens__aa__should_return_tokens__aa_Special__a_Data()
			{
				const string input = "aaa";
				var specialTokens = new[] { "aa" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "aa"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "a"
							               }
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__ababc__and_special_tokens__a__aba__ababc__should_return_tokens__a_Special__b_Data__a_Special__bc_Data()
			{
				const string input = "ababc";
				var specialTokens = new[] { "a", "aba", "ababc" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "b"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "bc"
							               }
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__ababc__and_special_tokens__aa__ab__abc__should_return_tokens__ab_Special__ab_Special__c_Data()
			{
				const string input = "ababc";
				var specialTokens = new[] { "aa", "ab", "abc" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "ab"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "ab"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "c"
							               }
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__abe__and_special_tokens__beab__a__eaec__b__should_return_tokens__a_Special__b_Special__e_Data()
			{
				const string input = "abe";
				var specialTokens = new[] { "beab", "a", "eaec", "b" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "b"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "e"
							               }
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__bcabb__and_special_tokens__b__babe__a__should_return_tokens__b_Special__c_Data__a_Special()
			{
				const string input = "bca";
				var specialTokens = new[] { "b", "babe", "a" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "b"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "c"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "a"
							               }
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__bddc__and_special_tokens__bc_d_dd_bdbc__should_return_tokens__b_Data__d_Special__d_Special__c_Data()
			{
				const string input = "bddc";
				var specialTokens = new[] { "bc", "d", "dd", "bdbc" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "b"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "d"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "d"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "c"
							               }
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__beabeedaed__and_special_tokens__eab__eb__bbe__eaad__should_return_tokens__b_Data__eab_Special__eadaed_Data()
			{
				const string input = "beabeedaed";
				var specialTokens = new[] { "eab", "eb", "bbe", "eaad" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "b"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "eab"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "eedaed"
							               }
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__beae__and_special_tokens__bb__e__ea__should_return_tokens__b_Data__e_Special__a_Data__e_Special()
			{
				const string input = "beae";
				var specialTokens = new[] { "bb", "e", "ea" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "b"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "e"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "e"
							               }
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__cdbdbbdbdc__and_special_tokens__d__cb__a__bdc__should_return_tokens__c_Data__d_Special__b_Data__d_Special__bb_Data__d_Special__bdc_Special()
			{
				const string input = "cdbdbbdbdc";
				var specialTokens = new[] { "d", "cb", "a", "bdc" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "c"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "d"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "b"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "d"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "bb"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "d"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "bdc"
							               }
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__cdbeaebaba__and_special_tokens__acdc__e__should_return_tokens__cdb_Data__e_Special__a_Data__e_Special__baba_Data()
			{
				const string input = "cdbeaebaba";
				var specialTokens = new[] { "acdc", "e" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "cdb"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "e"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "e"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "baba"
							               }
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__ceaada__and_special_tokens__ea_c_e_cab__should_return_tokens__c_Special__e_Special__aada_Data()
			{
				const string input = "ceaada";
				var specialTokens = new[] { "ea", "c", "e", "cab" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "c"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "e"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "aada"
							               }
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__daebbcaabd__and_special_tokens__cacd__d__ded__cab__a__b__ea__cbc__should_return_tokens__d_Special__a_Special__e_Data__b_Special__b_Special__c_Data__a_Special__a_Special__b_Special__d_Special()
			{
				const string input = "daebbcaabd";
				var specialTokens = new[] { "cacd", "d", "ded", "cab", "a", "b", "ea", "cbc" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "d"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "e"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "b"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "b"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "c"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "b"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "d"
							               }
					               };
				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__dcad__and_special_tokens__ad_dcac_c_a__should_return_tokens__d_Data__c_Special__a_Special__d_Special()
			{
				const string input = "dcad";
				var specialTokens = new[] { "ad", "dcac", "c", "a" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "d"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "c"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "d"
							               }
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__ebebbb__and_special_tokens__ebe__should_return_tokens__cdb_Data__ebe_Special__bbb_Data()
			{
				const string input = "ebebbb";
				var specialTokens = new[] { "ebe" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "ebe"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "bbb"
							               }
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void Given_stream_containing__edbb__and_special_tokens__bb_ecc_dc_b_bd_d__should_return_tokens__e_Data__d_Special__b_Special__b_Special()
			{
				const string input = "edbb";
				var specialTokens = new[] { "bb", "ecc", "dc", "b", "bd", "d" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "e"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "d"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "b"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "b"
							               }
					               };

				Check(input, specialTokens, expected);
			}

			[Test]
			public void TestGetExpected_1()
			{
				const string input = "cdbdbbdbdc";
				var specialTokens = new[] { "d", "aad", "cb", "a", "ebbb", "bdc", "ebb" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "c"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "d"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "b"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "d"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "bb"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "d"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "bdc"
							               }
					               };
				var result = GetExpected(input, specialTokens).ToList();
				CompareResultWithExpected(result, expected);
			}

			[Test]
			public void TestGetExpected_2()
			{
				const string input = "beeaeddabc";
				var specialTokens = new[] { "eeba", "eeaa", "abad", "c", "cd" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "beeaeddab"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "c"
							               }
					               };
				var result = GetExpected(input, specialTokens).ToList();
				CompareResultWithExpected(result, expected);
			}

			[Test]
			public void TestGetExpected_3()
			{
				const string input = "cccbdacdce";
				var specialTokens = new[] { "a", "ccc", "bead" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "ccc"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "bd"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "cdce"
							               }
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
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "bbaccedbde"
							               }
					               };

				var result = GetExpected(input, specialTokens).ToList();
				CompareResultWithExpected(result, expected);
			}

			[Test]
			public void TestGetExpected_5()
			{
				const string input = "cdbeaebaba";
				var specialTokens = new[] { "acdc", "e" };
				var expected = new[]
					               {
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "cdb"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "e"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "a"
							               },
						               new Token
							               {
								               TokenType = TokenType.Special,
								               Value = "e"
							               },
						               new Token
							               {
								               TokenType = TokenType.Data,
								               Value = "baba"
							               }
					               };

				var result = GetExpected(input, specialTokens).ToList();
				CompareResultWithExpected(result, expected);
			}

			private void Check(string input, string[] specialTokens, IList<Token> expect)
			{
				var stream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var tokens = _tokenizer.Tokenize(stream, specialTokens.SelectMany(x => x).Distinct().ToArray()).ToList();
				var result = _merger.Merge(tokens, specialTokens).ToList();
				CompareResultWithExpected(result, expect);
			}

			private static void CompareResultWithExpected(List<Token> result, IList<Token> expect)
			{
				result.Count.ShouldBeEqualTo(expect.Count, "expected " + expect.Count + " tokens but received " + result.Count + ": \"" + String.Join("\", \"", result.Select(x => x.Value).ToArray()) + "\"");
				for (var i = 0; i < result.Count; i++)
				{
					var actual = result[i];
					var expected = expect[i];
					actual.TokenType.ShouldBeEqualTo(expected.TokenType, "Token " + (1 + i) + " had token type " + actual.TokenType + " but should have been " + expected.TokenType);
					actual.Value.ShouldBeEqualTo(expected.Value, "Token " + (1 + i) + " had value '" + actual.Value + "' but should have been: " + expected.Value);
				}
			}

			private static IEnumerable<Token> GetExpected(string input, IEnumerable<string> specials)
			{
				var data = new StringBuilder();
				var specialsOrderedByLengthDescending = specials.OrderBy(x => x.Length).ToList();
				while (input.Length > 0)
				{
					var match = specialsOrderedByLengthDescending.FirstOrDefault(input.StartsWith);
					if (match != null)
					{
						if (data.Length > 0)
						{
							yield return new Token
								             {
									             TokenType = TokenType.Data,
									             Value = data.ToString()
								             };
							data = new StringBuilder();
						}
						yield return new Token
							             {
								             TokenType = TokenType.Special,
								             Value = match
							             };
						input = input.Substring(match.Length);
					}
					else
					{
						data.Append(input.Substring(0, 1));
						input = input.Substring(1);
					}
				}
				if (data.Length > 0)
				{
					yield return new Token
						             {
							             TokenType = TokenType.Data,
							             Value = data.ToString()
						             };
				}
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