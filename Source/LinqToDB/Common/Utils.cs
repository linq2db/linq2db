using System;
using System.Collections.Generic;

using JetBrains.Annotations;

namespace LinqToDB.Common
{
	public static class Utils
	{
		public static void MakeUniqueNames<T>([NotNull] IEnumerable<T> items, [NotNull] Func<T, string> nameFunc,
			[NotNull] Action<T, string> nameSetter, string defaultName = "t")
		{
			if (items      == null) throw new ArgumentNullException(nameof(items));
			if (nameFunc   == null) throw new ArgumentNullException(nameof(nameFunc));
			if (nameSetter == null) throw new ArgumentNullException(nameof(nameSetter));

			MakeUniqueNames(items, null, nameFunc, nameSetter, t =>
			{
				var name = nameFunc(t);
				return string.IsNullOrEmpty(name) ? defaultName : name;
			});
		}

		public static void MakeUniqueNames<T>([NotNull] IEnumerable<T> items, IEnumerable<string> staticNames, [NotNull] Func<T, string> nameFunc,
			[NotNull] Action<T, string> nameSetter, string defaultName = "t")
		{
			if (items      == null) throw new ArgumentNullException(nameof(items));
			if (nameFunc   == null) throw new ArgumentNullException(nameof(nameFunc));
			if (nameSetter == null) throw new ArgumentNullException(nameof(nameSetter));

			MakeUniqueNames(items, staticNames, nameFunc, nameSetter, t =>
			{
				var name = nameFunc(t);
				return string.IsNullOrEmpty(name) ? defaultName : name;
			});
		}

		public static void MakeUniqueNames<T>([NotNull] IEnumerable<T> items, IEnumerable<string> staticNames, [NotNull] Func<T, string> nameFunc,
			[NotNull] Action<T, string> nameSetter, [NotNull] Func<T, string> defaultName)
		{
			if (items       == null) throw new ArgumentNullException(nameof(items));
			if (nameFunc    == null) throw new ArgumentNullException(nameof(nameFunc));
			if (nameSetter  == null) throw new ArgumentNullException(nameof(nameSetter));
			if (defaultName == null) throw new ArgumentNullException(nameof(defaultName));

			var currentNames = staticNames != null ? new HashSet<string>(staticNames) : new HashSet<string>();
			List<T> conflicted = null;

			foreach (var item in items)
			{
				var name = nameFunc(item);
				if (name.IsNullOrEmpty() || currentNames.Contains(name))
				{
					if (conflicted == null)
						conflicted = new List<T>();
					conflicted.Add(item);
				}
				else
				{
					currentNames.Add(name);
				}
			}

			if (conflicted != null)
			{
				foreach (var item in conflicted)
				{
					var name = nameFunc(item);
					if (name.IsNullOrEmpty())
						name = defaultName(item);
					if (name.IsNullOrEmpty())
						name = "t";

					var newName = name;

					if (currentNames.Contains(newName))
					{
						var digitCount = 0;
						while (char.IsDigit(name[name.Length - 1 - digitCount]))
						{
							++digitCount;
						}

						var startDigit = 0;
						if (digitCount > 0)
						{
							digitCount = Math.Min(6, digitCount);
							startDigit = int.Parse(name.Substring(name.Length - digitCount, digitCount));
							name = name.Remove(digitCount);
						}

						do
						{
							++startDigit;
							newName = name + startDigit;
						} while (currentNames.Contains(newName));
					}

					nameSetter(item, newName);
					currentNames.Add(newName);
				}
			}
		}
	}
}
