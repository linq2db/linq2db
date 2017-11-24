using System;

namespace LinqToDB.Common
{
	/// <summary>
	/// Option type implementation.
	/// <a href="https://en.wikipedia.org/wiki/Option_type">Option type</a>.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	class Option<T>
	{
		/// <summary>
		/// Gets value, stored in option.
		/// </summary>
		public readonly T Value;

		Option(T value)
		{
			Value = value;
		}

		/// <summary>
		/// Returns <c>true</c> of current option stores <see cref="None"/> value.
		/// </summary>
		public bool IsNone
		{
			get { return this == None; }
		}

		/// <summary>
		/// Returns <c>true</c> of current option stores some value instead of <see cref="None"/>.
		/// </summary>
		public bool IsSome
		{
			get { return this != None; }
		}

		/// <summary>
		/// Creates option with value.
		/// </summary>
		/// <param name="value">Option's value.</param>
		/// <returns>Option instance.</returns>
		public static Option<T> Some(T value)
		{
			return new Option<T>(value);
		}

		/// <summary>
		/// Gets <see cref="None"/> value for option.
		/// </summary>
		public static readonly Option<T> None = new Option<T>(default(T));
	}
}
