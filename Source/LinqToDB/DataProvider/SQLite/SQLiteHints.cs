using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.DataProvider.SQLite;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Metadata;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SQLite
{
	public static class SQLiteHints
	{
		public static class Hint
		{
			public const string NotIndexed = "NOT INDEXED";

			[Sql.Expression("INDEXED BY {0}")]
			public static string IndexedBy(string value)
			{
				return "INDEXED BY " + value;
			}
		}

		/// <summary>
		/// Adds a SQLite index hint.
		/// </summary>
		[AiTags(Groups = AiGroup.Hints, HintType = AiHintType.Index, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[ExpressionMethod(nameof(IndexedByImpl))]
		public static ISQLiteSpecificTable<TSource> IndexedByHint<TSource>(this ISQLiteSpecificTable<TSource> table, string indexName)
			where TSource : notnull
		{
			return table.TableHint(Hint.IndexedBy(indexName));
		}

		static Expression<Func<ISQLiteSpecificTable<TSource>,string,ISQLiteSpecificTable<TSource>>> IndexedByImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexName) => table.TableHint(Hint.IndexedBy(indexName));
		}

		/// <summary>
		/// Adds a SQLite table hint.
		/// </summary>
		[AiTags(Groups = AiGroup.Hints, HintType = AiHintType.Table, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[ExpressionMethod(nameof(NotIndexedImpl))]
		public static ISQLiteSpecificTable<TSource> NotIndexedHint<TSource>(this ISQLiteSpecificTable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(Hint.NotIndexed);
		}

		static Expression<Func<ISQLiteSpecificTable<TSource>,ISQLiteSpecificTable<TSource>>> NotIndexedImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(Hint.NotIndexed);
		}

		/// <summary>
		/// Adds a SQLite table hint.
		/// </summary>
		[AiTags(Groups = AiGroup.Hints, HintType = AiHintType.Table, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.SQLite, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISQLiteSpecificTable<TSource> TableHint<TSource>(this ISQLiteSpecificTable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return new SQLiteSpecificTable<TSource>(newTable);
		}
	}
}
