using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Oracle
{
	using Linq;
	using SqlProvider;

	public interface IOracleSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}

	class OracleSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IOracleSpecificTable<TSource>, ITable
		where TSource : notnull
	{
		public OracleSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}

	public static partial class OracleTools
	{
		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.Ignore, typeof(HintExtensionBuilder))]
		public static IOracleSpecificTable<TSource> AsOracleSpecific<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsOracleSpecific, table),
				table.Expression);

			return new OracleSpecificTable<TSource>(table);
		}
	}
}
