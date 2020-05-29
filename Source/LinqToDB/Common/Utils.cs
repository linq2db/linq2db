using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Common
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
				if (!name.IsNullOrEmpty() && currentNames?.Contains(name) != true && validatorFunc(name, namesParameter))
				{
					if (currentNames == null)
						currentNames = new HashSet<string>(comparer);
					currentNames.Add(name);
					nameSetter(item, name, namesParameter);
					continue;
				}

				if (currentNames == null)
					currentNames = new HashSet<string>(comparer);
				if (currentCounters == null)
					currentCounters = new Dictionary<string, int>(comparer);

				name = defaultName(item);

				if (name.IsNullOrEmpty())
					name = nameFunc(item);
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
				} while (currentNames.Contains(newName) || !validatorFunc(newName, namesParameter));

				nameSetter(item, newName, namesParameter);
				currentNames.Add(newName);

				currentCounters.Remove(name);
				currentCounters.Add(name, startDigit);
			}
		}
	}
}
