using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using JetBrains.Annotations;

namespace EtlGate.Core
{
	public interface IStreamTokenizer
	{
		void PushBack([NotNull] StringBuilder content);
		void PushBack([NotNull] string content);
		void PushBack([NotNull] char[] chars);

		[NotNull]
		IEnumerable<Token> Tokenize([NotNull] Stream stream, [NotNull] params char[] specials);

		[NotNull]
		IEnumerable<Token> Tokenize([NotNull] Stream stream, [NotNull] params string[] specials);
	}

	public class StreamTokenizer : IStreamTokenizer
	{
		public const string ErrorSpecialCharactersMustBeSpecified = "Special characters must be specified.";
		public const string ErrorStreamCannotBeNull = "Stream cannot be null.";
		private Action<char[]> _doPushBack;
		private char[] _pushBack;
		private int _readBufferSize = 4096;
		public int ReadBufferSize
		{
			get { return _readBufferSize; }
			set
			{
				if (value < 1)
				{
					throw new ArgumentOutOfRangeException("value", "buffer size must be greater than zero.");
				}
				_readBufferSize = value;
			}
		}

		public void PushBack(char[] chars)
		{
			_doPushBack(chars);
		}

		public IEnumerable<Token> Tokenize(Stream stream, params char[] specials)
		{
			if (stream == null)
			{
				throw new ArgumentException(ErrorStreamCannotBeNull, "stream");
			}
			if (specials == null)
			{
				throw new ArgumentException(ErrorSpecialCharactersMustBeSpecified, "specials");
			}

			var lookup = new bool[char.MaxValue];
			foreach (var ch in specials)
			{
				lookup[ch] = true;
			}

			_doPushBack = AllowPushBack;
			using (var reader = new StreamReader(stream))
			{
				var next = new char[_readBufferSize];
				var count = reader.Read(next, 0, _readBufferSize);
				while (count > 0)
				{
					foreach (var token in TokenizeArray(next, count, lookup))
					{
						yield return token;
					}
					next = new char[_readBufferSize];
					count = reader.Read(next, 0, _readBufferSize);
				}
			}

			do
			{
				while (_pushBack != null)
				{
					var copy = _pushBack;
					_pushBack = null;
					_doPushBack = AllowPushBack;
					foreach (var token in TokenizeArray(copy, copy.Length, lookup))
					{
						yield return token;
					}
				}
				yield return new EndOfStreamToken();
			} while (_pushBack != null);
		}

		public IEnumerable<Token> Tokenize(Stream stream, params string[] specialTokens)
		{
			if (stream == null)
			{
				throw new ArgumentException(ErrorStreamCannotBeNull, "stream");
			}
			if (specialTokens == null)
			{
				throw new ArgumentException(ErrorSpecialCharactersMustBeSpecified, "specialTokens");
			}

			var trie = new Trie();
			foreach (var specialToken in specialTokens)
			{
				trie.Add(specialToken);
			}
			var specialsBuffer = new List<TrieNode>();

			_doPushBack = ThrowOnPushBack;
			foreach (var token in Tokenize(stream, specialTokens.SelectMany(x => x).Distinct().ToArray()))
			{
				if (token is SpecialToken)
				{
					if (!specialsBuffer.Any())
					{
						var node = trie.Get(token.Value[0]);
						if (node == null)
						{
							yield return new DataToken(token);
						}
						else
						{
							specialsBuffer.Add(node);
						}
						continue;
					}

					{
						var parentNode = specialsBuffer.Last();
						var node = parentNode.Get(token.Value[0]);
						if (node == null)
						{
							var buffer = new StringBuilder(specialsBuffer.Count);
							parentNode = specialsBuffer.LastOrDefault(x => x.IsEnding);
							if (parentNode != null)
							{
								var index = specialsBuffer.LastIndexOf(parentNode);
								var value = parentNode.Value;
								Debug.Assert(value != null, "value != null");
								yield return new SpecialToken(value.ToCharArray(), 0, value.Length);
								Combine(buffer, specialsBuffer, index + 1, specialsBuffer.Count - 1);
								specialsBuffer.Clear();
								if (buffer.Length > 0)
								{
									buffer.Append(token.Value);
									PushBack(buffer);
								}
								else
								{
									node = trie.Get(token.Value[0]);
									if (node == null)
									{
										yield return new DataToken(token);
									}
									else
									{
										specialsBuffer.Add(node);
									}
								}
								continue;
							}

							yield return new DataToken(new[] { specialsBuffer.First().Key }, 0, 1);
							Combine(buffer, specialsBuffer, 1, specialsBuffer.Count - 1);
							specialsBuffer.Clear();
							buffer.Append(token.Value);
							PushBack(buffer);
							continue;
						}
						specialsBuffer.Add(node);
						continue;
					}
				}

				if (specialsBuffer.Any())
				{
					var buffer = new StringBuilder(specialsBuffer.Count);

					var parentNode = specialsBuffer.LastOrDefault(x => x.IsEnding);
					if (parentNode != null)
					{
						var index = specialsBuffer.LastIndexOf(parentNode);
						var value = parentNode.Value;
						Debug.Assert(value != null, "value != null");
						yield return new SpecialToken(value.ToCharArray(), 0, value.Length);
						Combine(buffer, specialsBuffer, index + 1, specialsBuffer.Count - 1);
						specialsBuffer.Clear();
						if (buffer.Length > 0)
						{
							if (token is DataToken)
							{
								buffer.Append(token.Value);
							}

							PushBack(buffer);
						}
						else
						{
							yield return token;
						}
						continue;
					}

					yield return new DataToken(new[] { specialsBuffer.First().Key }, 0, 1);
					Combine(buffer, specialsBuffer, 1, specialsBuffer.Count - 1);
					specialsBuffer.Clear();
					if (token is DataToken)
					{
						buffer.Append(token.Value);
					}
					if (buffer.Length > 0)
					{
						PushBack(buffer);
					}
					else
					{
						yield return token;
					}
					continue;
				}

				yield return token;
			}
		}

		public void PushBack(StringBuilder content)
		{
			PushBack(content.ToString());
		}

		public void PushBack(string content)
		{
			PushBack(content.ToCharArray());
		}

		private void AllowPushBack(char[] chars)
		{
			if (chars != null && chars.Length > 0)
			{
				_doPushBack = ThrowOnDoublePushBack;
				_pushBack = chars;
			}
		}

		private static StringBuilder Combine(StringBuilder buffer, IList<TrieNode> specialsBuffer, int start, int stop)
		{
			for (var i = start; i <= stop; i++)
			{
				buffer.Append(specialsBuffer[i].Key);
			}
			return buffer;
		}

		private void HandlePushBack(ref char[] next, ref int i, ref int count)
		{
			_doPushBack = AllowPushBack;
			var overlap = Math.Min(_pushBack.Length, Math.Min(i + 1, count));
			var lenOfRemainder = _pushBack.Length - overlap;
			i -= overlap;
			if (lenOfRemainder == 0)
			{
				_pushBack = null;
				return;
			}
			var newNext = new char[lenOfRemainder + count];
			Array.Copy(_pushBack, 0, newNext, 0, lenOfRemainder);
			Array.Copy(next, 0, newNext, lenOfRemainder, count);
			next = newNext;
			i = -1;
			count = newNext.Length;
			_pushBack = null;
		}

		private static void ThrowOnDoublePushBack(char[] chars)
		{
			throw new NotImplementedException("Don't push twice in a row.");
		}

		private static void ThrowOnPushBack(char[] chars)
		{
			throw new NotImplementedException("Push back not supported when tokenizing with string specials.");
		}

		private IEnumerable<Token> TokenizeArray(char[] next, int count, IList<bool> lookup)
		{
			var dataStart = 0;
			var i = 0;
			do
			{
				for (; i < count; i++)
				{
					var ch = next[i];
					if (!lookup[ch])
					{
						continue;
					}

					if (i - dataStart > 0)
					{
						yield return new DataToken(next, dataStart, i - dataStart);
						if (_pushBack != null)
						{
							i--;
							HandlePushBack(ref next, ref i, ref count);
							dataStart = i + 1;
							continue;
						}
					}
					yield return new SpecialToken(next, i, 1);
					if (_pushBack != null)
					{
						HandlePushBack(ref next, ref i, ref count);
					}
					dataStart = i + 1;
				}

				if (i - dataStart > 0)
				{
					yield return new DataToken(next, dataStart, i - dataStart);
					if (_pushBack != null)
					{
						HandlePushBack(ref next, ref i, ref count);
						if (i < 0)
						{
							i = 0;
						}
					}
					dataStart = i;
				}
			} while (dataStart < count);
		}
	}
}