using System.Collections.Generic;

using JetBrains.Annotations;

namespace EtlGate
{
	public class Trie
	{
		private readonly IDictionary<char, TrieNode> _nodes = new Dictionary<char, TrieNode>();

		public void Add([NotNull] string value)
		{
			TrieNode node;
			if (!_nodes.TryGetValue(value[0], out node))
			{
				node = new TrieNode(value[0], value.Length == 1 ? value : null, null);
				_nodes.Add(value[0], node);
			}
			else
			{
				if (value.Length == 1)
				{
					node.Value = value;
				}
			}

			for (var i = 1; i < value.Length; i++)
			{
				node = node.Add(value[i], i == value.Length - 1 ? value : null);
			}
		}

		[CanBeNull]
		[Pure]
		public TrieNode Get(char ch)
		{
			TrieNode node;
			_nodes.TryGetValue(ch, out node);
			return node;
		}
	}

	public class TrieNode
	{
		private readonly IDictionary<char, TrieNode> _nodes = new Dictionary<char, TrieNode>();

		public TrieNode(char key, [CanBeNull] string value, [CanBeNull] TrieNode parent)
		{
			Value = value;
			Parent = parent;
			Key = key;
		}

		public bool IsEnding
		{
			[Pure] get { return Value != null; }
		}
		public char Key { [Pure] get; private set; }
		[CanBeNull]
		public TrieNode Parent { [Pure] get; private set; }
		[CanBeNull]
		public string Value { [Pure] get; internal set; }

		[NotNull]
		public TrieNode Add(char ch, [CanBeNull] string value)
		{
			TrieNode node;
			if (!_nodes.TryGetValue(ch, out node))
			{
				node = new TrieNode(ch, value, this);
				_nodes.Add(ch, node);
			}
			else
			{
				if (value != null)
				{
					node.Value = value;
				}
			}
			return node;
		}

		[CanBeNull]
		[Pure]
		public TrieNode Get(char ch)
		{
			TrieNode node;
			_nodes.TryGetValue(ch, out node);
			return node;
		}
	}
}