using System;

namespace LinqToDB.Extensions
{
	using Common;

	static class CollectionExtensions
	{
		public static T[] Flatten<T>(this T[][] items)
		{
			var size = 0;
			foreach (var col in items)
				size += items.Length;

			if (size == 0)
				return Array<T>.Empty;

			var res = new T[size];

			size = 0;
			foreach (var col in items)
			{
				Array.Copy(col, 0, res, size, col.Length);
				size += col.Length;
			}

			return res;
		}
	}
}
