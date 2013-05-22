namespace EtlGate.Core
{
	public abstract class Token
	{
		private readonly string _value;

		protected Token(string value)
		{
			_value = value;
		}

		public string Value
		{
			get { return _value; }
		}
	}

	public class DataToken : Token
	{
		public DataToken(string value)
			: base(value)
		{
		}
	}

	public class SpecialToken : Token
	{
		public SpecialToken(string value)
			: base(value)
		{
		}
	}
}