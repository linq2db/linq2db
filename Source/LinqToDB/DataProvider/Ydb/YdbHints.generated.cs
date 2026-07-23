#nullable enable
// Generated.
//
using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Metadata;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.Ydb
{
	public static partial class YdbHints
	{
		// 1) IYdbSpecificQueryable<T>
		/// <summary>
		/// Adds a YDB <c>unique</c> query hint.
		/// </summary>
		[AiTags(Groups = AiGroup.Hints, HintType = AiHintType.Query, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[ExpressionMethod(nameof(UniqueHintImpl))]
		public static IYdbSpecificQueryable<TSource> UniqueHint<TSource>(
			this IYdbSpecificQueryable<TSource> query,
			params string[]                     columns)
			where TSource : notnull
		{
			return QueryHint(query, Unique, columns);
		}
		static Expression<Func<IYdbSpecificQueryable<TSource>,string[],IYdbSpecificQueryable<TSource>>> UniqueHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, columns) => QueryHint(query, Unique, columns);
		}

		// 2) IQueryable<T>
		/// <summary>
		/// Adds a YDB <c>unique</c> query hint.
		/// </summary>
		[AiTags(Groups = AiGroup.Hints, HintType = AiHintType.Query, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[ExpressionMethod(nameof(UniqueHintQImpl))]
		public static IYdbSpecificQueryable<TSource> UniqueHint<TSource>(
			this IQueryable<TSource> query,
			params string[]          columns)
			where TSource : notnull
		{
			// QueryHint(IQueryable<T>)
			return QueryHint(query, Unique, columns);
		}
		static Expression<Func<IQueryable<TSource>,string[],IYdbSpecificQueryable<TSource>>> UniqueHintQImpl<TSource>()
			where TSource : notnull
		{
			return (query, columns) => QueryHint(query, Unique, columns);
		}

		// 1) IYdbSpecificQueryable<T>
		/// <summary>
		/// Adds a YDB <c>distinct</c> query hint.
		/// </summary>
		[AiTags(Groups = AiGroup.Hints, HintType = AiHintType.Query, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[ExpressionMethod(nameof(DistinctHintImpl))]
		public static IYdbSpecificQueryable<TSource> DistinctHint<TSource>(
			this IYdbSpecificQueryable<TSource> query,
			params string[]                     columns)
			where TSource : notnull
		{
			return QueryHint(query, Distinct, columns);
		}
		static Expression<Func<IYdbSpecificQueryable<TSource>,string[],IYdbSpecificQueryable<TSource>>> DistinctHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, columns) => QueryHint(query, Distinct, columns);
		}

		// 2) IQueryable<T>
		/// <summary>
		/// Adds a YDB <c>distinct</c> query hint.
		/// </summary>
		[AiTags(Groups = AiGroup.Hints, HintType = AiHintType.Query, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[ExpressionMethod(nameof(DistinctHintQImpl))]
		public static IYdbSpecificQueryable<TSource> DistinctHint<TSource>(
			this IQueryable<TSource> query,
			params string[]          columns)
			where TSource : notnull
		{
			// QueryHint(IQueryable<T>)
			return QueryHint(query, Distinct, columns);
		}
		static Expression<Func<IQueryable<TSource>,string[],IYdbSpecificQueryable<TSource>>> DistinctHintQImpl<TSource>()
			where TSource : notnull
		{
			return (query, columns) => QueryHint(query, Distinct, columns);
		}

	}
}
