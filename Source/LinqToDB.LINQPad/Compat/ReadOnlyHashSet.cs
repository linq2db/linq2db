#if NETFRAMEWORK
namespace System.Collections.Generic;

internal sealed class ReadOnlyHashSet<T>(ISet<T> set) : IReadOnlySet<T>
{
	int IReadOnlyCollection<T>.Count => set.Count;

	bool IReadOnlySet<T>.Contains(T item) => set.Contains(item);

	IEnumerator<T> IEnumerable<T>.GetEnumerator() => set.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)set).GetEnumerator();

	bool IReadOnlySet<T>.IsProperSubsetOf(IEnumerable<T> other) => set.IsProperSubsetOf(other);

	bool IReadOnlySet<T>.IsProperSupersetOf(IEnumerable<T> other) => set.IsProperSupersetOf(other);

	bool IReadOnlySet<T>.IsSubsetOf(IEnumerable<T> other) => set.IsSubsetOf(other);

	bool IReadOnlySet<T>.IsSupersetOf(IEnumerable<T> other) => set.IsSupersetOf(other);

	bool IReadOnlySet<T>.Overlaps(IEnumerable<T> other) => set.Overlaps(other);

	bool IReadOnlySet<T>.SetEquals(IEnumerable<T> other) => set.SetEquals(other);

	
}
#endif
