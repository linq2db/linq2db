using System;

namespace LinqToDB.Common
{
	class Option<T>
	{
		public readonly T Value;

		public Option(T value)
		{
			Value = value;
		}

		public bool IsNone
		{
			get { return this == None; }
		}

		public bool IsSome
		{
			get { return this != None; }
		}

		public static Option<T> Some(T value)
		{
			return new Option<T>(value);
		}

		public static Option<T> None = new Option<T>(default(T));
	}
}
