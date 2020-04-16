using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Common
{
	public static class Utils
	{
		public static void MakeUniqueNames<T>(
			IEnumerable<T>       items,
			IEnumerable<string>? staticNames,
			Func<T, string?>     nameFunc,
			Action<T, string>    nameSetter,
			string               defaultName = "t",
			StringComparer?      comparer    = null)
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
			IEnumerable<T>       items,
			IEnumerable<string>? staticNames,
			Func<T, string?>     nameFunc,
			Action<T, string>    nameSetter,
			Func<T, string?>     defaultName,
			StringComparer?      comparer = null)
		{
			if (staticNames != null)
			{
				var staticHash = new HashSet<string>(staticNames, comparer);
				MakeUniqueNames(items, n => !staticHash.Contains(n), nameFunc, nameSetter, defaultName, comparer);
			}
			else
			{
				MakeUniqueNames(items, n => true, nameFunc, nameSetter, defaultName, comparer);
			}
		}

		public static void MakeUniqueNames<T>(
			IEnumerable<T>     items,
			Func<string, bool> validatorFunc,
			Func<T, string?>   nameFunc,
			Action<T, string>  nameSetter,
			Func<T, string?>   defaultName,
			StringComparer?    comparer = null)
		{
			if (items         == null) throw new ArgumentNullException(nameof(items));
			if (validatorFunc == null) throw new ArgumentNullException(nameof(validatorFunc));
			if (nameFunc      == null) throw new ArgumentNullException(nameof(nameFunc));
			if (nameSetter    == null) throw new ArgumentNullException(nameof(nameSetter));
			if (defaultName   == null) throw new ArgumentNullException(nameof(defaultName));

			var duplicates = items.ToLookup(i => nameFunc(i) ?? string.Empty, comparer);

			if (duplicates.Count == 0)
				return;

			var currentNames    = new HashSet<string>(comparer);
			var currentCounters = new Dictionary<string, int>(comparer);

			foreach (var pair in duplicates)
			{
				var groupItems = pair.ToArray();

				if (pair.Key != string.Empty && groupItems.Length == 1 && !currentNames.Contains(pair.Key) && validatorFunc(pair.Key))
				{
					currentNames.Add(pair.Key);
					nameSetter(groupItems[0], pair.Key);
					continue;
				}

				foreach (var groupItem in groupItems)
				{
					string? name = defaultName(groupItem);

					if (name.IsNullOrEmpty())
						name = nameFunc(groupItem);
					if (name.IsNullOrEmpty())
						name = "t";

					var digitCount = 0;
					while (char.IsDigit(name[name.Length - 1 - digitCount]))
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
							startDigit = int.Parse(prevName.Substring(prevName.Length - digitCount, digitCount));
						}
					}

					string newName;
					do
					{
						newName = name + startDigit;
						++startDigit;
					} while (duplicates.Contains(newName) || currentNames.Contains(newName) || !validatorFunc(newName));

					nameSetter(groupItem, newName);
					currentNames.Add(newName);

					currentCounters.Remove(name);
					currentCounters.Add(name, startDigit);
				}
			}
		}
	}
}
