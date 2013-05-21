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
		private const int ReadBufferSize = 4096;

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
				var next = new char[ReadBufferSize];
				int count;
				do
				{
					count = reader.Read(next, 0, ReadBufferSize);
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
							yield return new Token(TokenType.Data, data.ToString());
							data.Length = 0;
						}
						yield return new Token(TokenType.Special, new String(next, i, 1));
					}
				} while (count > 0);
			}

			if (data.Length > 0)
			{
				yield return new Token(TokenType.Data, data.ToString());
				data.Length = 0;
			}
		}
	}
}