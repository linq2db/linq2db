#if NETSTANDARD2_0

#pragma warning disable IDE0130

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LinqToDB
{
	internal static class ArgumentNullException
	{
		public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
		{
			if (argument is null)
			{
				Throw(paramName);
			}
		}

		[DoesNotReturn]
		private static void Throw(string? paramName) => throw new System.ArgumentNullException(paramName);
	}
}

#endif
