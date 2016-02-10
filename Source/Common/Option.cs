using System;

namespace LinqToDB.Common
{
	class Option<T>
	{
		public readonly T Value;

		Option(T value)
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

		public static readonly Option<T> None = new Option<T>(default(T));
	}
}
