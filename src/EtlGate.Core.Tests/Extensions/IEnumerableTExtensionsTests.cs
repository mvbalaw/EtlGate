using System.Collections.Generic;
using System.Linq;

using EtlGate.Core.Extensions;

using FluentAssert;

using NUnit.Framework;

namespace EtlGate.Core.Tests.Extensions
{
// ReSharper disable ClassNeverInstantiated.Global
	public class IEnumerableTExtensionsTests
// ReSharper restore ClassNeverInstantiated.Global
	{
// ReSharper disable ClassNeverInstantiated.Global
		public class When_asked_to_convert_a_List_to_a_LinkedList
// ReSharper restore ClassNeverInstantiated.Global
		{
			[TestFixture]
			public class Given_an_empty_list
			{
				[Test]
				public void Should_return_and_empty_result()
				{
					var input = new int[] { };
					var result = input.ToLinkedList().ToList();
					result.Count.ShouldBeEqualTo(0);
				}
			}

			[TestFixture]
			public class Given_list_containing_1_item
			{
				[Test]
				public void Should_return_1_item()
				{
					var input = new[] { 1 };
					var result = input.ToLinkedList().ToList();
					result.Count.ShouldBeEqualTo(1);
				}
			}

			[TestFixture]
			public class Given_list_containing_2_items
			{
				private IList<int> _input;
				private List<LinkedListNode<int>> _result;

				[TestFixtureSetUp]
				public void Before_first_test()
				{
					_input = new[] { 1, 2 };
					_result = _input.ToLinkedList().ToList();
				}

				[Test]
				public void Should_link_first_result_Next_to_second_result()
				{
					var linkedListNode = _result.First().Next;
					linkedListNode.ShouldNotBeNull();
// ReSharper disable PossibleNullReferenceException
					linkedListNode.Value.ShouldBeEqualTo(_input.Last());
// ReSharper restore PossibleNullReferenceException
				}

				[Test]
				public void Should_link_second_result_Previous_to_first_result()
				{
					var linkedListNode = _result.Last().Previous;
					linkedListNode.ShouldNotBeNull();
// ReSharper disable PossibleNullReferenceException
					linkedListNode.Value.ShouldBeEqualTo(_input.First());
// ReSharper restore PossibleNullReferenceException
				}

				[Test]
				public void Should_return_2_items()
				{
					_result.Count.ShouldBeEqualTo(2);
				}

				[Test]
				public void Should_return_the_first_input_first()
				{
					_result.First().Value.ShouldBeEqualTo(_input.First());
				}

				[Test]
				public void Should_return_the_second_input_second()
				{
					_result.Last().Value.ShouldBeEqualTo(_input.Last());
				}
			}
		}
	}
}