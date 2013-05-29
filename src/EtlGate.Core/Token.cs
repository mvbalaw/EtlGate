using System;

using JetBrains.Annotations;

namespace EtlGate.Core
{
	public abstract class Token
	{
		private readonly int _length;
		private readonly char[] _source;
		private readonly int _start;
		private string _value;

		protected Token([NotNull] char[] source, int start, int length)
		{
			_source = source;
			_start = start;
			_length = length;
		}

		protected Token([NotNull] Token token)
		{
			_source = token._source;
			_start = token._start;
			_length = token._length;
			_value = token._value;
		}

		protected Token([NotNull] string source)
		{
			_source = new[] { source[0] };
			_start = 0;
			_length = source.Length;
			_value = source;
		}

		public char First
		{
			[Pure] get { return _source[_start]; }
		}
		public int Length
		{
			[Pure] get { return _length; }
		}
		[NotNull]
		public string Value
		{
			[Pure] get { return _value ?? (_value = new String(_source, _start, _length)); }
		}
	}

	public class DataToken : Token
	{
		public DataToken([NotNull] char[] source, int start, int length)
			: base(source, start, length)
		{
		}

		public DataToken([NotNull] Token token)
			: base(token)
		{
		}

		public DataToken([NotNull] string source)
			: base(source)
		{
		}
	}

	public class SpecialToken : Token
	{
		public SpecialToken([NotNull] char[] source, int start, int length)
			: base(source, start, length)
		{
		}

		public SpecialToken([NotNull] string source)
			: base(source)
		{
		}
	}

	public class EndOfStreamToken : Token
	{
		public EndOfStreamToken()
			: base(new char[] { }, 0, 0)
		{
		}
	}
}