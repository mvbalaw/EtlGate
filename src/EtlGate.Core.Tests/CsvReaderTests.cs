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
	public class CsvReaderTests
// ReSharper restore ClassNeverInstantiated.Global
	{
		[TestFixture]
		public class When_asked_to_read_from_a_stream
		{
			private CsvReader _reader;

			[SetUp]
			public void Before_each_test()
			{
				_reader = new CsvReader(new DelimitedDataReader(new StreamTokenizer()));
			}

			[Test]
			public void FuzzTestIt()
			{
				var anyBreakage = false;
				const string values = "a\r\n,\"";
				var random = new Random();
				for (var i = 0; i < 1000; i++)
				{
					var input = new StringBuilder();
					for (var j = 0; j < 10; j++)
					{
						var index = random.Next(values.Length);
						input.Append(values[index]);
					}

					var rows = GetExpected(input.ToString()).ToList();

					if (rows.Last().Count == 0)
					{
						rows.RemoveAt(rows.Count - 1);
					}
					var expect = rows.Select(x => x.Select((y, inx) => new
						                                                   {
							                                                   Value = y,
							                                                   Key = "Field" + (inx + 1)
						                                                   }).ToDictionary(y => y.Key, z => z.Value.Value)).ToArray();
					var expectUnclosedQuoteException = rows.Where(x => x.Any(y => y.IsQuoted && y.ErrorMissingTrailingQuote)).ToList();
					var expectUnescapedQuotedException = rows.Where(x => x.Any(y => y.IsQuoted && y.ErrorUnescapedQuote)).ToList();
					try
					{
						Check(input.ToString(), expect);
					}
					catch (Exception e)
					{
						if (expectUnclosedQuoteException.Any() && e.Message.Contains("Quoted field does not have a close quote"))
						{
							continue;
						}
						if (expectUnescapedQuotedException.Any() && e.Message.Contains("Unescaped"))
						{
							continue;
						}
						anyBreakage = true;
						Console.WriteLine("exception with input '" + input.ToString().Replace("\r", "RETURN").Replace("\n", "NEWLINE") + "' : " + e);
					}
				}
				anyBreakage.ShouldBeFalse();
			}

			[Test]
			[ExpectedException(typeof(ArgumentException))]
			public void Given_a_null_stream__should_throw_an_exception()
			{
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
				_reader.ReadFrom(null).ToList();
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
			}

			[Test]
			public void Given_a_stream_containing__COMMA_b__should_return_1_row_with_2_fields__EMPTY__b()
			{
				const string input = ",b";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "" },
								             { "Field2", "b" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__NEWLINE__and_delimiter__NEWLINE__should_return_1_row_with_0_fields()
			{
				const string input = "\n";
				Check(input, new[]
					             {
						             new Dictionary<string, string>()
					             }, input);
			}

			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "Unescaped '\"' on line 1 field 1")]
			public void Given_a_stream_containing__QUOTE_COMMA_RETURN_QUOTE_RETURN_a__should_throw_a_parse_exception_due_to_unescaped_quote()
			{
				const string input = "\",\r\"\ra";
				Check(input, new Dictionary<string, string>[] { });
			}

			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "Quoted field does not have a close quote on line 1 field 1")]
			public void Given_a_stream_containing__QUOTE_COMMA_a__should_throw_a_parse_exception_due_to_unclosed_quote()
			{
				const string input = "\",a";
				Check(input, new Dictionary<string, string>[] { });
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_QUOTE_COMMA_b__should_return_1_row_with_2_fields__EMPTY__b()
			{
				const string input = "\"\",b";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "" },
								             { "Field2", "b" }
							             }
					             });
			}

			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "Unescaped '\"' on line 1 field 1")]
			public void Given_a_stream_containing__QUOTE_QUOTE_RETURN_QUOTE__should_throw_a_parse_exception_due_to_unescaped_quote()
			{
				const string input = "\"\"\r\"";
				Check(input, new Dictionary<string, string>[] { });
			}

			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "Unescaped '\"' on line 1 field 1")]
			public void Given_a_stream_containing__QUOTE_QUOTE_a__should_throw_a_parse_exception_due_to_unescaped_quote()
			{
				const string input = "\"\"a\",c";
				Check(input, new Dictionary<string, string>[] { });
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_a_COMMA_b_QUOTE_COMMA_c__should_return_1_row_with_2_fields__a_COMMA_b__c()
			{
				const string input = "\"a,b\",c";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a,b" },
								             { "Field2", "c" }
							             }
					             });
			}
			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "Unescaped '\"' on line 1 field 1")]
			public void Given_a_stream_containing__QUOTE_RETURN_NEWLINE_QUOTE_RETURN__should_throw_a_parse_exception_due_to_unescaped_quote()
			{
				const string input = "\"\r\n\"\r";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "\r\n\"\r" }
							             }
					             });
			}
			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "Unescaped '\"' on line 1 field 1")]
			public void Given_a_stream_containing__QUOTE_a_QUOTE_RETURN_RETURN_NEWLINE_a_COMMA_aa__should_throw_a_parse_exception_due_to_unescaped_quote()
			{
				const string input = "\"a\"\r\r\na,aa";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a\"\r\r\na,aa" }
							             }
					             });
			}
			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "Unescaped '\"' on line 1 field 1")]
			public void Given_a_stream_containing__x__should_throw_a_parse_exception_due_to_unescaped_quote()
			{
				//",,"RETURN,,aaa
				const string input = "\",,\"\r,,aaa";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", ",," }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_a_QUOTE_QUOTE_b_QUOTE_COMMA_c__should_return_1_row_with_2_fields__a_QUOTE_b__c()
			{
				const string input = "\"a\"\"b\",c";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a\"b" },
								             { "Field2", "c" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_a_RETURN_NEWLINE_b_QUOTE_COMMA_c__should_return_1_row_with_2_fields__a_RETURN_NEWLINE_b__c()
			{
				const string input = "\"a\r\nb\",c";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a\r\nb" },
								             { "Field2", "c" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_ab_QUOTE_COMMA_c__should_return_1_row_with_2_fields__ab__c()
			{
				const string input = "\"ab\",c";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "ab" },
								             { "Field2", "c" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__RETURN_NEWLINE__should_return_1_row_with_0_fields()
			{
				const string input = "\r\n";
				Check(input, new[]
					             {
						             new Dictionary<string, string>()
					             });
			}

			[Test]
			public void Given_a_stream_containing__RETURN__and_delimiter__NEWLINE__should_return_1_row_with_1_field__RETURN()
			{
				const string input = "\r";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "\r" }
							             }
					             }, "\n");
			}

			[Test]
			public void Given_a_stream_containing__RETURN__should_return_1_row_with_1_field__RETURN()
			{
				const string input = "\r";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "\r" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__RETURN_a_QUOTE_ca_QUOTE_RETURN_COMMA_bc__should_return_1_row_with_2_fields__RETURN_a_QUOTE_ca_QUOTE_RETURN__bc()
			{
				const string input = "\ra\"ca\"\r,bc";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "\ra\"ca\"\r" },
								             { "Field2", "bc" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__a_COMMA_COMMA_c__should_return_1_row_with_3_fields__a__EMPTY__c()
			{
				const string input = "a,,c";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a" },
								             { "Field2", "" },
								             { "Field3", "c" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__a_COMMA_QUOTE_QUOTE_COMMA_c__should_return_1_row_with_3_fields__a__EMPTY__c()
			{
				const string input = "a,\"\",c";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a" },
								             { "Field2", "" },
								             { "Field3", "c" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__a_COMMA_QUOTE_QUOTE__should_return_1_row_with_2_fields__a__EMPTY()
			{
				const string input = "a,\"\"";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a" },
								             { "Field2", "" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__a_COMMA_QUOTE_b_QUOTE_RETURN_NEWLINE_c_COMMA_QUOTE_d_QUOTE_RETURN_NEWLINE__should_return_2_rows_with_2_fields_each__a__b__c__d()
			{
				const string input = "a,\"b\"\r\nc,\"d\"\r\n";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a" },
								             { "Field2", "b" }
							             },
						             new Dictionary<string, string>
							             {
								             { "Field1", "c" },
								             { "Field2", "d" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__a_COMMA__should_return_1_row_with_2_fields__a__EMPTY()
			{
				const string input = "a,";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a" },
								             { "Field2", "" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__a_COMMA_b_RETURN_NEWLINE_c_COMMA_QUOTE_d_QUOTE__should_return_2_rows_with_2_fields_each__a__b__c__d()
			{
				const string input = "a,b\r\nc,\"d\"";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a" },
								             { "Field2", "b" }
							             },
						             new Dictionary<string, string>
							             {
								             { "Field1", "c" },
								             { "Field2", "d" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__a_COMMA_b_RETURN_NEWLINE_c_COMMA_d_RETURN_NEWLINE__should_return_2_rows_with_2_fields_each__a__b__c__d()
			{
				const string input = "a,b\r\nc,d\r\n";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a" },
								             { "Field2", "b" }
							             },
						             new Dictionary<string, string>
							             {
								             { "Field1", "c" },
								             { "Field2", "d" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__a_COMMA_b__should_return_1_row_with_2_fields__a__b()
			{
				const string input = "a,b";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a" },
								             { "Field2", "b" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__a_QUOTE_QUOTE_b_COMMA_c__should_return_1_row_with_2_fields__a_QUOTE_QUOTE_b__c()
			{
				const string input = "a\"\"b,c";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a\"\"b" },
								             { "Field2", "c" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__a_QUOTE_b_COMMA_c__should_return_1_row_with_2_fields__a_QUOTE_B__c()
			{
				const string input = "a\"b,c";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a\"b" },
								             { "Field2", "c" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing_header_row__A_COMMA_B__and_data__c_COMMA_d__should_return_1_row_with_2_fields__A__B__having_values__c__d()
			{
				const string input = "A,B\r\nc,d";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "A", "c" },
								             { "B", "d" },
								             { "Field1", "c" },
								             { "Field2", "d" }
							             }
					             }, hasHeaderRow:true);
			}

			[Test]
			public void Given_a_stream_containing_header_row__A_COMMA_B__and_data__c__should_return_1_row_with_fields__Field1__A__having_values__c__c()
			{
				const string input = "A,B\r\nc";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "A", "c" },
								             { "Field1", "c" }
							             }
					             }, hasHeaderRow:true);
			}

			[Test]
			public void Given_a_stream_containing_header_row__A__and_data__c_d__should_return_1_row_with_fields__Field1__Field2__A__having_values__c__d__c()
			{
				const string input = "A\r\nc,d";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "A", "c" },
								             { "Field1", "c" },
								             { "Field2", "d" }
							             }
					             }, hasHeaderRow:true);
			}

			[Test]
			public void Given_a_stream_containing_header_row__QUOTE_A_QUOTE_COMMA_B__and_data__c_d__should_return_1_row_with_fields__Field1__Field2__A__B__having_values__c__d__c__d()
			{
				const string input = "\"A\",B\r\nc,d";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "A", "c" },
								             { "B", "d" },
								             { "Field1", "c" },
								             { "Field2", "d" }
							             }
					             }, hasHeaderRow:true);
			}

			[Test]
			public void Given_a_stream_containing_header_row__QUOTE_A_SPACE_B_QUOTE_COMMA_C__and_data__c_d__should_return_1_row_with_fields__Field1__Field2__A_SPACE_B__C__having_values__c__d__c__d()
			{
				const string input = "\"A B\",C\r\nc,d";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "A B", "c" },
								             { "C", "d" },
								             { "Field1", "c" },
								             { "Field2", "d" }
							             }
					             }, hasHeaderRow:true);
			}

			[Test]
			public void Given_an_empty_stream__should_return_an_empty_result()
			{
				const string input = "";
				Check(input, new Dictionary<string, string>[] { });
			}

			[Test]
			public void Given_content_and_a_null_record_separator__should_return_only_1_record()
			{
				const string input = "\r";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "\r" }
							             }
					             }, null);
			}

			[Test]
			public void TestGetExpected_1()
			{
				const string input = "cc,\"b\",bb\n";
				var result = GetExpected(input).ToList();
				result.Count.ShouldBeEqualTo(1);
				var items = result.First();
				items[0].Value.ShouldBeEqualTo("cc");
				items[1].Value.ShouldBeEqualTo("b");
				items[1].IsQuoted.ShouldBeTrue();
				items[2].Value.ShouldBeEqualTo("bb\n");
			}

			[Test]
			public void TestGetExpected_10()
			{
				const string input = "\",\"\r\rab,,\"";
				var result = GetExpected(input).ToList();
				result.Count.ShouldBeEqualTo(1);
				var items = result.First();
				items.Count.ShouldBeEqualTo(1);
				items[0].Value.ShouldBeEqualTo(",\"\r\rab,,");
				items[0].IsQuoted.ShouldBeTrue();
				items[0].ErrorUnescapedQuote.ShouldBeTrue();
			}

			[Test]
			public void TestGetExpected_11()
			{
				const string input = "\",a\"\r";
				var result = GetExpected(input).ToList();
				result.Count.ShouldBeEqualTo(1);
				var items = result.First();
				items.Count.ShouldBeEqualTo(1);
				items[0].Value.ShouldBeEqualTo(",a\"\r");
				items[0].IsQuoted.ShouldBeTrue();
				items[0].ErrorUnescapedQuote.ShouldBeTrue();
			}

			[Test]
			public void TestGetExpected_2()
			{
				const string input = "\n\"c,,\"\"\"c\"";
				var result = GetExpected(input).ToList();
				result.Count.ShouldBeEqualTo(1);
				var items = result.First();
				items[0].Value.ShouldBeEqualTo("\n\"c");
				items[1].Value.ShouldBeEqualTo("");
				items[2].Value.ShouldBeEqualTo("\"c");
				items[2].IsQuoted.ShouldBeTrue();
			}

			[Test]
			public void TestGetExpected_3()
			{
				const string input = ",c,\"\"\"a,a\"";
				var result = GetExpected(input).ToList();
				result.Count.ShouldBeEqualTo(1);
				var items = result.First();
				items[0].Value.ShouldBeEqualTo("");
				items[1].Value.ShouldBeEqualTo("c");
				items[2].Value.ShouldBeEqualTo("\"a,a");
				items[2].IsQuoted.ShouldBeTrue();
			}

			[Test]
			public void TestGetExpected_4()
			{
				const string input = "\r\n,\"\nab,\"";
				var result = GetExpected(input).ToList();
				result.Count.ShouldBeEqualTo(2);
				var items = result.First();
				items.Count.ShouldBeEqualTo(1);
				items[0].Value.ShouldBeEqualTo("");
				items[0].IsQuoted.ShouldBeFalse();
				items = result.Last();
				items.Count.ShouldBeEqualTo(2);
				items[0].Value.ShouldBeEqualTo("");
				items[0].IsQuoted.ShouldBeFalse();
				items[1].Value.ShouldBeEqualTo("\nab,");
				items[1].IsQuoted.ShouldBeTrue();
			}

			[Test]
			public void TestGetExpected_5()
			{
				const string input = "\"\"\r\nbca,,a";
				var result = GetExpected(input).ToList();
				result.Count.ShouldBeEqualTo(2);
				var items = result.First();
				items[0].Value.ShouldBeEqualTo("");
				items[0].IsQuoted.ShouldBeTrue();
				items = result.Last();
				items.Count.ShouldBeEqualTo(3);
				items[0].Value.ShouldBeEqualTo("bca");
				items[1].Value.ShouldBeEqualTo("");
				items[2].Value.ShouldBeEqualTo("a");
			}

			[Test]
			public void TestGetExpected_6()
			{
				const string input = "\"\"\"a\n\rcbac";
				var result = GetExpected(input).ToList();
				result.Count.ShouldBeEqualTo(1);
				var items = result.First();
				items.Count.ShouldBeEqualTo(1);
				items[0].Value.ShouldBeEqualTo("\"a\n\rcbac");
				items[0].IsQuoted.ShouldBeTrue();
				items[0].ErrorMissingTrailingQuote.ShouldBeTrue();
			}

			[Test]
			public void TestGetExpected_7()
			{
				const string input = "cc\r,bba\r\rb";
				var result = GetExpected(input).ToList();
				result.Count.ShouldBeEqualTo(1);
				var items = result.First();
				items.Count.ShouldBeEqualTo(2);
				items[0].Value.ShouldBeEqualTo("cc\r");
				items[1].Value.ShouldBeEqualTo("bba\r\rb");
			}

			[Test]
			public void TestGetExpected_8()
			{
				const string input = "\"b\"\"\"a";
				var result = GetExpected(input).ToList();
				result.Count.ShouldBeEqualTo(1);
				var items = result.First();
				items.Count.ShouldBeEqualTo(1);
				items[0].Value.ShouldBeEqualTo("b\"\"a");
				items[0].ErrorUnescapedQuote.ShouldBeTrue();
			}

			[Test]
			public void TestGetExpected_9()
			{
				const string input = "\r";
				var result = GetExpected(input).ToList();
				result.Count.ShouldBeEqualTo(1);
				var items = result.First();
				items.Count.ShouldBeEqualTo(1);
				items[0].Value.ShouldBeEqualTo("\r");
			}

			private void Check(string input, IList<Dictionary<string, string>> expect, string recordSeparator = "\r\n", bool hasHeaderRow = false)
			{
				var stream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _reader.ReadFrom(stream, recordSeparator, hasHeaderRow).ToList();
				result.Count.ShouldBeEqualTo(expect.Count);
				for (var rowNumber = 0; rowNumber < expect.Count; rowNumber++)
				{
					var actualRow = result[rowNumber];
					var expectedRow = expect[rowNumber];
					foreach (var field in expectedRow)
					{
						string actualValue;
						if (!actualRow.TryGetValue(field.Key, out actualValue))
						{
							Assert.Fail("Row " + (1 + rowNumber) + " does not contain field named " + field.Key);
						}
						else
						{
							actualValue = actualValue.Replace("\r", "RETURN").Replace("\n", "NEWLINE");
						}
						var expectedValue = field.Value.Replace("\r", "RETURN").Replace("\n", "NEWLINE");
						actualValue.ShouldBeEqualTo(expectedValue, "Row " + (1 + rowNumber) + " " + field.Key + " did not match. Expected '" + expectedValue + "' but was '" + actualValue + "'");
					}
				}
			}

			private class Field
			{
				public bool ErrorMissingTrailingQuote;
				public bool ErrorUnescapedQuote;
				public bool IsQuoted;
				public string Value;

				public Field()
				{
					Value = "";
				}
			}

			private static IEnumerable<List<Field>> GetExpected(string input)
			{
				var result = new List<List<Field>>();
				var quoted = false;
				var items = new List<Field>();
				var item = new Field();
				var expectQuote = false;
				var haveReturn = false;
				foreach (var ch in input)
				{
					if (haveReturn && ch == '\n')
					{
						if (!quoted || expectQuote)
						{
							items.Add(item);
							result.Add(items);
							items = new List<Field>();
							item = new Field();
							quoted = false;
							expectQuote = false;
							haveReturn = false;
							continue;
						}
					}
					if (haveReturn)
					{
						if (expectQuote)
						{
							item.Value += "\"";
							item.ErrorUnescapedQuote = true;
							expectQuote = false;
						}
						item.Value += "\r";
						haveReturn = false;
					}
					if (ch == '\r')
					{
						haveReturn = true;
						continue;
					}

					if (ch == '"' && quoted)
					{
						if (expectQuote)
						{
							item.Value += "\"";
							expectQuote = false;
						}
						else
						{
							expectQuote = true;
						}
						continue;
					}
					if (ch == '"' && item.Value.Length == 0 && !expectQuote)
					{
						item.IsQuoted = true;
						quoted = true;
						continue;
					}

					if (ch == ',')
					{
						if (expectQuote)
						{
							items.Add(item);
						}
						else if (quoted)
						{
							item.Value += ch;
							continue;
						}
						else
						{
							items.Add(item);
						}
						item = new Field();
						quoted = false;
						expectQuote = false;
						continue;
					}
					if (expectQuote)
					{
						item.ErrorUnescapedQuote = true;
						item.Value += "\"";
						expectQuote = false;
					}
					item.Value += ch;
				}
				if (haveReturn)
				{
					if (expectQuote)
					{
						item.Value += "\"";
						item.ErrorUnescapedQuote = true;
					}
					item.Value += "\r";
				}
				if (item.Value.Length > 0 || item.IsQuoted)
				{
					if (quoted)
					{
						item.ErrorMissingTrailingQuote = true;
					}
					items.Add(item);
				}
				if (items.Count > 0)
				{
					result.Add(items);
				}
				return result;
			}
		}
	}
}