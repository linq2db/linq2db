using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using JetBrains.Annotations;

using LinqToDB.Internal.DataProvider.PostgreSQL;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Metadata;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.PostgreSQL
{
	// https://www.postgresql.org/docs/current/sql-select.html
	//
	public static partial class PostgreSQLHints
	{
		public const string ForUpdate      = "FOR UPDATE";
		public const string ForNoKeyUpdate = "FOR NO KEY UPDATE";
		public const string ForShare       = "FOR SHARE";
		public const string ForKeyShare    = "FOR KEY SHARE";

		public const string NoWait         = "NOWAIT";
		public const string SkipLocked     = "SKIP LOCKED";

		sealed class SubQueryTableHintExtensionBuilder : ISqlQueryExtensionBuilder
		{
			void ISqlQueryExtensionBuilder.Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
			{
				var hint = (string)((SqlValue)sqlQueryExtension.Arguments["hint"]).Value!;

				if (hint is ForNoKeyUpdate or ForKeyShare && sqlBuilder.MappingSchema.ConfigurationList.Contains(ProviderName.PostgreSQL92, StringComparer.Ordinal))
					stringBuilder.Append("-- ");

				stringBuilder.Append(hint);

				var idCount = (int)((SqlValue)sqlQueryExtension.Arguments["tableIDs.Count"]).Value!;

				for (var i = 0; i < idCount; i++)
				{
					if (i == 0)
						stringBuilder.Append(" OF ");
					else if (i > 0)
						stringBuilder.Append(", ");

					var id    = (Sql.SqlID)((SqlValue)sqlQueryExtension.Arguments[string.Create(CultureInfo.InvariantCulture, $"tableIDs.{i}")]).Value!;
					var alias = sqlBuilder.BuildSqlID(id);

					stringBuilder.Append(alias);
				}

				if (sqlQueryExtension.Arguments.TryGetValue("hint2", out var h) && h is SqlValue { Value: string value })
				{
					if (!string.Equals(value, SkipLocked, StringComparison.Ordinal)
						|| sqlBuilder.MappingSchema.ConfigurationList.Contains(ProviderName.PostgreSQL95, StringComparer.Ordinal)
						|| sqlBuilder.MappingSchema.ConfigurationList.Contains(ProviderName.PostgreSQL13, StringComparer.Ordinal)
						|| sqlBuilder.MappingSchema.ConfigurationList.Contains(ProviderName.PostgreSQL15, StringComparer.Ordinal)
						|| sqlBuilder.MappingSchema.ConfigurationList.Contains(ProviderName.PostgreSQL18, StringComparer.Ordinal)
						|| sqlBuilder.MappingSchema.ConfigurationList.Contains(ProviderName.PostgreSQL19, StringComparer.Ordinal))
					{
						stringBuilder.Append(' ');
						stringBuilder.Append(value);
					}
				}
			}
		}

		/// <summary>
		/// Adds a PostgreSQL subquery row-locking hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">PostgreSQL row-locking hint, e.g. <c>FOR UPDATE</c> or <c>FOR SHARE</c>.</param>
		/// <param name="tableIDs">Optional table identifiers for the <c>OF</c> clause.</param>
		/// <returns>Query source with subquery hint.</returns>
		/// <remarks>
		/// The <c>tableIDs</c> values are created with <c>Sql.TableAlias</c>, <c>Sql.TableName</c>, or
		/// <c>Sql.TableSpec</c> for table sources marked with <c>TableID</c>. They resolve to the
		/// generated SQL identifiers for the selected table sources.
		/// </remarks>
		[AiTags(Groups = AiGroup.Hints, HintType = AiHintType.SubQuery, Execution = AiExecution.Deferred,
			Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics,
			Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.PostgreSQL, Sql.QueryExtensionScope.SubQueryHint, typeof(SubQueryTableHintExtensionBuilder))]
		[Sql.QueryExtension(null,                    Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
		public static IPostgreSQLSpecificQueryable<TSource> SubQueryTableHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new PostgreSQLSpecificQueryable<TSource>((IExpressionQuery<TSource>)currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryTableHint, source, hint, tableIDs),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(Sql.SqlID), tableIDs.Select(p => Expression.Constant(p))))));
		}

		/// <summary>
		/// Adds a PostgreSQL subquery row-locking hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">PostgreSQL row-locking hint, e.g. <c>FOR UPDATE</c> or <c>FOR SHARE</c>.</param>
		/// <param name="hint2">NOWAIT | SKIP LOCKED</param>
		/// <param name="tableIDs">Optional table identifiers for the <c>OF</c> clause.</param>
		/// <returns>Query source with subquery hint.</returns>
		/// <remarks>
		/// The <c>tableIDs</c> values are created with <c>Sql.TableAlias</c>, <c>Sql.TableName</c>, or
		/// <c>Sql.TableSpec</c> for table sources marked with <c>TableID</c>. They resolve to the
		/// generated SQL identifiers for the selected table sources.
		/// </remarks>
		[AiTags(Groups = AiGroup.Hints, HintType = AiHintType.SubQuery, Execution = AiExecution.Deferred,
			Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics,
			Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.PostgreSQL, Sql.QueryExtensionScope.SubQueryHint, typeof(SubQueryTableHintExtensionBuilder))]
		[Sql.QueryExtension(null,                    Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
		public static IPostgreSQLSpecificQueryable<TSource> SubQueryTableHint<TSource>(
			this IPostgreSQLSpecificQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] string hint2,
			[SqlQueryDependent] params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new PostgreSQLSpecificQueryable<TSource>((IExpressionQuery<TSource>)currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryTableHint, source, hint, hint2, tableIDs),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.Constant(hint2),
					Expression.NewArrayInit(typeof(Sql.SqlID), tableIDs.Select(p => Expression.Constant(p))))));
		}
	}
}
