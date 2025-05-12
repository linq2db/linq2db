using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Linq;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.Ydb
{
	// ---------- TABLE-SPECIFIC ----------

	/// <summary>
	/// Interface representing a YDB-specific LINQ-to-DB table.
	/// </summary>
	/// <typeparam name="TSource">The type of the entity.</typeparam>
	public interface IYdbSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	/// <summary>
	/// Internal implementation of a YDB-specific table wrapper.
	/// </summary>
	/// <typeparam name="TSource">The type of the entity.</typeparam>
	sealed class YdbSpecificTable<TSource> :
		DatabaseSpecificTable<TSource>,
		IYdbSpecificTable<TSource>,
		ITable
		where TSource : notnull
	{
		public YdbSpecificTable(ITable<TSource> table) : base(table) { }
	}

	// ---------- QUERYABLE-SPECIFIC ----------

	/// <summary>
	/// Interface representing a YDB-specific LINQ-to-DB queryable source.
	/// </summary>
	/// <typeparam name="TSource">The type of the entity.</typeparam>
	public interface IYdbSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	/// <summary>
	/// Internal implementation of a YDB-specific queryable wrapper.
	/// </summary>
	/// <typeparam name="TSource">The type of the entity.</typeparam>
	sealed class YdbSpecificQueryable<TSource> :
		DatabaseSpecificQueryable<TSource>,
		IYdbSpecificQueryable<TSource>,
		ITable
	{
		public YdbSpecificQueryable(IQueryable<TSource> queryable) : base(queryable) { }
	}

	// ---------- EXTENSION METHODS ----------

	public static partial class YdbTools
	{
		// --- ITable → IYdbSpecificTable ---

		/// <summary>
		/// Converts a LINQ-to-DB <see cref="ITable{T}"/> into a YDB-specific table
		/// by wrapping it in an expression the YDB provider can recognize.
		/// </summary>
		/// <typeparam name="TSource">The entity type.</typeparam>
		/// <param name="table">The source table.</param>
		/// <returns>A YDB-specific table wrapper.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IYdbSpecificTable<TSource> AsYdb<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			// Wrap the original Table expression in a new AsYdb call,
			// so that the provider can detect the "YDB mode".
			var newTable = new Table<TSource>(
				table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsYdb, table),
					table.Expression));

			return new YdbSpecificTable<TSource>(newTable);
		}

		// --- IQueryable → IYdbSpecificQueryable ---

		/// <summary>
		/// Converts a LINQ <see cref="IQueryable{T}"/> into a YDB-specific queryable
		/// by normalizing and wrapping it for recognition by the YDB provider.
		/// </summary>
		/// <typeparam name="TSource">The entity type.</typeparam>
		/// <param name="source">The source queryable.</param>
		/// <returns>A YDB-specific queryable wrapper.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static IYdbSpecificQueryable<TSource> AsYdb<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			// It is important for the provider to receive a normalized IQueryable,
			// so we use the built-in helper.
			var currentSource = source.ProcessIQueryable();

			return new YdbSpecificQueryable<TSource>(
				currentSource.Provider.CreateQuery<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(AsYdb, source),
						currentSource.Expression)));
		}
	}
}
