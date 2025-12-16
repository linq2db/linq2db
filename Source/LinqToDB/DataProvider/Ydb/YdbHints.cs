using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using JetBrains.Annotations;

using LinqToDB.Internal.DataProvider.Ydb;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// SQL hints for YDB (YQL). See:
	///  - SQL hints syntax: comments starting with "--+".
	///  - SELECT-level hints: 'unique' / 'distinct' right after SELECT.
	/// </summary>
	/// <remarks>
	/// Emits YQL hint comment lines like:
	///   --+ unique(col1 col2)
	///   --+ distinct()
	/// </remarks>
	public static partial class YdbHints
	{
		public const string Unique   = "unique";
		public const string Distinct = "distinct";

		[LinqTunnel, Pure, IsQueryable]
        		[Sql.QueryExtension(ProviderName.Ydb, Sql.QueryExtensionScope.SubQueryHint, typeof(YdbQueryHintExtensionBuilder))]
        		[Sql.QueryExtension(null,             Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
        		public static IYdbSpecificQueryable<TSource> QueryHint<TSource>(
        			this IQueryable<TSource> source,
        			[SqlQueryDependent] string hint,
        			[SqlQueryDependent] params string[] values)
        			where TSource : notnull
        		{
        			var current = source.ProcessIQueryable();
        
        			return new YdbSpecificQueryable<TSource>(current.Provider.CreateQuery<TSource>(
        				Expression.Call(
        					null,
        					MethodHelper.GetMethodInfo(QueryHint, source, hint, values),
        					current.Expression,
        					Expression.Constant(hint),
        					Expression.NewArrayInit(typeof(string), values.Select(Expression.Constant)))));
        		}
				
		/// <summary>
		/// Generic query-hint injector for YDB/YQL.
		/// </summary>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Ydb, Sql.QueryExtensionScope.SubQueryHint, typeof(YdbQueryHintExtensionBuilder))]
		[Sql.QueryExtension(null,             Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
		public static IYdbSpecificQueryable<TSource> QueryHint<TSource>(
			this IYdbSpecificQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] params string[] values)
			where TSource : notnull
		{
			var current = source.ProcessIQueryable();

			return new YdbSpecificQueryable<TSource>(current.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryHint, source, hint, values),
					current.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(string), values.Select(Expression.Constant)))));
		}

		/// <summary>
		/// Builds a YQL SQL-hint comment line:
		///   --+ hint(v1 v2 ...)
		/// </summary>
		sealed class YdbQueryHintExtensionBuilder : ISqlQueryExtensionBuilder
		{
			void ISqlQueryExtensionBuilder.Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
			{
				var hint = (string)((SqlValue)sqlQueryExtension.Arguments["hint"]).Value!;

				stringBuilder.Append("--+ ").Append(hint).Append('(');

				var count = (int)((SqlValue)sqlQueryExtension.Arguments["values.Count"]).Value!;
				for (var i = 0; i < count; i++)
				{
					if (i > 0) stringBuilder.Append(' ');

					var raw = (string)((SqlValue)sqlQueryExtension.Arguments[string.Create(CultureInfo.InvariantCulture, $"values.{i}")]).Value!;
					// quote value if it contains whitespace or parentheses or quote
					var needQuote = raw.Any(ch => char.IsWhiteSpace(ch) || ch is '(' or ')' or '\'');
					if (needQuote)
					{
						stringBuilder.Append('\'')
						  .Append(raw.Replace("'", "''"))
						  .Append('\'');
					}
					else
					{
						stringBuilder.Append(raw);
					}
				}

				stringBuilder.Append(')').AppendLine();
			}
		}
	}
}
