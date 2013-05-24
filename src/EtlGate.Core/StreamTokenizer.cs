using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EtlGate.Core
{
	public interface IStreamTokenizer
	{
		void PushBack(char[] chars);
		IEnumerable<Token> Tokenize(Stream stream, params char[] specials);
	}

	public class StreamTokenizer : IStreamTokenizer
	{
		public const string ErrorSpecialCharactersMustBeSpecified = "Special characters must be specified.";
		public const string ErrorStreamCannotBeNull = "Stream cannot be null.";
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
			if (chars != null && chars.Length > 0)
			{
				_pushBack = chars;
			}
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

			var data = new StringBuilder();
			using (var reader = new StreamReader(stream))
			{
				var next = new char[_readBufferSize];
				int count;
				do
				{
					count = reader.Read(next, 0, _readBufferSize);
					if (count == 0)
					{
						break;
					}

					foreach (var token in TokenizeArray(next, count, lookup, data))
					{
						yield return token;
					}
				} while (count > 0);
			}

			while (data.Length > 0)
			{
				yield return new DataToken(data.ToString());
				data.Length = 0;
				if (_pushBack != null)
				{
					var copy = _pushBack;
					_pushBack = null;
					foreach (var token in TokenizeArray(copy, copy.Length, lookup, data))
					{
						yield return token;
					}
				}
			}
		}

		private void HandlePushback(ref char[] next, ref int i, ref int count)
		{
			if (_pushBack.Length - 1 <= i)
			{
				i = i - _pushBack.Length;
				_pushBack = null;
				return;
			}
			var lenOfRemainder = count - 1 - i;
			var remainderStartIndex = i + 1;
			var newNext = new char[lenOfRemainder + _pushBack.Length];
			Array.Copy(_pushBack, newNext, _pushBack.Length);
			Array.Copy(next, remainderStartIndex, newNext, _pushBack.Length, lenOfRemainder);
			next = newNext;
			i = -1;
			count = newNext.Length;
			_pushBack = null;
		}

		private IEnumerable<Token> TokenizeArray(char[] next, int count, IList<bool> lookup, StringBuilder data)
		{
			for (var i = 0; i < count; i++)
			{
				var ch = next[i];
				if (!lookup[ch])
				{
					data.Append(ch);
					continue;
				}

				if (data.Length > 0)
				{
					yield return new DataToken(data.ToString());
					data.Length = 0;
					if (_pushBack != null)
					{
						i--;
						HandlePushback(ref next, ref i, ref count);
						continue;
					}
				}
				yield return new SpecialToken(new String(next, i, 1));
				if (_pushBack != null)
				{
					HandlePushback(ref next, ref i, ref count);
				}
			}
		}
	}
}