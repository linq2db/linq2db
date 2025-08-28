namespace System.Collections.Generic;

internal static class ReadOnlySetExtensions
{
#pragma warning disable CA1859 // change return type
	public static IReadOnlySet<T> AsReadOnly<T>(this HashSet<T> set)
#pragma warning restore CA1859 // change return type
	{
#if !NETFRAMEWORK
		return set;
#else
		return new ReadOnlyHashSet<T>(set);
#endif
	}
}
