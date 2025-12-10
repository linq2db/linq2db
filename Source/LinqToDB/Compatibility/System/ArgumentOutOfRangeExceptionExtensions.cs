#if !NET8_0_OR_GREATER

#pragma warning disable IDE0130
#pragma warning disable IDE0160

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

internal static class ArgumentOutOfRangeExceptionExtensions
{
	extension(ArgumentOutOfRangeException)
	{
		[StackTraceHidden]
		public static void ThrowIfNegativeOrZero(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
		{
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0, paramName);
		}

		[StackTraceHidden]
		public static void ThrowIfNegativeOrZero(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
		{
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0, paramName);
		}

		[StackTraceHidden]
		public static void ThrowIfNegative(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, paramName);
		}

		[StackTraceHidden]
		public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, paramName);
		}
	}
}

#endif
