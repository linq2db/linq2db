using System;

namespace LinqToDB.Common
{
	struct Option
	{
		public readonly object Value;

		public Option(object value)
		{
			Value = value;
		}

		public bool IsNone
		{
			get { return Value == _none; }
		}

		public bool IsSome
		{
			get { return Value != _none; }
		}

		static public Option Some(object value)
		{
			return new Option(value);
		}

		private static object _none = new object();
		public  static Option  None = new Option(_none);
	}
}
