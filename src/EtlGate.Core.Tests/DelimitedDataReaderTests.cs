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
	public class DelimitedDataReaderTests
// ReSharper restore ClassNeverInstantiated.Global
	{
		[TestFixture]
		public class When_asked_to_read_from_a_stream_with_field_delimiter_COMMA_record_delimiter_RETURN_NEWLINE_and_quoted_fields_NOT_supported
		{
			private IDelimitedDataReader _reader;

			[SetUp]
			public void Before_each_test()
			{
				_reader = new DelimitedDataReader(new StreamTokenizer());
			}

			[Test]
			public void FuzzTestIt()
			{
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

					if (input.ToString().EndsWith("\r\n"))
					{
						rows.RemoveAt(rows.Count - 1);
					}
					var expect = rows.Select(x => x.Select((y, inx) => new
						                                                   {
							                                                   Value = y,
							                                                   Key = "Field" + (inx + 1)
						                                                   })
					                               .ToDictionary(y => y.Key, z => z.Value))
					                 .ToArray();
					try
					{
						Check(input.ToString(), expect);
					}
					catch (Exception e)
					{
						Console.WriteLine("exception with input '" + input.ToString().Replace("\r", "RETURN").Replace("\n", "NEWLINE") + "' : " + e);
					}
				}
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
			public void Given_a_stream_containing__QUOTE_COMMA_RETURN_QUOTE_RETURN_a__should_return_1_row_with_2_fields__QUOTE__RETURN_QUOTE_RETURN_a()
			{
				const string input = "\",\r\"\ra";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "\"" },
								             { "Field2", "\r\"\ra" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_a_COMMA_b_QUOTE_COMMA_c__should_return_1_row_with_3_fields__QUOTE_a__b_QUOTE__c()
			{
				const string input = "\"a,b\",c";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "\"a" },
								             { "Field2", "b\"" },
								             { "Field3", "c" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_a_RETURN_NEWLINE_b_QUOTE_COMMA_c__should_return_2_rows__the_first_with_1_field__QUOTE_a__the_second_with_2_fields__b_QUOTE__c()
			{
				const string input = "\"a\r\nb\",c";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "\"a" }
							             },
						             new Dictionary<string, string>
							             {
								             { "Field1", "b\"" },
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
			public void Given_a_stream_containing__a_COMMA_QUOTE_QUOTE_COMMA_c__should_return_1_row_with_3_fields__a__QUOTE_QUOTE__c()
			{
				const string input = "a,\"\",c";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a" },
								             { "Field2", "\"\"" },
								             { "Field3", "c" }
							             }
					             });
			}

			[Test]
			public void Given_a_stream_containing__a_COMMA_QUOTE_QUOTE__should_return_1_row_with_2_fields__a__QUOTE_QUOTE()
			{
				const string input = "a,\"\"";
				Check(input, new[]
					             {
						             new Dictionary<string, string>
							             {
								             { "Field1", "a" },
								             { "Field2", "\"\"" }
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
			public void Given_an_empty_stream__should_return_an_empty_result()
			{
				const string input = "";
				Check(input, new Dictionary<string, string>[] { });
			}

			private void Check(string input, IList<Dictionary<string, string>> expect)
			{
				var stream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _reader.ReadFrom(stream, ",", "\r\n", false).ToList();
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

			private static IEnumerable<List<string>> GetExpected(string input)
			{
				return input.Split(new[] { "\r\n" }, StringSplitOptions.None).Select(x => x.Split(',').ToList());
			}
		}

		[TestFixture]
		public class When_asked_to_read_from_a_stream_with_multi_character_field_delimiter_and_RETURN_NEWLINE_record_delimiter
		{
			private IDelimitedDataReader _reader;

			[SetUp]
			public void Before_each_test()
			{
				_reader = new DelimitedDataReader(new StreamTokenizer());
			}

			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "Header row must not have more than one field with the same name. 'Field1' appears more than once in the header row.")]
			public void Given_a_stream_containing_a_header_row__should_throw_ParseException_for_two_header_rows_with_same_name()
			{
				const string input = "Field1,Field1\r\nValue1,Value2";
				const string fieldSeparator = ",";
				const bool supportQuotedFields = true;
				const bool hasHeaderRow = true;
				var stream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _reader.ReadFrom(stream, fieldSeparator, "\r\n", supportQuotedFields, hasHeaderRow);
				result.GetEnumerator().MoveNext();

			}

			[Test]
			[Ignore("Still has failures for long field separators that contain the record separator")]
			public void FuzzTestIt()
			{
				const string values = "a\r\n,\"";
				var random = new Random();
				const string recordSeparator = "\r\n";
				for (var i = 0; i < 1000000; i++)
				{
					var input = new StringBuilder();
					for (var j = 0; j < 10; j++)
					{
						var index = random.Next(values.Length);
						input.Append(values[index]);
					}

					var fieldSeparator = new StringBuilder();
					for (var j = 0; j < random.Next(0, 5); j++)
					{
						var index = random.Next(values.Length);
						fieldSeparator.Append(values[index]);
					}

					var quotedFieldsSupported = random.Next(2) == 0;

					var rows = GetExpected(input.ToString(), fieldSeparator.ToString(), quotedFieldsSupported).ToList();

					if (input.ToString().EndsWith(recordSeparator) &&
					    rows.Any() &&
					    rows.Last().Count == 1 &&
					    rows.Last().Last().Value == "")
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
					var expectFieldSeparatorException = fieldSeparator.ToString() == recordSeparator;
					try
					{
						Check(input.ToString(), expect, fieldSeparator.ToString(), quotedFieldsSupported);
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
						if (expectFieldSeparatorException && e.Message.Contains("field separator and record separator must be different."))
						{
							continue;
						}

						Console.WriteLine("input: " + input.ToString().Replace("\r", "RETURN").Replace("\n", "NEWLINE"));
						Console.WriteLine("fieldSeparator: " + fieldSeparator.ToString().Replace("\r", "RETURN").Replace("\n", "NEWLINE"));
						Console.WriteLine("supportQuotedFields: " + quotedFieldsSupported);
						Console.WriteLine("exception: " + e);
						Assert.Fail();
					}
				}
			}

			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "Quoted field does not have a close quote on line 3 field 1")]
			public void Given_a_stream_containing__COMMA_QUOTE_RETURN_NEWLINE_RETURN_QUOTE_RETURN_RETURN_NEWLINE_QUOTE__and_field_separator__NEWLINE_RETURN_RETURN__and_quoted_fields_ARE_supported__should_throw_parse_exception_due_to_unclosed_quote()
			{
				const string input = ",\"\r\n\r\"\r\r\n\"";
				const string fieldSeparator = "\n\r\r";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", ",\"" }
							               },
						               new Dictionary<string, string>
							               {
								               { "Field1", "\r\"\r" }
							               },
						               new Dictionary<string, string>
							               {
								               { "Field1", "" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__COMMA_QUOTE_a_QUOTE_COMMA_QUOTE_NEWLINE_RETURN_QUOTE_RETURN__and_field_separator__QUOTE_NEWLINE__and_quoted_fields_ARE_supported__should_return_only_1_row_with_fields__COMMA_QUOTE_a_QUOTE_COMMA__RETURN_QUOTE_RETURN()
			{
				const string input = ",\"a\",\"\n\r\"\r";
				const string fieldSeparator = "\"\n";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", ",\"a\"," },
								               { "Field2", "\r\"\r" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__COMMA_RETURN_NEWLINE_RETURN_a__and_field_separator__COMMA_RETURN_NEWLINE_RETURN__and_quoted_fields_NOT_supported__should_return_only_1_row_with_fields__EMPTY__a()
			{
				const string input = ",\r\n\ra";
				const string fieldSeparator = ",\r\n\r";

				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "" },
								               { "Field2", "a" }
							               }
					               };
				Check(input, expected, fieldSeparator);
			}

			[Test]
			public void Given_a_stream_containing__NEWLINE_QUOTE_a_NEWLINE_a_COMMA_RETURN_COMMA_QUOTE_QUOTE__and_field_separator__EMPTY__and_quoted_fields_ARE_supported__should_return_only_1_row_with_fields__NEWLINE_QUOTE_a_NEWLINE_a_COMMA_RETURN_COMMA_QUOTE_QUOTE()
			{
				const string input = "\n\"a\na,\r,\"\"";
				const string fieldSeparator = "";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\n\"a\na,\r,\"\"" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "Quoted field does not have a close quote on line 1 field 1")]
			public void Given_a_stream_containing__QUOTE_COMMA_QUOTE_QUOTE_a__and_field_separator__COMMA_QUOTE__and_quoted_fields_ARE_supported__should_throw_parse_exception_due_to_missing_trailing_quote()
			{
				const string input = "\",\"\"a";
				const string fieldSeparator = ",\"";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\",\"a" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "Unescaped '\"' on line 1 field 1")]
			public void Given_a_stream_containing__QUOTE_COMMA_QUOTE_RETURN_RETURN_NEWLINE__and_field_separator__a_RETURN_COMMA__and_quoted_fields_ARE_supported__should_throw_parse_exception_due_to_unescaped_quote()
			{
				const string input = "\",\"\r\r\n";
				const string fieldSeparator = "a\r,";
				const bool supportQuotedFields = true;

				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "a" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_NEWLINE_QUOTE_QUOTE_RETURN_QUOTE_RETURN_NEWLINE_COMMA_QUOTE__and_field_separator__RETURN_QUOTE__and_quoted_fields_ARE_supported__should_return_only_1_row_with_fields__NEWLINE_QUOTE_QUOTE_RETURN__COMMA_QUOTE()
			{
				const string input = "\"\n\"\"\r\"\r\n,\"";
				const string fieldSeparator = "\r\"";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\n\"\r" }
							               },
						               new Dictionary<string, string>
							               {
								               { "Field1", ",\"" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_NEWLINE_RETURN_NEWLINE_RETURN_a_QUOTE_a_QUOTE_COMMA__and_field_separator__a_QUOTE__and_quoted_fields_ARE_supported__should_return_only_1_row_with_fields__NEWLINE_RETURN_NEWLINE_RETURN_a__COMMA()
			{
				const string input = "\"\n\r\n\ra\"a\",";
				const string fieldSeparator = "a\"";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\n\r\n\ra" },
								               { "Field2", "," }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_NEWLINE_RETURN_RETURN_QUOTE_RETURN_NEWLINE_COMMA_RETURN_a__and_field_separator__NEWLINE__and_quoted_fields_ARE_supported__should_return_only_1_row_with_fields__NEWLINE_RETURN_RETURN__COMMA_RETURN_a()
			{
				const string input = "\"\n\r\r\"\r\n,\ra";
				const string fieldSeparator = "\r\"\r";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\n\r\r" }
							               },
						               new Dictionary<string, string>
							               {
								               { "Field1", ",\ra" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_NEWLINE_RETURN_RETURN_a_COMMA_RETURN_RETURN_NEWLINE_QUOTE__and_field_separator__QUOTE_NEWLINE__should_throw_parse_exception_due_to_missing_close_quote()
			{
				const string input = "\"\n\r\ra,\r\r\n\"";
				const string fieldSeparator = "\"\n";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\n\r\ra,\r\r\n" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_QUOTE_NEWLINE_COMMA_NEWLINE_a_RETURN_RETURN_RETURN_a__and_field_separator__a_COMMA__and_quoted_fields_NOT_supported__should_return_only_1_row_with_1_field__QUOTE_QUOTE_NEWLINE_COMMA_NEWLINE_a_RETURN_RETURN_RETURN_a()
			{
				const string input = "\"\"\n,\na\r\r\ra";
				const string fieldSeparator = "a,";
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\"\"\n,\na\r\r\ra" }
							               }
					               };
				Check(input, expected, fieldSeparator);
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_QUOTE_RETURN_NEWLINE_a__and_field_separator__RETURN_QUOTE__and_quoted_fields_ARE_supported__should_return_only_1_row_with_fields__EMPTY__a()
			{
				const string input = "\"\"\r\na";
				const string fieldSeparator = "\"\r";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "" }
							               },
						               new Dictionary<string, string>
							               {
								               { "Field1", "a" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			[Ignore("Need to handle this case - it looses the 'a' after the newline")]
			public void Given_a_stream_containing__QUOTE_RETURN_NEWLINE_a_RETURN_QUOTE_COMMA__and_field_separator__QUOTE_RETURN_NEWLINE_RETURN__and_quoted_fields_NOT_supported__should_return_2_rows_with_1_field_each__QUOTE__a_RETURN_QUOTE_COMMA()
			{
				const string input = "\"\r\na\r\",";
				const string fieldSeparator = "\"\r\n\r";
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\"" }
							               },
						               new Dictionary<string, string>
							               {
								               { "Field1", "a\r\"," }
							               }
					               };
				Check(input, expected, fieldSeparator);
			}

			[Test]
			public void Given_a_stream_containing__QUOTE_RETURN_RETURN_NEWLINE_RETURN_QUOTE_COMMA_COMMA_a_NEWLINE__and_field_separator__COMMA_QUOTE_COMMA_NEWLINE__and_quoted_fields_NOT_supported__should_return_only_1_row_with_fields__QUOTE_RETURN__RETURN_QUOTE_COMMA_COMMA_a_NEWLINE()
			{
				const string input = "\"\r\r\n\r\",,a\n";
				const string fieldSeparator = ",\",\n";

				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\"\r" }
							               },
						               new Dictionary<string, string>
							               {
								               { "Field1", "\r\",,a\n" }
							               }
					               };
				Check(input, expected, fieldSeparator);
			}

			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "Quoted field does not have a close quote on line 1 field 1")]
			public void Given_a_stream_containing__QUOTE_RETURN_a_RETURN_NEWLINE_RETURN_QUOTE_QUOTE_COMMA_RETURN__and_field_separator__QUOTE_COMMA__and_quoted_fields_ARE_supported__should_throw_a_parse_exception_due_to_missing_close_quote()
			{
				const string input = "\"\ra\r\n\r\"\",\r";
				const string fieldSeparator = "\",";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\ra\r\n\r\",r" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__RETURN_COMMA_RETURN_a_NEWLINE_COMMA__and_field_separator__a_NEWLINE_COMMA__and_quoted_fields_ARE_supported__should_return_only_1_row_with_fields__RETURN_COMMA_RETURN__EMPTY()
			{
				// RETURN,,"RETURN,RETURNaNEWLINE,
				const string input = "\r,\ra\n,";
				const string fieldSeparator = "a\n,";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\r,\r" },
								               { "Field2", "" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__RETURN_QUOTE_NEWLINE_a_RETURN_NEWLINE_a_QUOTE_QUOTE_a__and_field_separator__RETURN_NEWLINE_a__and_quoted_fields_NOT_supported__should_return_2_rows_with_1_field_each__RETURN_QUOTE_NEWLINE_a__a_QUOTE_QUOTE_a()
			{
				const string input = "\r\"\na\r\na\"\"a";
				const string fieldSeparator = "\r\na";
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\r\"\na" }
							               },
						               new Dictionary<string, string>
							               {
								               { "Field1", "a\"\"a" }
							               }
					               };
				Check(input, expected, fieldSeparator);
			}

			[Test]
			public void Given_a_stream_containing__RETURN_QUOTE_QUOTE_QUOTE_a_QUOTE__and_field_separator__QUOTE_QUOTE__and_quoted_fields_ARE_supported__should_return_only_1_row_with_fields__RETURN__a()
			{
				const string input = "\r\"\"\"a\"";
				const string fieldSeparator = "\"\"";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\r" },
								               { "Field2", "a" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__RETURN_RETURN_NEWLINE__and_field_separator__NEWLINE__and_quoted_fields_ARE_supported__should_return_only_1_row_with_fields__RETURN()
			{
				const string input = "\r\r\n";
				const string fieldSeparator = "\n";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "\r" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__a_QUOTE_QUOTE_NEWLINE_COMMA__and_field_separator__QUOTE_NEWLINE__and_quoted_fields_ARE_supported__should_return_only_1_row_with_fields__a_QUOTE__COMMA()
			{
				const string input = "a\"\"\n,";
				const string fieldSeparator = "\"\n";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "a\"" },
								               { "Field2", "," }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__a_RETURN_NEWLINE_a__and_field_separator__NEWLINE__and_quoted_fields_ARE_supported__should_return_2_row_with_1_field_each__a__a()
			{
				const string input = "a\r\na";
				const string fieldSeparator = "\n";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "a" }
							               },
						               new Dictionary<string, string>
							               {
								               { "Field1", "a" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			public void Given_a_stream_containing__aa_RETURN_RETURN_QUOTE_RETURN_NEWLINE_a_RETURN_a__and_field_separator__QUOTE_RETURN__and_quoted_fields_ARE_supported__should_return_only_1_row_with_fields__aa_RETURN_RETURN__NEWLINE_a_RETURN_a()
			{
				const string input = "aa\r\r\"\r\na\ra";
				const string fieldSeparator = "\"\r";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new Dictionary<string, string>
							               {
								               { "Field1", "aa\r\r" },
								               { "Field2", "\na\ra" }
							               }
					               };
				Check(input, expected, fieldSeparator, supportQuotedFields);
			}

			[Test]
			[ExpectedException(typeof(ParseException), ExpectedMessage = "field separator and record separator must be different.")]
			public void Given_field_separator_and_record_separator_are_the_same__should_throw_parse_execption()
			{
				const string input = "a";
				const string fieldSeparator = "\r\n";
				Check(input, null, fieldSeparator);
			}

			[Test]
			public void TestGetExpected_1()
			{
				const string input = "\n\"\n\n,aaa\",";
				const string fieldSeparator = "\"\n";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "\n",
								               "\n,aaa\","
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_10()
			{
				const string input = "\",\"\"a";
				const string fieldSeparator = ",\"";

				const bool supportQuotedFields = true;
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				result[0][0].ErrorMissingTrailingQuote.ShouldBeTrue();
			}

			[Test]
			public void TestGetExpected_11()
			{
				const string input = "\"\n,\"aa";
				const string fieldSeparator = "aa";

				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "\n,",
								               ""
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_12()
			{
				const string input = "\"\r\",\r";
				const string fieldSeparator = ",\"";

				const bool supportQuotedFields = true;
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				result[0][0].ErrorUnescapedQuote.ShouldBeTrue();
			}

			[Test]
			public void TestGetExpected_13()
			{
				const string input = "\"\"\r";
				const string fieldSeparator = "\"\r";

				const bool supportQuotedFields = true;
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				result[0][0].ErrorUnescapedQuote.ShouldBeTrue();
			}

			[Test]
			public void TestGetExpected_14()
			{
				const string input = "\"\",\"\n,";
				const string fieldSeparator = ",\"";

				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "",
								               "\n,"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_15()
			{
				const string input = "\r\"\"a\"\r,\",\n";
				const string fieldSeparator = "\n\r";

				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "\r\"\"a\"\r,\",\n"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_16()
			{
				const string input = "\"\n\"";
				const string fieldSeparator = "\"\n";

				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "\n"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();
				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_17()
			{
				const string input = "a\ra\",a\na\r";
				const string fieldSeparator = "\ra";

				const bool supportQuotedFields = true;
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				result.SelectMany(x => x).Any(x => x.ErrorMissingTrailingQuote).ShouldBeTrue();
			}

			[Test]
			public void TestGetExpected_18()
			{
				const string input = "\"\n\r\n\ra\"a\",";
				const string fieldSeparator = "a\"";

				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "\n\r\n\ra",
								               ","
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_19()
			{
				const string input = "\"a\n\r,a\n,,\"";
				const string fieldSeparator = "\ra";

				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "a\n\r,a\n,,"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_2()
			{
				const string input = "aa\"a\"\ra,,\n";
				const string fieldSeparator = "\"a";
				const bool supportQuotedFields = false;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "aa",
								               "\"\ra,,\n"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_20()
			{
				const string input = "a,,\"\r\n\n\n\r\"";
				const string fieldSeparator = "\r\"";

				const bool supportQuotedFields = false;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "a,,\""
							               },
						               new List<string>
							               {
								               "\n\n",
								               ""
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_21()
			{
				const string input = "\"\"\r\n\n\ra\"a\"";
				const string fieldSeparator = "\"\n";

				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               ""
							               },
						               new List<string>
							               {
								               "\n\ra\"a\""
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_22()
			{
				const string input = "\"\n\"\r\n,\r\n\"\"";
				const string fieldSeparator = "\r\"";

				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "\n"
							               },
						               new List<string>
							               {
								               ","
							               },
						               new List<string>
							               {
								               ""
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_23()
			{
				const string input = "\"a\"\"aaa\na\"";
				const string fieldSeparator = "";

				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "a\"aaa\na"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_24()
			{
				const string input = "\"a\n\"aa\r";
				const string fieldSeparator = "\"a";

				const bool supportQuotedFields = true;
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				result.SelectMany(x => x).Any(x => x.ErrorMissingTrailingQuote).ShouldBeTrue();
			}

			[Test]
			public void TestGetExpected_25()
			{
				const string input = "aa\"\"\",,\"\"\"";
				const string fieldSeparator = "\"\"";

				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "aa",
								               ",,\""
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_26()
			{
				const string input = "\r\"\na\r\na\"\"a";
				const string fieldSeparator = "\r\na";

				const bool supportQuotedFields = false;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "\r\"\na"
							               },
						               new List<string>
							               {
								               "a\"\"a"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_3()
			{
				const string input = ",\"\r\n\r\r\"\r\r\"";
				const string fieldSeparator = "";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               ",\""
							               },
						               new List<string>
							               {
								               "\r\r\"\r\r\""
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_4()
			{
				const string input = ",a";
				const string fieldSeparator = "a\r";
				const bool supportQuotedFields = false;
				var expected = new[]
					               {
						               new List<string>
							               {
								               ",a"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_5()
			{
				const string input = ",\r\n\n\raa,\n,";
				const string fieldSeparator = "\ra";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               ","
							               },
						               new List<string>
							               {
								               "\n",
								               "a,\n,"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_6()
			{
				const string input = "\n\r\"\"a\n\raa,";
				const string fieldSeparator = "\r\r";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "\n\r\"\"a\n\raa,"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_7()
			{
				const string input = "a\r,\n\r\"\r\"a\n";
				const string fieldSeparator = "\"a";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "a\r,\n\r\"\r",
								               "\n"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_8()
			{
				const string input = "\r\r,a\na,\n,\r";
				const string fieldSeparator = "\r\r";
				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               "",
								               ",a\na,\n,\r"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			[Test]
			public void TestGetExpected_9()
			{
				const string input = ",\n\n\r\ra\na\n";
				const string fieldSeparator = "\r\"";

				const bool supportQuotedFields = true;
				var expected = new[]
					               {
						               new List<string>
							               {
								               ",\n\n\r\ra\na\n"
							               }
					               };
				var result = GetExpected(input, fieldSeparator, supportQuotedFields).ToList();

				CheckGetExpectedResult(result, expected);
			}

			private void Check(string input, IList<Dictionary<string, string>> expect, string fieldSeparator, bool supportQuotedFields = false)
			{
				var stream = new MemoryStream(Encoding.ASCII.GetBytes(input));
				var result = _reader.ReadFrom(stream, fieldSeparator, "\r\n", supportQuotedFields).ToList();
				CompareResultWithExpected(result, expect);
			}

			private static void CheckGetExpectedResult(IEnumerable<List<Field>> result, IEnumerable<List<string>> expected)
			{
				var resultConverted = result.Select(x => x.Select(((y, i) => new
					                                                             {
						                                                             Key = "Field" + (1 + i),
						                                                             y.Value
					                                                             })).ToDictionary(y => y.Key, y => y.Value)).ToList();
				var expectedConverted = expected.Select(x => x.Select((y, i) => new
					                                                                {
						                                                                Key = "Field" + (1 + i),
						                                                                Value = y
					                                                                }).ToDictionary(y => y.Key, y => y.Value)).ToList();
				CompareResultWithExpected(resultConverted, expectedConverted);
			}

			private static void CompareResultWithExpected(List<Dictionary<string, string>> result, IList<Dictionary<string, string>> expect)
			{
				result.Count.ShouldBeEqualTo(expect.Count);
				for (var rowNumber = 0; rowNumber < expect.Count; rowNumber++)
				{
					var actualRow = result[rowNumber];
					var expectedRow = expect[rowNumber];
					actualRow.Count.ShouldBeEqualTo(expectedRow.Count);
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

			private static IEnumerable<List<Field>> GetExpected(string input, string fieldSeparator, bool quotedFieldsSupported)
			{
				var result = new List<List<Field>>();
				var rowText = input;
				var items = new List<Field>();
				var checkForQuote = true;
				var item = new Field();
				while (rowText.Length > 0)
				{
					const string recordSeparator = "\r\n";
					if (checkForQuote && quotedFieldsSupported && rowText.StartsWith("\""))
					{
						if (item.Value.Length > 0)
						{
							items.Add(item);
							item = new Field();
						}
						rowText = rowText.Substring(1);
						var expectQuote = false;
						item.IsQuoted = true;
						var foundEnd = false;
						for (var i = 0; i < rowText.Length; i++)
						{
							if (rowText[i] == '"')
							{
								if (expectQuote)
								{
									item.Value += "\"";
								}
								else
								{
									if (rowText.Length == i + 1)
									{
										foundEnd = true;
										rowText = "";
										break;
									}
									if (fieldSeparator.Length < recordSeparator.Length)
									{
										if (rowText.Length > i + recordSeparator.Length &&
										    rowText.Substring(i + 1, recordSeparator.Length) == recordSeparator)
										{
											rowText = rowText.Substring(i + 1);
											foundEnd = true;
											break;
										}
										if (fieldSeparator.Length > 0 &&
										    !fieldSeparator.StartsWith("\"") &&
										    rowText.Length > i + fieldSeparator.Length &&
										    rowText.Substring(i + 1, fieldSeparator.Length) == fieldSeparator)
										{
											rowText = rowText.Substring(i + 1);
											foundEnd = true;
											checkForQuote = false;
											break;
										}
									}
									else
									{
										if (fieldSeparator.Length > 0 &&
										    !fieldSeparator.StartsWith("\"") &&
										    rowText.Length > i + fieldSeparator.Length &&
										    rowText.Substring(i + 1, fieldSeparator.Length) == fieldSeparator)
										{
											rowText = rowText.Substring(i + 1);
											foundEnd = true;
											checkForQuote = false;
											break;
										}
										if (rowText.Length > i + recordSeparator.Length &&
										    rowText.Substring(i + 1, recordSeparator.Length) == recordSeparator)
										{
											rowText = rowText.Substring(i + 1);
											foundEnd = true;
											break;
										}
									}
								}
								expectQuote = !expectQuote;
							}
							else
							{
								if (expectQuote)
								{
									items.Add(item);
									item.ErrorUnescapedQuote = true;
									item = new Field();
									break;
								}
								item.Value += rowText[i];
							}
						}
						if (!foundEnd)
						{
							items.Add(item);
							item.ErrorMissingTrailingQuote = true;
							item = new Field();
						}
						continue;
					}

					var idx = rowText.IndexOf(fieldSeparator);
					if (fieldSeparator.Length < recordSeparator.Length)
					{
						if (fieldSeparator.Length > 0 && idx == 0)
						{
							items.Add(item);
							item = new Field();
							rowText = rowText.Substring(fieldSeparator.Length);
							checkForQuote = true;
							continue;
						}
						if (rowText.StartsWith(recordSeparator))
						{
							items.Add(item);
							item = new Field();
							result.Add(items);
							items = new List<Field>();
							rowText = rowText.Substring(2);
							checkForQuote = true;
							continue;
						}
					}
					else
					{
						if (rowText.StartsWith(recordSeparator))
						{
							items.Add(item);
							item = new Field();
							result.Add(items);
							items = new List<Field>();
							rowText = rowText.Substring(2);
							checkForQuote = true;
							continue;
						}
						if (fieldSeparator.Length > 0 && idx == 0)
						{
							items.Add(item);
							item = new Field();
							rowText = rowText.Substring(fieldSeparator.Length);
							checkForQuote = true;
							continue;
						}
					}

					checkForQuote = false;
					item.Value += rowText.Substring(0, 1);
					rowText = rowText.Substring(1);
				}
				if (result.Any() || items.Any() || item.Value.Length > 0)
				{
					items.Add(item);
				}
				if (items.Any())
				{
					result.Add(items);
				}

				return result;
			}

//			private static IEnumerable<List<Field>> GetExpected(string input, string fieldSeparator, bool quotedFieldsSupported)
//			{
//				var result = new List<List<Field>>();
//				var quoted = false;
//				var items = new List<Field>();
//				var item = new Field();
//				var expectQuote = false;
//				var haveFieldSeparator0 = false;
//				var haveReturn = false;
//				foreach (var ch in input)
//				{
//					if (haveReturn && ch == '\n')
//					{
//						if (!quotedFieldsSupported || !quoted || expectQuote)
//						{
//							items.Add(item);
//							result.Add(items);
//							items = new List<Field>();
//							item = new Field();
//							quoted = false;
//							expectQuote = false;
//							haveReturn = false;
//							haveFieldSeparator0 = false;
//							continue;
//						}
//					}
//
//					if (haveFieldSeparator0 && ch == fieldSeparator[1])
//					{
//						if (!quoted)
//						{
//							items.Add(item);
//							item = new Field();
//							haveFieldSeparator0 = false;
//							haveReturn = false;
//							continue;
//						}
//						if (expectQuote)
//						{
//							items.Add(item);
//							item = new Field();
//							quoted = false;
//							haveReturn = false;
//							haveFieldSeparator0 = false;
//							expectQuote = false;
//							continue;
//						}
//					}
//
//					if (fieldSeparator.Length > 0 && ch == fieldSeparator[0])
//					{
//						if (!quoted && quotedFieldsSupported && ch == '"' && item.Value.Length == 0)
//						{
//							item.IsQuoted = true;
//							quoted = true;
//							continue;
//						}
//						if (haveFieldSeparator0)
//						{
//							item.Value += fieldSeparator[0];
//							haveReturn = false;
//						}
//						haveFieldSeparator0 = true;
//						if (haveReturn)
//						{
//							item.Value += "\r";
//						}
//
//						haveReturn = ch == '\r';
//						continue;
//					}
//
//					if (haveFieldSeparator0)
//					{
//						if (expectQuote)
//						{
//							item.Value += "\"";
//							item.ErrorUnescapedQuote = true;
//							expectQuote = false;
//						}
//						item.Value += fieldSeparator[0];
//						if (quoted && fieldSeparator[0] == '"')
//						{
//							item.ErrorUnescapedQuote = true;
//						}
//						haveFieldSeparator0 = false;
//						haveReturn = false;
//					}
//					if (haveReturn)
//					{
//						if (expectQuote)
//						{
//							item.Value += "\"";
//							item.ErrorUnescapedQuote = true;
//							expectQuote = false;
//						}
//						item.Value += "\r";
//						haveReturn = false;
//					}
//					if (ch == '\r')
//					{
//						haveReturn = true;
//						continue;
//					}
//
//					if (ch == '"' && quoted)
//					{
//						if (expectQuote)
//						{
//							item.Value += "\"";
//							expectQuote = false;
//						}
//						else
//						{
//							expectQuote = true;
//						}
//						continue;
//					}
//					if (quotedFieldsSupported && ch == '"' && item.Value.Length == 0 && !expectQuote)
//					{
//						item.IsQuoted = true;
//						quoted = true;
//						continue;
//					}
//
//
//
//
//					if (expectQuote)
//					{
//						item.ErrorUnescapedQuote = true;
//						item.Value += "\"";
//						expectQuote = false;
//					}
//
//
//					item.Value += ch;
//				}
//				if (haveFieldSeparator0)
//				{
//					if (!quoted || fieldSeparator[0] != '"')
//					{
//						item.Value += fieldSeparator[0];
//					}
//				}
//				else
//				{
//					if (expectQuote)
//					{
//						item.Value += "\"";
//						item.ErrorUnescapedQuote = true;
//					}
//					if (haveReturn)
//					{
//						item.Value += "\r";
//					}
//				}
//				if (item.Value.Length > 0 || item.IsQuoted)
//				{
//					if (quoted)
//					{
//						item.ErrorMissingTrailingQuote = true;
//					}
//					items.Add(item);
//				}
//				if (items.Count > 0)
//				{
//					result.Add(items);
//				}
//				return result;
//			}
		}
	}
}