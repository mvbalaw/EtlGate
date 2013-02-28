using System;

namespace EtlGate.Core
{
	public class ParseException : Exception
	{
		public ParseException(string message) : base(message)
		{
		}
	}
}