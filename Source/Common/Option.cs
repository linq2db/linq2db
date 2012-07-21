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

		static readonly object _none = new object();
		static public   Option  None = new Option(_none);
	}
}
