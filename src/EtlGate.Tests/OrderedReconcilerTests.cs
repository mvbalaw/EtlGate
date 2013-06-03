using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssert;

using JetBrains.Annotations;

using NUnit.Framework;

namespace EtlGate.Tests
{
	[UsedImplicitly]
	public class OrderedReconcilerTests
	{
		[TestFixture]
		public class When_asked_to_reconcile_two_IEnumerables
		{
			private OrderedReconciler<KeyValuePair<int, char>> _comparer;

			[SetUp]
			public void Before_each_test()
			{
				_comparer = new OrderedReconciler<KeyValuePair<int, char>>();
			}

			[Test]
			[ExpectedException(typeof(ArgumentException), ExpectedMessage = OrderedReconciler<string>.ErrorNotSortedMessage)]
			public void Given_left_not_sorted_and_right_empty__should_throw_argument_exception_on_unsorted_enumerable()
			{
				var left = new List<KeyValuePair<int, char>>();
				var right = new List<KeyValuePair<int, char>>();

				left.Add(new KeyValuePair<int, char>(4, 'D'));
				left.Add(new KeyValuePair<int, char>(3, 'C'));

// ReSharper disable ReturnValueOfPureMethodIsNotUsed
				_comparer.Reconcile(left, right, GetState).ToList();
// ReSharper restore ReturnValueOfPureMethodIsNotUsed

			}

			[Test]
			[ExpectedException(typeof(ArgumentException), ExpectedMessage = OrderedReconciler<string>.ErrorNotSortedMessage)]
			public void Given_left_empty_and_right_not_sorted__should_throw_argument_exception_on_unsorted_enumerable()
			{
				var left = new List<KeyValuePair<int, char>>();
				var right = new List<KeyValuePair<int, char>>
				{
					new KeyValuePair<int, char>(4, 'D'),
					new KeyValuePair<int, char>(3, 'C')
				};

				// ReSharper disable ReturnValueOfPureMethodIsNotUsed
				_comparer.Reconcile(left, right, GetState).ToList();
				// ReSharper restore ReturnValueOfPureMethodIsNotUsed

			}


			[Test]
			[ExpectedException(typeof(ArgumentException), ExpectedMessage = OrderedReconciler<string>.ErrorNotSortedMessage)]
			public void Given_left_has_items_and_right_not_sorted__should_throw_argument_exception_on_unsorted_enumerable()
			{
				var left = new List<KeyValuePair<int, char>>();
				var right = new List<KeyValuePair<int, char>>();

				left.Add(new KeyValuePair<int, char>(3, 'C'));
				left.Add(new KeyValuePair<int, char>(4, 'D'));
				right.Add(new KeyValuePair<int, char>(2, 'B'));
				right.Add(new KeyValuePair<int, char>(1, 'A'));

				// ReSharper disable ReturnValueOfPureMethodIsNotUsed
				_comparer.Reconcile(left, right, GetState).ToList();
				// ReSharper restore ReturnValueOfPureMethodIsNotUsed

			}

			[Test]
			[ExpectedException(typeof(ArgumentException), ExpectedMessage = OrderedReconciler<string>.ErrorNotSortedMessage)]
			public void Given_left_not_sorted_and_right_has_items__should_throw_argument_exception_on_unsorted_enumerable()
			{
				var left = new List<KeyValuePair<int, char>>();
				var right = new List<KeyValuePair<int, char>>();

				left.Add(new KeyValuePair<int, char>(2, 'B'));
				left.Add(new KeyValuePair<int, char>(1, 'A'));
				right.Add(new KeyValuePair<int, char>(3, 'C'));
				right.Add(new KeyValuePair<int, char>(4, 'D'));

				// ReSharper disable ReturnValueOfPureMethodIsNotUsed
				_comparer.Reconcile(left, right, GetState).ToList();
				// ReSharper restore ReturnValueOfPureMethodIsNotUsed

			}

			[Test]
			public void Given_left_and_right_alternating__should_return_correct_results()
			{
				var left = Enumerable.Range(1, 3).Where(x => (x & 1) == 1).Select(x => new KeyValuePair<int, char>(x, 'A')).ToList();
				var right = Enumerable.Range(2, 3).Where(x => (x & 1) == 0).Select(x => new KeyValuePair<int, char>(x, 'B')).ToList();
				var result = _comparer.Reconcile(left, right, GetState).ToList();
				result.Count.ShouldBeEqualTo(4);

				var first = result[0];
				first.Item.ShouldBeEqualTo(left[0]);
				first.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Deleted.Key);

				var second = result[1];
				second.Item.ShouldBeEqualTo(right[0]);
				second.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Added.Key);

				var third = result[2];
				third.Item.ShouldBeEqualTo(left[1]);
				third.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Deleted.Key);

				var fourth = result[3];
				fourth.Item.ShouldBeEqualTo(right[1]);
				fourth.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Added.Key);
			}

			[Test]
			public void Given_left_and_right_have_both_overlapping_and_unique_items_where_left_starts_before_right__should_return_correct_results()
			{
				var left = Enumerable.Range(1, 3).Select(x => new KeyValuePair<int, char>(x, 'A')).ToList();
				var right = Enumerable.Range(2, 3).Select(x => new KeyValuePair<int, char>(x, x == 2 ? 'A' : 'B')).ToList();
				var result = _comparer.Reconcile(left, right, GetState).ToList();
				result.Count.ShouldBeEqualTo(4);

				var first = result[0];
				first.Item.ShouldBeEqualTo(left[0]);
				first.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Deleted.Key);

				var second = result[1];
				second.Item.ShouldBeEqualTo(left[1]);
				second.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Same.Key);

				var third = result[2];
				third.Item.ShouldBeEqualTo(right[1]);
				third.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Updated.Key);

				var fourth = result[3];
				fourth.Item.ShouldBeEqualTo(right[2]);
				fourth.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Added.Key);
			}

			[Test]
			public void Given_left_and_right_have_both_overlapping_and_unique_items_where_right_starts_before_left__should_return_correct_results()
			{
				var left = Enumerable.Range(2, 3).Select(x => new KeyValuePair<int, char>(x, 'A')).ToList();
				var right = Enumerable.Range(1, 3).Select(x => new KeyValuePair<int, char>(x, x == 2 ? 'A' : 'B')).ToList();
				var result = _comparer.Reconcile(left, right, GetState).ToList();
				result.Count.ShouldBeEqualTo(4);

				var first = result[0];
				first.Item.ShouldBeEqualTo(right[0]);
				first.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Added.Key);

				var second = result[1];
				second.Item.ShouldBeEqualTo(right[1]);
				second.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Same.Key);

				var third = result[2];
				third.Item.ShouldBeEqualTo(right[2]);
				third.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Updated.Key);

				var fourth = result[3];
				fourth.Item.ShouldBeEqualTo(left[2]);
				fourth.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Deleted.Key);
			}

			[Test]
			public void Given_the_left_enumerable_has_items_and_the_right_has_same_items__should_return_results_for_all_left_items_as_Same()
			{
				var left = Enumerable.Range(1, 2).Select(x => new KeyValuePair<int, char>(x, 'A')).ToList();
				var right = Enumerable.Range(1, 2).Select(x => new KeyValuePair<int, char>(x, 'A')).ToList();
				var result = _comparer.Reconcile(left, right, GetState).ToList();
				result.Count.ShouldBeEqualTo(2);

				var first = result.First();
				first.Item.ShouldBeEqualTo(left.First());
				first.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Same.Key);

				var second = result.Last();
				second.Item.ShouldBeEqualTo(left.Last());
				second.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Same.Key);
			}

			[Test]
			public void Given_the_left_enumerable_has_items_and_the_right_has_updated_items__should_return_results_for_all_right_items_as_Updated()
			{
				var left = Enumerable.Range(1, 2).Select(x => new KeyValuePair<int, char>(x, 'A')).ToList();
				var right = Enumerable.Range(1, 2).Select(x => new KeyValuePair<int, char>(x, 'B')).ToList();
				var result = _comparer.Reconcile(left, right, GetState).ToList();
				result.Count.ShouldBeEqualTo(2);

				var first = result.First();
				first.Item.ShouldBeEqualTo(right.First());
				first.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Updated.Key);

				var second = result.Last();
				second.Item.ShouldBeEqualTo(right.Last());
				second.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Updated.Key);
			}

			[Test]
			public void Given_the_left_enumerable_has_items_and_the_right_is_empty__should_return_results_for_all_left_items_as_Deleted()
			{
				var left = Enumerable.Range(1, 2).Select(x => new KeyValuePair<int, char>(x, 'A')).ToList();
				var right = new List<KeyValuePair<int, char>>();
				var result = _comparer.Reconcile(left, right, GetState).ToList();
				result.Count.ShouldBeEqualTo(2);

				var first = result.First();
				first.Item.ShouldBeEqualTo(left.First());
				first.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Deleted.Key);

				var second = result.Last();
				second.Item.ShouldBeEqualTo(left.Last());
				second.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Deleted.Key);
			}

			[Test]
			public void Given_the_left_enumerable_is_empty_and_the_right_has_items__should_return_results_for_all_right_items_as_Added()
			{
				var left = new List<KeyValuePair<int, char>>();
				var right = Enumerable.Range(1, 2).Select(x => new KeyValuePair<int, char>(x, 'A')).ToList();
				var result = _comparer.Reconcile(left, right, GetState).ToList();
				result.Count.ShouldBeEqualTo(2);

				var first = result.First();
				first.Item.ShouldBeEqualTo(right.First());
				first.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Added.Key);

				var second = result.Last();
				second.Item.ShouldBeEqualTo(right.Last());
				second.Status.Key.ShouldBeEqualTo(ReconciliationStatus.Added.Key);
			}

			private static ReconciliationStatus GetState(KeyValuePair<int, char> x, KeyValuePair<int, char> y)
			{
				if (x.Key < y.Key)
				{
					return ReconciliationStatus.Deleted;
				}
				if (x.Key > y.Key)
				{
					return ReconciliationStatus.Added;
				}

				return x.Value == y.Value ? ReconciliationStatus.Same : ReconciliationStatus.Updated;
			}
		}
	}
}