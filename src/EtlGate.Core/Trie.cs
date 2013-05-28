using System.Collections.Generic;

namespace EtlGate.Core
{
	public class Trie
	{
		private readonly IDictionary<char, TrieNode> _nodes = new Dictionary<char, TrieNode>();

		public void Add(string value)
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

		public TrieNode(char key, string value, TrieNode parent)
		{
			Value = value;
			Parent = parent;
			Key = key;
		}

		public bool IsEnding
		{
			get { return Value != null; }
		}
		public char Key { get; private set; }
		public TrieNode Parent { get; private set; }
		public string Value { get; internal set; }

		public TrieNode Add(char ch, string value)
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

		public TrieNode Get(char ch)
		{
			TrieNode node;
			_nodes.TryGetValue(ch, out node);
			return node;
		}
	}
}