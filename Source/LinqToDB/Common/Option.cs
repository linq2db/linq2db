﻿using System;
using System.Runtime.CompilerServices;

namespace LinqToDB.Common
{
	/// <summary>
	/// Option type implementation.
	/// <a href="https://en.wikipedia.org/wiki/Option_type">Option type</a>.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	public readonly struct Option<T>
	{
		private readonly bool _hasValue;
		private readonly T    _value;

		private Option(T value)
		{
			_hasValue = true;
			_value    = value;
		}

		/// <summary>
		/// Returns <c>true</c> if current option stores some value instead of <see cref="None"/>.
		/// </summary>
		public bool HasValue => _hasValue;

		/// <summary>
		/// Gets value, stored in option.
		/// </summary>
		public T Value
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _hasValue ? _value : throw new InvalidOperationException($"{nameof(Option<int>)}.{nameof(Value)} not set");
		}

		/// <summary>
		/// Creates option with value.
		/// </summary>
		/// <param name="value">Option's value.</param>
		/// <returns>Option instance.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Option<T> Some(T value)
		{
			return new Option<T>(value);
		}

		/// <summary>
		/// Gets <see cref="None"/> value for option.
		/// </summary>
		public static readonly Option<T> None;

		public static implicit operator Option<T>(T value) => new (value);
	}
}
