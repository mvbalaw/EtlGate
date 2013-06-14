using System;
using System.Collections.Generic;
using System.IO;

using FluentAssert;

using JetBrains.Annotations;

using NUnit.Framework;

namespace EtlGate.Tests
{
	[UsedImplicitly]
	public class PipelineTests
	{
		[TestFixture]
		public class When_asked_to_write_out_a_sorted_file
		{

			[Test, Explicit]
			public void Given_an_unsorted_file__should_return_a_sorted_file()
			{

				var headings = new[] { "Number", "Name" };
				var records = new List<Record>
				{
					new Record(new[] { "2", "Two" }, headings),
					new Record(new[] { "4", "Four" }, headings),
					new Record(new[] { "6", "Six" }, headings),
					new Record(new[] { "8", "Eight" }, headings),
					new Record(new[] { "9", "Nine" }, headings),
					new Record(new[] { "7", "Seven" }, headings),
					new Record(new[] { "5", "Five" }, headings),
					new Record(new[] { "3", "Three" }, headings),
					new Record(new[] { "1", "One" }, headings)
				};				


				var reader = new CsvReader(new DelimitedDataReader(new StreamTokenizer()));
				var writer = new CsvWriter();
				var comparer = new RecordKeyComparer(new StringFieldComparer("Number"));

				writer.WriteTo("UnsortedNumbers.csv", records, true);

				var sorted = reader
					.ReadFrom(File.OpenRead("UnsortedNumbers.csv"), "\r\n", true)
					.Sort(comparer);
				writer.WriteTo("SortedNumbers.csv", sorted, true);

				var actual = reader.ReadFrom(File.OpenRead("SortedNumbers.csv"), "\r\n", true);

				var lastNumber = "";
				Console.WriteLine("Number, Name");
				foreach (var record in actual)
				{
					Console.WriteLine("{0}, {1}", record["Number"], record["Name"]);
					record["Number"].ShouldBeGreaterThan(lastNumber);
					lastNumber = record["Number"];
				}

			}

		}
	}
}