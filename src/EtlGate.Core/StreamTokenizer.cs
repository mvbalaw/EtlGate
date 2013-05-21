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

			var specialsHash = new HashSet<char>(specials);

			var data = new StringBuilder();
			using (var reader = new StreamReader(stream))
			{
				var next = new char[1];
				int count;
				do
				{
					count = reader.Read(next, 0, 1);
					if (count == 0)
					{
						break;
					}

					if (!specialsHash.Contains(next[0]))
					{
						data.Append(next[0]);
						continue;
					}

					if (data.Length > 0)
					{
						yield return new Token(TokenType.Data, data.ToString());
						data.Length = 0;
					}
					yield return new Token(TokenType.Special, new String(next));
				} while (count > 0);
			}

			if (data.Length > 0)
			{
				yield return new Token(TokenType.Data, data.ToString());
			}
		}
	}
}