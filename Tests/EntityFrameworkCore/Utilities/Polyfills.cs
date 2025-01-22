#if NETFRAMEWORK

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Microsoft.EntityFrameworkCore
{
	internal static class RelationalIndexBuilderExtensionsPolyfill
	{
		public static IndexBuilder HasDatabaseName(this IndexBuilder indexBuilder, string? name) => indexBuilder.HasName(name);
	}
}

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

namespace System.Linq
{
	internal static class EnumerablePolyfill
	{
		public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source) => new HashSet<TSource>(source);
	}
}

#endif
