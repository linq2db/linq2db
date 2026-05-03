using System;

using JetBrains.Annotations;

namespace LinqToDB.Tools.Mapper
{
	/// <summary>
	/// Mapper helper class.
	/// </summary>
	/// <example>
	/// This <see href="https://github.com/rsdn/CodeJam/blob/master/CodeJam.Blocks.Tests/Mapping/Examples/MapTests.cs">example</see> shows how to map one object to another.
	/// </example>
	[PublicAPI]
	public static class Map
	{
		/// <summary>
		/// Returns a mapper to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <typeparam name="TFrom">Type to map from.</typeparam>
		/// <typeparam name="TTo">Type to map to.</typeparam>
		/// <returns>Mapping expression.</returns>
		[Pure]
		public static Mapper<TFrom,TTo> GetMapper<TFrom,TTo>()
			=> new Mapper<TFrom,TTo>(new MapperBuilder<TFrom,TTo>());

		/// <summary>
		/// Returns a mapper to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <typeparam name="TFrom">Type to map from.</typeparam>
		/// <typeparam name="TTo">Type to map to.</typeparam>
		/// <param name="setter">MapperBuilder parameter setter.</param>
		/// <returns>Mapping expression.</returns>
		[Pure]
		public static Mapper<TFrom,TTo> GetMapper<TFrom,TTo>(
			Func<MapperBuilder<TFrom,TTo>,MapperBuilder<TFrom,TTo>> setter)
		{
			ArgumentNullException.ThrowIfNull(setter);
			return new Mapper<TFrom,TTo>(setter(new MapperBuilder<TFrom,TTo>()));
		}

		static class MapHolder<T>
		{
			public static readonly Mapper<T,T> Mapper =
				GetMapper<T,T>(m => m
					.SetProcessCrossReferences(true)
					.SetDeepCopy(true));
		}

		/// <summary>
		/// Performs deep copy.
		/// </summary>
		/// <param name="obj">An object to copy.</param>
		/// <typeparam name="T">Type of object.</typeparam>
		/// <returns>Created object.</returns>
		[Pure]
		public static T DeepCopy<T>(this T obj) => MapHolder<T>.Mapper.Map(obj);
	}
}
