using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace LinqToDB.Internal.Common
{
	public static class Utils
	{
		public static void MakeUniqueNames<T>(
			IEnumerable<T>                   items,
			IEnumerable<string>?             staticNames,
			Func<T, string?>                 nameFunc,
			Action<T, string, ISet<string>?> nameSetter,
			string                           defaultName = "t",
			StringComparer?                  comparer    = null)
		{
			if (items      == null) throw new ArgumentNullException(nameof(items));
			if (nameFunc   == null) throw new ArgumentNullException(nameof(nameFunc));
			if (nameSetter == null) throw new ArgumentNullException(nameof(nameSetter));

			MakeUniqueNames(items, staticNames, nameFunc, nameSetter, t =>
			{
				var name = nameFunc(t);
				return string.IsNullOrEmpty(name) ? defaultName : name!;
			}, comparer);
		}

		public static void MakeUniqueNames<T>(
			IEnumerable<T>                   items,
			IEnumerable<string>?             staticNames,
			Func<T, string?>                 nameFunc,
			Action<T, string, ISet<string>?> nameSetter,
			Func<T, string?>                 defaultName,
			StringComparer?                  comparer = null)
		{
			if (staticNames != null)
			{
				var staticHash = new HashSet<string>(staticNames, comparer);
				MakeUniqueNames(items, null, (n, a) => !staticHash.Contains(n), nameFunc, nameSetter, defaultName, comparer);
			}
			else
			{
				MakeUniqueNames(items, null, (n, a) => true, nameFunc, nameSetter, defaultName, comparer);
			}
		}

		public static void MakeUniqueNames<T>(
			IEnumerable<T>                    items,
			ISet<string>?                     namesParameter,
			Func<string, ISet<string>?, bool> validatorFunc,
			Func<T, string?>                  nameFunc,
			Action<T, string, ISet<string>?>  nameSetter,
			Func<T, string?>                  defaultName,
			StringComparer?                   comparer = null)
		{
			if (items         == null) throw new ArgumentNullException(nameof(items));
			if (validatorFunc == null) throw new ArgumentNullException(nameof(validatorFunc));
			if (nameFunc      == null) throw new ArgumentNullException(nameof(nameFunc));
			if (nameSetter    == null) throw new ArgumentNullException(nameof(nameSetter));
			if (defaultName   == null) throw new ArgumentNullException(nameof(defaultName));

			HashSet<string>?         currentNames    = null;
			Dictionary<string, int>? currentCounters = null;

			foreach (var item in items)
			{
				var name = nameFunc(item);
				if (!string.IsNullOrEmpty(name) && currentNames?.Contains(name!) != true && validatorFunc(name!, namesParameter))
				{
					currentNames ??= new HashSet<string>(comparer);
					currentNames.Add(name!);
					nameSetter(item, name!, namesParameter);
					continue;
				}

				currentNames ??= new HashSet<string>(comparer);
				currentCounters ??= new Dictionary<string, int>(comparer);

				name = defaultName(item);

				if (string.IsNullOrEmpty(name)) name = nameFunc(item);
				if (string.IsNullOrEmpty(name)) name = "t";

				var digitCount = 0;

				while (char.IsDigit(name![name.Length - 1 - digitCount]))
				{
					++digitCount;
				}

				var startDigit = 0;
				if (digitCount > 0)
				{
					digitCount = Math.Min(6, digitCount);

					var prevName = name;

					name = name.Remove(name.Length - digitCount);

					if (!currentCounters.TryGetValue(name, out startDigit))
					{
						startDigit = int.Parse(prevName.Substring(prevName.Length - digitCount, digitCount), NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
					}
				}

				string newName;

				do
				{
					newName = FormattableString.Invariant($"{name}{startDigit}");
					++startDigit;
				} while (currentNames.Contains(newName) || !validatorFunc(newName, namesParameter));

				nameSetter(item, newName, namesParameter);
				currentNames.Add(newName);

				currentCounters.Remove(name);
				currentCounters.Add(name, startDigit);
			}
		}

		public static void RemoveDuplicates<T>(this IList<T> list, IEqualityComparer<T>? comparer = null)
		{
			if (list.Count <= 1)
				return;

			var hashSet = new HashSet<T>(comparer ?? EqualityComparer<T>.Default);
			var i = 0;
			while (i < list.Count)
			{
				if (hashSet.Add(list[i]))
					++i;
				else
				{
					list.RemoveAt(i);
				}
			}
		}

		public static void RemoveDuplicatesFromTail<T>(this IList<T> list, Func<T, T, bool> compareFunc)
		{
			if (list.Count <= 1)
				return;

			for (var i = list.Count - 1; i >= 0; i--)
			{
				var current = list[i];
				for (int j = 0; j < i; j++)
				{
					if (compareFunc(current, list[j]))
					{
						list.RemoveAt(j);
						--j;
						--i;
					}
				}
			}
		}

		public class ObjectReferenceEqualityComparer<T> : IEqualityComparer<T>
		{
			public static IEqualityComparer<T> Default = new ObjectReferenceEqualityComparer<T>();

			#region IEqualityComparer<T> Members

			public bool Equals(T? x, T? y)
			{
				return ReferenceEquals(x, y);
			}

			public int GetHashCode(T obj)
			{
				if (obj == null)
					return 0;

				return RuntimeHelpers.GetHashCode(obj);
			}

			#endregion
		}

		public static void RemoveDuplicates<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null)
		{
			if (list.Count <= 1)
				return;

			var hashSet = new HashSet<TKey>(comparer ?? EqualityComparer<TKey>.Default);
			var i = 0;
			while (i < list.Count)
			{
				if (hashSet.Add(keySelector(list[i])))
					++i;
				else
				{
					list.RemoveAt(i);
				}
			}
		}
	}
}
