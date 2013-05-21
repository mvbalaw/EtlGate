namespace EtlGate.Core
{
	public class Token
	{
		private readonly TokenType _tokenType;
		private readonly string _value;

		public Token(TokenType tokenType, string value)
		{
			_tokenType = tokenType;
			_value = value;
		}

		public TokenType TokenType
		{
			get { return _tokenType; }
		}

		public string Value
		{
			get { return _value; }
		}
	}
}