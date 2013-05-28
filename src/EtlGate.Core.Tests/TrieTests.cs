using FluentAssert;

using NUnit.Framework;

namespace EtlGate.Core.Tests
{
	[TestFixture]
	public class TrieTests
	{
		[Test]
		public void Given__an__a__should_have_hierarchy_of_nodes__a_ending__n_ending()
		{
			var trie = new Trie();
			trie.Add("an");
			trie.Add("a");

			var aNode = trie.Get('a');
			aNode.ShouldNotBeNull("a node is missing");
			aNode.IsEnding.ShouldBeTrue("a node should be a ending");
			aNode.Parent.ShouldBeNull("a node's parent should be null");
			aNode.Value.ShouldBeEqualTo("a", "a node should have value 'a'");

			var nNode = aNode.Get('n');
			nNode.ShouldNotBeNull("n node is missing");
			nNode.IsEnding.ShouldBeTrue("n node should be a ending");
			nNode.Parent.ShouldBeSameInstanceAs(aNode);
			nNode.Value.ShouldBeEqualTo("an", "n should have a value 'an'");
		}

		[Test]
		public void Given__and__an__should_have_hierarchy_of_nodes__a_non_ending__n_ending__d_ending()
		{
			var trie = new Trie();
			trie.Add("and");
			trie.Add("an");

			var aNode = trie.Get('a');
			aNode.ShouldNotBeNull("a node is missing");
			aNode.IsEnding.ShouldBeFalse("a node should not be a ending");
			aNode.Parent.ShouldBeNull("a node's parent should be null");
			aNode.Value.ShouldBeNull("a node should not have a value");

			var nNode = aNode.Get('n');
			nNode.ShouldNotBeNull("n node is missing");
			nNode.IsEnding.ShouldBeTrue("n node should be a ending");
			nNode.Parent.ShouldBeSameInstanceAs(aNode);
			nNode.Value.ShouldBeEqualTo("an", "n should have a value 'an'");

			var dNode = nNode.Get('d');
			dNode.ShouldNotBeNull("d node is missing");
			dNode.IsEnding.ShouldBeTrue("d node should be a ending");
			dNode.Parent.ShouldBeSameInstanceAs(nNode);
			dNode.Value.ShouldBeEqualTo("and", "d should have a value 'and'");
		}

		[Test]
		public void Given__and__art__should_have_hierarchy_of_nodes__a_non_ending__n_non_ending__d_ending___and__a_non_ending__r_non_ending__t_ending()
		{
			var trie = new Trie();
			trie.Add("and");
			trie.Add("art");

			var aNode = trie.Get('a');
			aNode.ShouldNotBeNull("a node is missing");
			aNode.IsEnding.ShouldBeFalse("a node should not be a ending");
			aNode.Parent.ShouldBeNull("a node's parent should be null");
			aNode.Value.ShouldBeNull("a node should not have a value");

			var nNode = aNode.Get('n');
			nNode.ShouldNotBeNull("n node is missing");
			nNode.IsEnding.ShouldBeFalse("n node should not be a ending");
			nNode.Parent.ShouldBeSameInstanceAs(aNode);
			nNode.Value.ShouldBeNull("n node should not have a value");

			var dNode = nNode.Get('d');
			dNode.ShouldNotBeNull("d node is missing");
			dNode.IsEnding.ShouldBeTrue("d node should be a ending");
			dNode.Parent.ShouldBeSameInstanceAs(nNode);
			dNode.Value.ShouldBeEqualTo("and", "d should have a value 'and'");

			var rNode = aNode.Get('r');
			rNode.ShouldNotBeNull("r node is missing");
			rNode.IsEnding.ShouldBeFalse("r node should not be a ending");
			rNode.Parent.ShouldBeSameInstanceAs(aNode);
			rNode.Value.ShouldBeNull("r node should not have a value");

			var tNode = rNode.Get('t');
			tNode.ShouldNotBeNull("t node is missing");
			tNode.IsEnding.ShouldBeTrue("t node should be a ending");
			tNode.Parent.ShouldBeSameInstanceAs(rNode);
			tNode.Value.ShouldBeEqualTo("art", "t should have a value 'art'");
		}

		[Test]
		public void Given__and__should_have_hierarchy_of_nodes__a_non_ending__n_non_ending__d_ending()
		{
			var trie = new Trie();
			trie.Add("and");
			var aNode = trie.Get('a');
			aNode.ShouldNotBeNull("a node is missing");
			aNode.IsEnding.ShouldBeFalse("a node should not be a ending");
			aNode.Parent.ShouldBeNull("a node's parent should be null");
			aNode.Value.ShouldBeNull("a node should not have a value");

			var nNode = aNode.Get('n');
			nNode.ShouldNotBeNull("n node is missing");
			nNode.IsEnding.ShouldBeFalse("n node should not be a ending");
			nNode.Parent.ShouldBeSameInstanceAs(aNode);
			nNode.Value.ShouldBeNull("n node should not have a value");

			var dNode = nNode.Get('d');
			dNode.ShouldNotBeNull("d node is missing");
			dNode.IsEnding.ShouldBeTrue("d node should be a ending");
			dNode.Parent.ShouldBeSameInstanceAs(nNode);
			dNode.Value.ShouldBeEqualTo("and", "d should have a value 'and'");
		}

		[Test]
		public void Given__and_nda_dan__should_have_hierarchy_of_nodes__a_non_ending__n_non_ending__d_ending__and__n_non_ending__d_non_ending__a_ending__and__d_non_ending__a_non_ending__n_ending()
		{
			var trie = new Trie();
			trie.Add("and");
			trie.Add("nda");
			trie.Add("dan");

			var aNode = trie.Get('a');
			aNode.ShouldNotBeNull("a node of 'and' is missing");
			aNode.IsEnding.ShouldBeFalse("a node of 'and' should not be a ending");
			aNode.Parent.ShouldBeNull("a node of 'and' should have null parent");
			aNode.Value.ShouldBeNull("a node of 'and' should not have a value");

			var nNode = aNode.Get('n');
			nNode.ShouldNotBeNull("n node of 'and' is missing");
			nNode.IsEnding.ShouldBeFalse("n node of 'and' should not be a ending");
			nNode.Parent.ShouldBeSameInstanceAs(aNode);
			nNode.Value.ShouldBeNull("n node of 'and' should not have a value");

			var dNode = nNode.Get('d');
			dNode.ShouldNotBeNull("d node of 'and' is missing");
			dNode.IsEnding.ShouldBeTrue("d node of 'and' should be a ending");
			dNode.Parent.ShouldBeSameInstanceAs(nNode);
			dNode.Value.ShouldBeEqualTo("and", "d node of 'and' should have a value 'and'");

			nNode = trie.Get('n');
			nNode.ShouldNotBeNull("n node of 'nda' is missing");
			nNode.IsEnding.ShouldBeFalse("n node of 'nda' should not be a ending");
			nNode.Parent.ShouldBeNull("n node of 'nda' should have null parent");
			nNode.Value.ShouldBeNull("n node of 'nda' should not have a value");

			dNode = nNode.Get('d');
			dNode.ShouldNotBeNull("d node of 'nda' is missing");
			dNode.IsEnding.ShouldBeFalse("d node of 'nda' should not be a ending");
			dNode.Parent.ShouldBeSameInstanceAs(nNode);
			dNode.Value.ShouldBeNull("d node of 'nda' should not have a value");

			aNode = dNode.Get('a');
			aNode.ShouldNotBeNull("a node of 'nda' is missing");
			aNode.IsEnding.ShouldBeTrue("a node of 'nda' should be a ending");
			aNode.Parent.ShouldBeSameInstanceAs(dNode);
			aNode.Value.ShouldBeEqualTo("nda", "a node of 'nda' should have a value 'nda'");

			dNode = trie.Get('d');
			dNode.ShouldNotBeNull("d node of 'dan' is missing");
			dNode.IsEnding.ShouldBeFalse("d node of 'dan' should not be a ending");
			dNode.Parent.ShouldBeNull("d node of 'dan' should have null parent");
			dNode.Value.ShouldBeNull("d node of 'dan' should not have a value");

			aNode = dNode.Get('a');
			aNode.ShouldNotBeNull("a node of 'dan' is missing");
			aNode.IsEnding.ShouldBeFalse("a node of 'dan' should not be a ending");
			aNode.Parent.ShouldBeSameInstanceAs(dNode);
			aNode.Value.ShouldBeNull("a node of 'dan' should not have a value");

			nNode = aNode.Get('n');
			nNode.ShouldNotBeNull("n node of 'dan' is missing");
			nNode.IsEnding.ShouldBeTrue("n node of 'dan' should be a ending");
			nNode.Parent.ShouldBeSameInstanceAs(aNode);
			nNode.Value.ShouldBeEqualTo("dan", "n node of 'dan' should have a value 'dan'");
		}
	}
}