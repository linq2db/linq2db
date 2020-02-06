using System;
using JetBrains.Annotations;

namespace LinqToDB.Common
{
	/// <summary>
	/// Empty array instance helper.
	/// </summary>
	/// <typeparam name="T">Array element type.</typeparam>
	[PublicAPI]
	public static class Array<T>
	{
		/// <summary>
		/// Static instance of empty array of specific type.
		/// </summary>
		public static readonly T[] Empty = new T[0];

		internal static T[] Append(T[] array, T newElement)
		{
			var oldSize = array.Length;
			
			Array.Resize(ref array, oldSize + 1);

			array[oldSize] = newElement;

			return array;
		}
	}
}
