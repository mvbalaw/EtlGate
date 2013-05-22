using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EtlGate.Core
{
	public interface IStreamTokenizer
	{
		IEnumerable<Token> Tokenize(Stream stream, params char[] specials);
	}

	public class StreamTokenizer : IStreamTokenizer
	{
		public const string ErrorSpecialCharactersMustBeSpecified = "Special characters must be specified.";
		public const string ErrorStreamCannotBeNull = "Stream cannot be null.";
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
						}
						yield return new SpecialToken(new String(next, i, 1));
					}
				} while (count > 0);
			}

			if (data.Length > 0)
			{
				yield return new DataToken(data.ToString());
				data.Length = 0;
			}
		}
	}
}