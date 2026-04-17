using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class ExpressionBuilder
	{
		Expression ProcessEagerLoadingExpression(
			IBuildContext          buildContext,
			SqlEagerLoadExpression eagerLoad,
			ParameterExpression    queryParameter,
			List<Preamble>         preambles,
			Expression[]           previousKeys,
			EagerLoadState         state)
		{
			// Default strategy does not populate state today; kept as a parameter for API symmetry
			// with the other Process* strategies so CompleteEagerLoadingExpressions can thread state uniformly.
			_ = state;

			var cloningContext = new CloningContext();

			var itemType = eagerLoad.Type.GetItemType();

			if (itemType == null)
				throw new InvalidOperationException("Could not retrieve itemType for EagerLoading.");

			var dependencies = new HashSet<Expression>(ExpressionEqualityComparer.Instance);

			var sequenceExpression = eagerLoad.SequenceExpression;
			//var sequenceExpression = UnwrapDefaultIfEmpty(eagerLoad.SequenceExpression);

			sequenceExpression = ExpandContexts(buildContext, sequenceExpression);
			//sequenceExpression = UnwrapDefaultIfEmpty(sequenceExpression);

			CollectDependencies(buildContext, sequenceExpression, dependencies);

			var clonedParentContext = cloningContext.CloneContext(buildContext);
			clonedParentContext = new EagerContext(new SubQueryContext(clonedParentContext), buildContext.ElementType);

			var correctedSequence  = cloningContext.CloneExpression(sequenceExpression);
			var correctedPredicate = cloningContext.CloneExpression(eagerLoad.Predicate);

			dependencies.AddRange(previousKeys);

			var mainKeys   = new Expression[dependencies.Count];
			var detailKeys = new Expression[dependencies.Count];

			int i = 0;
			foreach (var dependency in dependencies)
			{
				mainKeys[i]   = dependency;
				detailKeys[i] = cloningContext.CloneExpression(dependency);
				++i;
			}

			Expression resultExpression;

			var mainType   = clonedParentContext.ElementType;
			var detailType = TypeHelper.GetEnumerableElementType(eagerLoad.Type);

			if (dependencies.Count == 0)
			{
				var detailSequence = BuildSequence(new BuildInfo((IBuildContext?)null, correctedSequence, new SelectQuery()));

				var parameters = new object[] { detailSequence, queryParameter, preambles };

				resultExpression = _buildPreambleQueryDetachedMethodInfo
					.MakeGenericMethod(detailType)
					.InvokeExt<Expression>(this, parameters);
			}
			else
			{
				if (correctedPredicate != null)
				{
					var predicateExpr = BuildSqlExpression(clonedParentContext, correctedPredicate);

					if (predicateExpr is not SqlPlaceholderExpression { Sql: ISqlPredicate predicateSql })
					{
						throw SqlErrorExpression.EnsureError(predicateExpr, correctedPredicate.Type).CreateException();
					}

					clonedParentContext.SelectQuery.Where.EnsureConjunction().Add(predicateSql);
				}

				var orderByToApply = CollectOrderBy(correctedSequence);

				var mainKeyExpression   = GenerateKeyExpression(mainKeys, 0);
				var detailKeyExpression = GenerateKeyExpression(detailKeys, 0);

				var keyDetailType   = typeof(KeyDetailEnvelope<,>).MakeGenericType(mainKeyExpression.Type, detailType);
				var mainParameter   = Expression.Parameter(mainType, "m");
				var detailParameter = Expression.Parameter(detailType, "d");

				var keyDetailExpression = Expression.New(keyDetailType.GetConstructor([mainKeyExpression.Type, detailType])!, detailKeyExpression, detailParameter);

				var clonedParentContextRef = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(clonedParentContext.ElementType), clonedParentContext);

				Expression sourceQuery = clonedParentContextRef;

				if (!typeof(IQueryable<>).IsSameOrParentOf(sourceQuery.Type))
				{
					sourceQuery = Expression.Call(Methods.Queryable.AsQueryable.MakeGenericMethod(mainType), sourceQuery);
				}

				sourceQuery = Expression.Call(Methods.LinqToDB.SelectDistinct.MakeGenericMethod(mainType), sourceQuery);

				var selector = Expression.Lambda(keyDetailExpression, mainParameter, detailParameter);

				var detailSelectorBody = correctedSequence;

				var detailSelector = _buildSelectManyDetailSelectorInfo
					.MakeGenericMethod(mainType, detailType)
					.InvokeExt<LambdaExpression>(null, new object[] { detailSelectorBody, mainParameter });

				var selectManyCall =
					Expression.Call(
						Methods.Queryable.SelectManyProjection.MakeGenericMethod(mainType, detailType, keyDetailType),
						sourceQuery, Expression.Quote(detailSelector), Expression.Quote(selector));

				var saveVisitor = _buildVisitor;
				_buildVisitor = _buildVisitor.Clone(cloningContext);

				cloningContext.UpdateContextParents();

				var detailSequence = BuildSequence(new BuildInfo((IBuildContext?)null, selectManyCall,
					clonedParentContextRef.BuildContext.SelectQuery));

				var parameters = new object?[] { detailSequence, mainKeyExpression, queryParameter, preambles, orderByToApply, detailKeys };

				resultExpression = _buildPreambleQueryAttachedMethodInfo
					.MakeGenericMethod(mainKeyExpression.Type, detailType)
					.InvokeExt<Expression>(this, parameters);

				_buildVisitor = saveVisitor;
			}

			if (resultExpression is SqlErrorExpression errorExpression)
			{
				return errorExpression.WithType(eagerLoad.Type);
			}

			resultExpression = SqlAdjustTypeExpression.AdjustType(resultExpression, eagerLoad.Type, MappingSchema);

			return resultExpression;
		}

		sealed class DatachedPreamble<T> : Preamble
		{
			readonly Query<T> _query;

			public DatachedPreamble(Query<T> query)
			{
				_query = query;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
			{
				return _query.GetResultEnumerable(dataContext, expressions, preambles, preambles).ToList();
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken)
			{
				return await _query.GetResultEnumerable(dataContext, expressions, preambles, preambles).ToListAsync(cancellationToken).ConfigureAwait(false);
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				foreach (var query in _query.Queries)
				{
					QueryHelper.CollectParametersAndValues(query.Statement, parameters, values);
				}
			}
		}

		sealed class Preamble<TKey, T> : Preamble
			where TKey : notnull
		{
			readonly Query<KeyDetailEnvelope<TKey, T>> _query;

			public Preamble(Query<KeyDetailEnvelope<TKey, T>> query)
			{
				_query = query;
			}

			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
			{
				var result = new PreambleResult<TKey, T>();
				foreach (var e in _query.GetResultEnumerable(dataContext, expressions, preambles, preambles))
				{
					result.Add(e.Key, e.Detail);
				}

				return result;
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles,
				CancellationToken                                        cancellationToken)
			{
				var result = new PreambleResult<TKey, T>();

				var enumerator = _query.GetResultEnumerable(dataContext, expressions, preambles, preambles)
					.GetAsyncEnumerator(cancellationToken);

				while (await enumerator.MoveNextAsync().ConfigureAwait(false))
				{
					var e = enumerator.Current;
					result.Add(e.Key, e.Detail);
				}

				return result;
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				foreach (var query in _query.Queries)
				{
					QueryHelper.CollectParametersAndValues(query.Statement, parameters, values);
				}
			}
		}
	}
}
