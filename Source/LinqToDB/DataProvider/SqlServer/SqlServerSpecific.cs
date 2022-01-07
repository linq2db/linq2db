using System;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SqlServer
{
	using Linq;
	using Expressions;
	using SqlProvider;

	public interface ISqlServerSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
//		/// <summary>
//		/// Adds a table hint to a table in generated query.
//		/// </summary>
//		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
//		/// <returns>Table-like query source with table hints.</returns>
//		[LinqTunnel, Pure] ISqlServerSpecificTable<TSource> With(string hint);
	}

	class SqlServerSpecificTable<TSource> : DatabaseSpecificTable<TSource>, ISqlServerSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public SqlServerSpecificTable(ITable<TSource> table) : base(table)
		{
		}

//		[Sql.QueryExtension(ProviderName.SqlServer, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
//		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.Ignore,    typeof(HintExtensionBuilder))]
//		public ISqlServerSpecificTable<TSource> With([SqlQueryDependent] string hint)
//		{
//			if (Expression.Type is not SqlServerSpecificTable<TSource>)
//				Expression = Expression.Convert(Expression, typeof(SqlServerSpecificTable<TSource>));
//
//			Expression = Expression.Call(
//				Expression,
//				MethodHelper.GetMethodInfo(With, hint),
//				Expression.Constant(hint));
//
//			return this;
//		}
	}

	public interface ISqlServerSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}

	class SqlServerSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, ISqlServerSpecificQueryable<TSource>, ITable
	{
		public SqlServerSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}

	public static partial class SqlServerTools
	{
		[LinqTunnel, Pure]
		[LinqToDB.Sql.QueryExtension(null, LinqToDB.Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificTable<TSource> AsSqlServerSpecific<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsSqlServerSpecific, table),
				table.Expression);

			return new SqlServerSpecificTable<TSource>(table);
		}

		[LinqTunnel, Pure]
		[LinqToDB.Sql.QueryExtension(null, LinqToDB.Sql.QueryExtensionScope.None, typeof(NoneExtensionBuilder))]
		public static ISqlServerSpecificQueryable<TSource> AsSqlServerSpecific<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return new SqlServerSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsSqlServerSpecific, source),
					currentSource.Expression)));
		}
	}
}
