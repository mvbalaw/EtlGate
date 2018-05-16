using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using FluentAssert;

using JetBrains.Annotations;

using NUnit.Framework;

namespace EtlGate.Tests
{
	[UsedImplicitly]
	public class CsvWriterTests
	{
		[TestFixture]
		public class When_asked_to_write_to_a_stream
		{
			private ICsvWriter _writer;

			[SetUp]
			public void Before_each_test()
			{
				_writer = new CsvWriter();
			}

			[Test]
			public void Given_a_field_with_a_null_value__should_not_put_double_quotes()
			{
				var headings = new[] { "Field1" };
				var records = new List<Record>
				{
					new Record(new string[] { null }, headings)
				};

				const string expected = "\r\n";
				const bool hasHeaderRow = false;
				Check(records, hasHeaderRow, expected);
			}

			[Test]
			public void Given_a_field_with_embedded_comma__should_put_double_quotes_around_field()
			{
				var headings = new[] { "Field1" };
				var records = new List<Record>
				{
					new Record(new[] { "Value1," }, headings)
				};

				const string expected = "\"Value1,\"\r\n";
				const bool hasHeaderRow = false;
				Check(records, hasHeaderRow, expected);
			}

			[Test]
			public void Given_a_field_with_embedded_new_line__should_put_double_quotes_around_field()
			{
				var headings = new[] { "Field1" };
				var records = new List<Record>
				{
					new Record(new[] { "Val\r\nue1" }, headings)
				};

				const string expected = "\"Val\r\nue1\"\r\n";
				const bool hasHeaderRow = false;
				Check(records, hasHeaderRow, expected);
			}

			[Test]
			public void Given_a_field_with_embedded_double_quote__should_put_double_quotes_around_field_and_add_extra_double_quote()
			{
				var headings = new[] { "Field1" };
				var records = new List<Record>
				{
					new Record(new[] { "\"Value1" }, headings)
				};

				const string expected = "\"\"\"Value1\"\r\n";
				const bool hasHeaderRow = false;
				Check(records, hasHeaderRow, expected);
			}

			[Test]
			public void Given_a_field_with_embedded_all_special_chars__should_put_double_quotes_around_field_and_add_extra_double_quotes()
			{
				var headings = new[] { "Field1" };
				var records = new List<Record>
				{
					new Record(new[] { "\"Val\r\nue,1" }, headings)
				};

				const string expected = "\"\"\"Val\r\nue,1\"\r\n";
				const bool hasHeaderRow = false;
				Check(records, hasHeaderRow, expected);
			}

			[Test]
			public void Given_a_row_with_field_that_has_embedded_comma__should_only_put_double_quotes_around_that_field()
			{
				var headings = new[] { "Field1", "Field2" };
				var records = new List<Record>
				{
					new Record(new[] { "Value,1", "Value2" }, headings)
				};

				const string expected = "\"Value,1\",Value2\r\n";
				const bool hasHeaderRow = false;
				Check(records, hasHeaderRow, expected);
			}

			[Test]
			public void Given_a_list_of_records_with_headers__should_write_correct_values_to_stream()
			{
				var headings = new[] { "Field1", "Field2", "Field3" };
				var records = new List<Record>
				{
					new Record(new[] { "Record1Value1", "Record1Value2", "Record1Value3" }, headings),
					new Record(new[] { "Record2Value1", "Record2Value2", "Record2Value3" }, headings)
				};

				const string expected = "Field1,Field2,Field3\r\n"
				                        + "Record1Value1,Record1Value2,Record1Value3\r\n"
				                        + "Record2Value1,Record2Value2,Record2Value3\r\n";
				const bool hasHeaderRow = true;
				Check(records, hasHeaderRow, expected);
			}


			[Test]
			public void Given_a_list_of_records_with_headers_and_embedded_special_chars__should_write_correct_values_to_stream()
			{
				var headings = new[] { "Field1", "Field2", "Field3" };
				var records = new List<Record>
				{
					new Record(new[] { "Record 1 Value 1", "Record 1, Value 2", "Record 1 Value 3" }, headings),
					new Record(new[] { "Record 2\r\n Value 1", "Record 2 Value 2", "\"Record 2 Value 3" }, headings)
				};

				const string expected = "Field1,Field2,Field3\r\n"
				                        + "Record 1 Value 1,\"Record 1, Value 2\",Record 1 Value 3\r\n"
				                        + "\"Record 2\r\n Value 1\",Record 2 Value 2,\"\"\"Record 2 Value 3\"\r\n";
				const bool hasHeaderRow = true;
				Check(records, hasHeaderRow, expected);
			}


			[Test]
			public void Given_a_list_of_records_and_hasHeaderRow_false__should_write_correct_values_to_stream()
			{
				var headings = new[] { "Field1", "Field2", "Field3" };
				var records = new List<Record>
				{
					new Record(new[] { "Record 1 Value 1", "Record 1 Value 2", "Record 1 Value 3" }, headings),
					new Record(new[] { "Record 2 Value 1", "Record 2 Value 2", "Record 2 Value 3" }, headings),
					new Record(new[] { "Record 3 Value 1", "Record 3 Value 2", "Record 3 Value 3" }, headings)
				};

				const string expected = "Record 1 Value 1,Record 1 Value 2,Record 1 Value 3\r\n"
				                        + "Record 2 Value 1,Record 2 Value 2,Record 2 Value 3\r\n"
				                        + "Record 3 Value 1,Record 3 Value 2,Record 3 Value 3\r\n";
				const bool hasHeaderRow = false;
				Check(records, hasHeaderRow, expected);
			}

			[Test, Ignore]
			public void Writing_10000000_rows_should_be_fast()
			{
				var count = 1;
				var headings = "abcdefghijklmnopqrstuvwxyz".Split();
				var headingsDict = Enumerable.Range(0, headings.Length)
					.ToDictionary(headingIndex => headings[headingIndex], fieldIndex => fieldIndex);
				var records = Enumerable.Range(0, 10000000)
					.Select(x => new Record(headings.Select(y => count++.ToString()), headingsDict));
				using (var stream = new MemoryStream())
				{
					var writer = new StreamWriter(stream);
					_writer.WriteTo(writer, records, true);
					writer.Flush();
				}
			}

			private void Check(IEnumerable<Record> records, bool hasHeader, string expectedValue)
			{
				using (var stream = new MemoryStream())
				{

					var writer = new StreamWriter(stream);
					_writer.WriteTo(writer, records, hasHeader);
					writer.Flush();
					stream.Position = 0;

					using (var reader = new StreamReader(stream))
					{
						var actual = reader.ReadToEnd();
						actual.ShouldBeEqualTo(expectedValue);
					}
				}
			}
		}

		[TestFixture]
		public class When_asked_to_write_to_a_file
		{
			private ICsvWriter _writer;

			[SetUp]
			public void Before_each_test()
			{
				_writer = new CsvWriter();
			}

			[Test,ExpectedException(typeof(ArgumentException))]
			public void Given_a_null_filename__should_throw_an_ArgumentException()
			{
				string fileName = null;
				var records = new List<Record>();
				const bool includeHeaders = true;

// ReSharper disable ExpressionIsAlwaysNull
				_writer.WriteTo(fileName, records, includeHeaders);
// ReSharper restore ExpressionIsAlwaysNull
				
			}

			[Test, ExpectedException(typeof(ArgumentException))]
			public void Given_a_null_list_of_records__should_throw_an_ArgumentException()
			{
				const string fileName = "testfile.csv";
				IEnumerable<Record> records = null;
				const bool includeHeaders = true;

// ReSharper disable ExpressionIsAlwaysNull
				_writer.WriteTo(fileName, records, includeHeaders);
// ReSharper restore ExpressionIsAlwaysNull

			}


			[Test]
			public void Given_a_list_of_records__should_write_correct_values_to_file()
			{
				var headings = new[] { "Field 1", "Field 2", "Field 3" };
				var records = new List<Record>
				{
					new Record(new[] { "Record 1 Value 1", "Record 1 Value 2", "Record 1 Value 3" }, headings),
					new Record(new[] { "Record 2 Value 1", "Record 2 Value 2", "Record 2 Value 3" }, headings),
					new Record(new[] { "Record 3 Value 1", "Record 3 Value 2", "Record 3 Value 3" }, headings)
				};

				const string expected = "Field 1,Field 2,Field 3\r\n"
										+ "Record 1 Value 1,Record 1 Value 2,Record 1 Value 3\r\n"
				                        + "Record 2 Value 1,Record 2 Value 2,Record 2 Value 3\r\n"
				                        + "Record 3 Value 1,Record 3 Value 2,Record 3 Value 3\r\n";

				const string fileName = "testfile.csv";
				const bool includeHeaders = true;

				_writer.WriteTo(fileName, records, includeHeaders);

				using (var reader = new StreamReader(new FileStream(fileName, FileMode.Open)))
				{
					var actual = reader.ReadToEnd();
					actual.ShouldBeEqualTo(expected);
				}
			}

			[Test]
			[Explicit]
			public void Given_a_large_list_of_records__should_write_values_to_file_in_less_than_15_seconds()
			{
				const int numberOfHeadings = 50;

				var headings = Enumerable
					.Range(0, numberOfHeadings)
					.Select(i => string.Format("Field {0}", i))
					.ToList();

				var records = new List<Record>();
				for (var iRecord = 0; iRecord < 500000; iRecord++)
				{
					var fields = new List<string>();
					for (var iField = 0; iField < numberOfHeadings; iField++)
					{
						if (iField % 10 == 0)
						{
							fields.Add("Embeded, comma" + iField);
						}
						else if (iField % 7 == 0)
						{
							fields.Add("Quote in \"String" + iField);
						}
						else
						{
							fields.Add(string.Format("Simple Value" + iField));
						}
					}
					records.Add(new Record(fields, headings));
				}

				var watch = new Stopwatch();
				watch.Start();
				_writer.WriteTo("bigfile.csv", records, true);
				watch.Stop();
				Console.WriteLine(watch.Elapsed);
				watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(15));
			}
		}
	}
}
