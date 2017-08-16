using System;

using JetBrains.Annotations;

namespace LinqToDB.Common
{
	/// <summary>
	/// Empty array instance helper.
	/// </summary>
	/// <typeparam name="T">Aray element type.</typeparam>
	[PublicAPI]
	public static class Array<T>
	{
		/// <summary>
		/// Static instance of empty array of specific type.
		/// </summary>
		[NotNull]
		public static readonly T[] Empty = new T[0];
	}
}
