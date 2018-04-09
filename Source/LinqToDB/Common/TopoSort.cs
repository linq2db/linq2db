using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace LinqToDB.Common
{
	/// <summary>
	/// This alogrithm is partially borrowed from CodeJam library.
	/// Modified to remove dependencies to the library.
	/// </summary>
	static class TopoSorting
	{
		#region TopoSort
		/// <summary>
		/// Performs topological sort on <paramref name="source"/>.
		/// </summary>
		/// <param name="source">Collection to sort.</param>
		/// <param name="dependsOnGetter">Function that returns items dependent on specified item.</param>
		/// <returns>Topologically sorted list of items in <paramref name="source"/>.</returns>
		[NotNull]
		[Pure]
		public static IEnumerable<T> TopoSort<T>(
				[NotNull, InstantHandle] IEnumerable<T> source,
				[NotNull, InstantHandle] Func<T, IEnumerable<T>> dependsOnGetter) =>
			TopoSort(source, dependsOnGetter, EqualityComparer<T>.Default);

		/// <summary>
		/// Performs topological sort on <paramref name="source"/>.
		/// </summary>
		/// <param name="source">Collection to sort.</param>
		/// <param name="dependsOnGetter">Function that returns items dependent on specified item.</param>
		/// <returns>Topologically sorted list of items in <paramref name="source"/>.</returns>
		[NotNull]
		[Pure]
		public static IEnumerable<T> TopoSort<T>(
				[NotNull, InstantHandle] ICollection<T> source,
				[NotNull, InstantHandle] Func<T, IEnumerable<T>> dependsOnGetter) =>
			TopoSort(source, dependsOnGetter, EqualityComparer<T>.Default);

		/// <summary>
		/// Performs topological sort on <paramref name="source"/>.
		/// </summary>
		/// <param name="source">Collection to sort.</param>
		/// <param name="dependsOnGetter">Function that returns items dependent on specified item.</param>
		/// <param name="equalityComparer">Equality comparer for item comparison</param>
		/// <returns>Topologically sorted list of items in <paramref name="source"/>.</returns>
		[NotNull]
		[Pure]
		public static IEnumerable<T> TopoSort<T>(
				[NotNull] this IEnumerable<T> source,
				[NotNull, InstantHandle] Func<T, IEnumerable<T>> dependsOnGetter,
				[NotNull] IEqualityComparer<T> equalityComparer) =>
			GroupTopoSort(source, dependsOnGetter, equalityComparer)
				.Select(g => g.AsEnumerable())
				.SelectMany(e => e);

		/// <summary>
		/// Performs topological sort on <paramref name="source"/>.
		/// </summary>
		/// <param name="source">Collection to sort.</param>
		/// <param name="dependsOnGetter">Function that returns items dependent on specified item.</param>
		/// <param name="equalityComparer">Equality comparer for item comparison</param>
		/// <returns>Topologically sorted list of items in <paramref name="source"/>.</returns>
		[NotNull]
		[Pure]
		public static IEnumerable<T> TopoSort<T>(
				[NotNull] this ICollection<T> source,
				[NotNull, InstantHandle] Func<T, IEnumerable<T>> dependsOnGetter,
				[NotNull] IEqualityComparer<T> equalityComparer) =>
			GroupTopoSort(source, dependsOnGetter, equalityComparer)
				.Select(g => g.AsEnumerable())
				.SelectMany(e => e);
		#endregion

		#region GroupTopoSort
		/// <summary>
		/// Performs topological sort on <paramref name="source"/>.
		/// </summary>
		/// <param name="source">Collection to sort.</param>
		/// <param name="dependsOnGetter">Function that returns items dependent on specified item.</param>
		/// <returns>Topologically sorted list of items in <paramref name="source"/> separated by dependency level.</returns>
		[NotNull]
		[Pure]
		public static IEnumerable<T[]> GroupTopoSort<T>(
				[NotNull, InstantHandle] this IEnumerable<T> source,
				[NotNull, InstantHandle] Func<T, IEnumerable<T>> dependsOnGetter) =>
			GroupTopoSort(source, dependsOnGetter, EqualityComparer<T>.Default);

		/// <summary>
		/// Performs topological sort on <paramref name="source"/>.
		/// </summary>
		/// <param name="source">Collection to sort.</param>
		/// <param name="dependsOnGetter">Function that returns items dependent on specified item.</param>
		/// <returns>Topologically sorted list of items in <paramref name="source"/> separated by dependency level.</returns>
		[NotNull]
		[Pure]
		public static IEnumerable<T[]> GroupTopoSort<T>(
				[NotNull, InstantHandle] this ICollection<T> source,
				[NotNull, InstantHandle] Func<T, IEnumerable<T>> dependsOnGetter) =>
			GroupTopoSort(source, dependsOnGetter, EqualityComparer<T>.Default);

		/// <summary>
		/// Performs topological sort on <paramref name="source"/>.
		/// </summary>
		/// <param name="source">Collection to sort.</param>
		/// <param name="dependsOnGetter">Function that returns items dependent on specified item.</param>
		/// <param name="equalityComparer">Equality comparer for item comparison</param>
		/// <returns>Topologically sorted list of items in <paramref name="source"/> separated by dependency level.</returns>
		[NotNull]
		[Pure]
		public static IEnumerable<T[]> GroupTopoSort<T>(
				[NotNull, InstantHandle] this IEnumerable<T> source,
				[NotNull, InstantHandle] Func<T, IEnumerable<T>> dependsOnGetter,
				[NotNull] IEqualityComparer<T> equalityComparer) =>
			GroupTopoSort(source.ToArray(), dependsOnGetter, equalityComparer);

		/// <summary>
		/// Performs topological sort on <paramref name="source"/>.
		/// </summary>
		/// <param name="source">Collection to sort.</param>
		/// <param name="dependsOnGetter">Function that returns items dependent on specified item.</param>
		/// <param name="equalityComparer">Equality comparer for item comparison</param>
		/// <returns>
		/// Topologically sorted list of items in <paramref name="source"/>, separated by dependency level.
		/// </returns>
		[NotNull]
		[Pure]
		public static IEnumerable<T[]> GroupTopoSort<T>(
			[NotNull, InstantHandle] this ICollection<T> source,
			[NotNull, InstantHandle] Func<T, IEnumerable<T>> dependsOnGetter,
			[NotNull] IEqualityComparer<T> equalityComparer)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (dependsOnGetter == null) throw new ArgumentNullException(nameof(dependsOnGetter));
			if (equalityComparer == null) throw new ArgumentNullException(nameof(equalityComparer));

			// Fast path
			if (source.Count == 0)
				yield break;

			var dependants = new Dictionary<T, List<T>>(equalityComparer);
			var workArray = new int[source.Count];
			var indices = new Dictionary<T, int>(equalityComparer);
			var level = new List<T>();
			foreach (var item in source.Select((item, index) => new {Item = item, Index = index}))
			{
				var count = 0;
				foreach (var dep in dependsOnGetter(item.Item))
				{
					if (!dependants.TryGetValue(dep, out var list))
					{
						list = new List<T>();
						dependants.Add(dep, list);
					}
					list.Add(item.Item);
					count++;
				}
				if (count == 0)
					level.Add(item.Item);
				else
					workArray[item.Index] = count;
				indices.Add(item.Item, item.Index);
			}

			if (level.Count == 0)
				throw CycleException(nameof(source));

			// Fast path
			if (level.Count == workArray.Length)
			{
				yield return level.ToArray();
				yield break;
			}

			var pendingCount = workArray.Length;
			while (true)
			{
				var nextLevel = new Lazy<List<T>>(() => new List<T>(), false);
				foreach (var item in level)
				{
					if (dependants.TryGetValue(item, out var list))
						foreach (var dep in list)
						{
							var pending = --workArray[indices[dep]];
							if (pending == 0)
								nextLevel.Value.Add(dep);
						}
				}
				yield return level.ToArray();
				pendingCount -= level.Count;
				if (pendingCount == 0)
					yield break;
				if (!nextLevel.IsValueCreated)
					throw CycleException(nameof(source));
				level = nextLevel.Value;
			}
		}

		private static ArgumentException CycleException(string argName) =>
			new ArgumentException(argName, "Cycle detected.");
		#endregion
	}}
