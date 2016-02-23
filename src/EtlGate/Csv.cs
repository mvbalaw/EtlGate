namespace EtlGate
{
	public static class Csv
	{
		// http://stackoverflow.com/questions/4685705/good-csv-writer-for-c

		private static readonly char[] CharactersThatMustBeQuoted = {',', '"', '\r', '\n'};

		public static string Escape(string s)
		{
			if (s == null)
			{
				return "";
			}
			var specialIndex = s.IndexOfAny(CharactersThatMustBeQuoted);
			if (specialIndex == -1)
			{
				return s;
			}
			return string.Format("\"{0}{1}\"", s.Substring(0, specialIndex), s.Substring(specialIndex).Replace("\"", "\"\""));
		}
	}
}