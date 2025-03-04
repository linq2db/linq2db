using System;

namespace LinqToDB.Internal.Extensions
{
	static class CollectionExtensions
	{
		public static T[] Flatten<T>(this T[][] items)
		{
			var size = 0;
			foreach (var col in items)
				size += col.Length;

			if (size == 0)
				return [];

			var res = new T[size];

			size = 0;
			foreach (var col in items)
			{
				if (col.Length > 0)
				{
					Array.Copy(col, 0, res, size, col.Length);
					size += col.Length;
				}
			}

			return res;
		}
	}
}
