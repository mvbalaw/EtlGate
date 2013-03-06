using System.Collections.Generic;
using System.Linq;

namespace EtlGate.Core
{
	public interface ITokenMerger
	{
		IEnumerable<Token> Merge(IEnumerable<Token> tokens, params string[] specialTokens);
	}

	public class TokenMerger : ITokenMerger
	{
		public IEnumerable<Token> Merge(IEnumerable<Token> tokens, params string[] specialTokens)
		{
			var mergeContext = new MergeContext
				                   {
					                   Data = "",
					                   Special = null,
					                   SpecialIndex = 0,
					                   CheckingSpecial = false,
					                   SpecialTokensByLengthAscending = specialTokens.OrderBy(x => x.Length).ToArray(),
					                   SpecialTokensByLengthDescending = specialTokens.OrderByDescending(x => x.Length).ToArray(),
				                   };

			foreach (var token in tokens)
			{
				switch (token.TokenType)
				{
					case TokenType.Special:
						if (!mergeContext.CheckingSpecial)
						{
							var match = mergeContext.SpecialTokensByLengthAscending.FirstOrDefault(x => x[0] == token.Value[0]);
							if (match == null)
							{
								mergeContext.Data += token.Value;
								continue;
							}
							mergeContext.Special = match;
							mergeContext.CheckingSpecial = true;
							mergeContext.SpecialIndex = 0;
						}
						foreach (var toYield in ProcessSpecial(token, mergeContext))
						{
							yield return toYield;
						}
						break;
					case TokenType.Data:
						if (mergeContext.CheckingSpecial)
						{
							// didn't make it to the end
							foreach (var toYield in Reprocess(mergeContext.Special.Substring(0, mergeContext.SpecialIndex), mergeContext, false))
							{
								yield return toYield;
							}
						}
						mergeContext.Data += token.Value;
						mergeContext.CheckingSpecial = false;
						break;
				}
			}

			if (mergeContext.CheckingSpecial)
			{
				foreach (var toYield in Reprocess(mergeContext.Special.Substring(0, mergeContext.SpecialIndex), mergeContext, false))
				{
					yield return toYield;
				}
			}
			if (mergeContext.Data.Length > 0)
			{
				yield return new Token
					             {
						             TokenType = TokenType.Data,
						             Value = mergeContext.Data
					             };
			}
		}

		private static IEnumerable<Token> ProcessSpecial(Token token, MergeContext mergeContext)
		{
			if (token.Value[0] != mergeContext.Special[mergeContext.SpecialIndex])
			{
				var current = mergeContext.Special.Substring(0, mergeContext.SpecialIndex) + token.Value;
				var alternate = mergeContext.SpecialTokensByLengthAscending
				                            .Where(x => x.Length >= current.Length)
				                            .FirstOrDefault(x => x.StartsWith(current));
				if (alternate != null)
				{
					mergeContext.Special = alternate;
				}
				else
				{
					mergeContext.Special = current;

					foreach (var toYield in Reprocess(current, mergeContext, true))
					{
						yield return toYield;
					}
					mergeContext.CheckingSpecial = mergeContext.Special.Length > 0;
					yield break;
				}
			}

			if (mergeContext.SpecialIndex == (mergeContext.Special.Length - 1))
			{
//				var match = mergeContext.SpecialTokensByLengthAscending
//				                        .Where(x => x.Length > mergeContext.Special.Length)
//				                        .FirstOrDefault(x => x.StartsWith(mergeContext.Special));
//				if (match != null)
//				{
//					mergeContext.Special = match;
//				}
//				else
				{
					if (mergeContext.Data.Length > 0)
					{
						yield return new Token
							             {
								             TokenType = TokenType.Data,
								             Value = mergeContext.Data
							             };
						mergeContext.Data = "";
					}

					// reached end and no alternate, so special
					token.Value = mergeContext.Special;
					yield return token;
					mergeContext.CheckingSpecial = false;
					yield break;
				}
			}

			mergeContext.SpecialIndex++;
		}

		private static IEnumerable<Token> Reprocess(string input, MergeContext mergeContext, bool checkForLongAlternate)
		{
			while (input.Length > 0)
			{
				var shorter = mergeContext.SpecialTokensByLengthAscending
				                          .FirstOrDefault(x => input.StartsWith(x));
				var input1 = input;
				var longAlternate = mergeContext.SpecialTokensByLengthDescending
				                                .Where(x => checkForLongAlternate && x.Length > input1.Length)
				                                .FirstOrDefault(x => x.StartsWith(input));

				if (shorter == null && longAlternate == null)
				{
					// didn't reach end and no alternate, so not really special
					mergeContext.Data += input.Substring(0, 1);
					input = input.Substring(1);
					continue;
				}

				while (input.Length > 0 && shorter != null)
				{
					if (mergeContext.Data.Length > 0)
					{
						yield return new Token
							             {
								             TokenType = TokenType.Data,
								             Value = mergeContext.Data
							             };
						mergeContext.Data = "";
					}

					// there is only a shorter special, use it
					yield return new Token
						             {
							             TokenType = TokenType.Special,
							             Value = shorter
						             };

					input = input.Substring(shorter.Length);
					shorter = mergeContext.SpecialTokensByLengthAscending
					                      .FirstOrDefault(x => input.StartsWith(x));
				}

				if (input.Length > 0)
				{
					// check for remainer being the start of a longer result
					var current = input;
					var alternate = mergeContext.SpecialTokensByLengthAscending
					                            .Where(x => checkForLongAlternate && x.Length >= current.Length || x.Length == current.Length)
					                            .FirstOrDefault(x => x.StartsWith(input));
					if (alternate != null)
					{
						if (alternate.Length >= input.Length)
						{
							mergeContext.SpecialIndex = input.Length;
							input = alternate;
							break;
						}
						if (longAlternate != null)
						{
							mergeContext.SpecialIndex = input.Length;
							input = longAlternate;
							break;
						}
					}
				}
			}

			mergeContext.Special = input;
		}

		private class MergeContext
		{
			public bool CheckingSpecial { get; set; }
			public string Data { get; set; }
			public string Special { get; set; }
			public int SpecialIndex { get; set; }
			public string[] SpecialTokensByLengthAscending { get; set; }
			public string[] SpecialTokensByLengthDescending { get; set; }
		}
	}
}