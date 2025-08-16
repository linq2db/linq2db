namespace System.Collections.Generic;

internal static class ReadOnlySetExtensions
{
#pragma warning disable CA1859 // change return type
	public static IReadOnlySet<T> AsReadOnly<T>(this HashSet<T> set)
#pragma warning restore CA1859 // change return type
	{
#if NET5_0_OR_GREATER
		return set;
#else
		return new ReadOnlyHashSet<T>(set);
#endif
	}
}
