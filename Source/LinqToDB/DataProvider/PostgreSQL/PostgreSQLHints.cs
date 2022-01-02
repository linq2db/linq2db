using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Expressions;
	using Linq;
	using SqlProvider;
	using SqlQuery;

	// https://www.postgresql.org/docs/current/sql-select.html
	//
	public static class PostgreSQLHints
	{
		public const string ForUpdate      = "FOR UPDATE";
		public const string ForNoKeyUpdate = "FOR NO KEY UPDATE";
		public const string ForShare       = "FOR SHARE";
		public const string ForKeyShare    = "FOR KEY SHARE";

		public const string NoWait     = "NOWAIT";
		public const string SkipLocked = "SKIP LOCKED";

		class SubQueryTableHintExtensionBuilder : ISqlQueryExtensionBuilder
		{
			void ISqlQueryExtensionBuilder.Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
			{
				var builder = (PostgreSQLSqlBuilder)sqlBuilder;
				var hint    = (SqlValue)sqlQueryExtension.Arguments["hint"];

				stringBuilder.Append((string)hint.Value!);

				var idCount = (int)((SqlValue)sqlQueryExtension.Arguments["tableIDs.Count"]).Value!;

				for (var i = 0; i < idCount; i++)
				{
					if (i == 0)
						stringBuilder.Append(" OF ");
					else if (i > 0)
						stringBuilder.Append(", ");

					var id    = (Sql.SqlID)((SqlValue)sqlQueryExtension.Arguments[$"tableIDs.{i}"]).Value!;
					var alias = builder.TableIDs?.TryGetValue(id.ID, out var a) == true ? a : id.ID;

					stringBuilder.Append(alias);
				}

				if (sqlQueryExtension.Arguments.TryGetValue("hint2", out var h) && h is SqlValue { Value: string value })
				{
					stringBuilder.Append(' ');
					stringBuilder.Append(value);
				}
			}
		}

		/// <summary>
		/// Adds join hint to a generated query.
		/// <code>
		/// // will produce following SQL code in generated query: INNER LOOP JOIN
		/// var tableWithHint = db.Table.JoinHint("LOOP");
		/// </code>
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added to join in generated query.</param>
		/// <param name="tableIDs">Table IDs.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.SubQueryHint, typeof(SubQueryTableHintExtensionBuilder))]
		public static IQueryable<TSource> SubQueryTableHint<TSource>(
			this IQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryTableHint, source, hint, tableIDs),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(Sql.SqlID), tableIDs.Select(p => Expression.Constant(p)))));
		}

		/// <summary>
		/// Adds join hint to a generated query.
		/// <code>
		/// // will produce following SQL code in generated query: INNER LOOP JOIN
		/// var tableWithHint = db.Table.JoinHint("LOOP");
		/// </code>
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added to join in generated query.</param>
		/// <param name="hint2">NOWAIT | SKIP LOCKED</param>
		/// <param name="tableIDs">Table IDs.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.SubQueryHint, typeof(SubQueryTableHintExtensionBuilder))]
		public static IQueryable<TSource> SubQueryTableHint<TSource>(
			this IQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] string hint2,
			[SqlQueryDependent] params Sql.SqlID[] tableIDs)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryTableHint, source, hint, hint2, tableIDs),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.Constant(hint2),
					Expression.NewArrayInit(typeof(Sql.SqlID), tableIDs.Select(p => Expression.Constant(p)))));
		}
	}
}
