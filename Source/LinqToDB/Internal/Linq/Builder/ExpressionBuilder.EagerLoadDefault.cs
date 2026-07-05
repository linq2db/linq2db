using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class ExpressionBuilder
	{
		Expression ProcessEagerLoadingExpression(
			IBuildContext          buildContext,
			SqlEagerLoadExpression eagerLoad,
			ParameterExpression    queryParameter,
			List<Harvester>         harvesters,
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

			sequenceExpression = ExpandContexts(buildContext, sequenceExpression);

			CollectDependencies(buildContext, sequenceExpression, dependencies);

			var clonedParentContext = cloningContext.CloneContext(buildContext);
			clonedParentContext = new EagerContext(new SubQueryContext(clonedParentContext), buildContext.ElementType);

			var correctedSequence  = cloningContext.CloneExpression(sequenceExpression);
			var correctedPredicate = cloningContext.CloneExpression(eagerLoad.Predicate);

			dependencies.AddRange(previousKeys);

			var mainKeys   = dependencies.ToArray();
			var detailKeys = mainKeys.Select(e => cloningContext.CloneExpression(e)!).ToArray();

			Expression resultExpression;

			var mainType   = clonedParentContext.ElementType;
			var detailType = TypeHelper.GetEnumerableElementType(eagerLoad.Type);

			if (dependencies.Count == 0)
			{
				var detailSequence = BuildSequence(new BuildInfo((IBuildContext?)null, correctedSequence, new SelectQuery()));

				var parameters = new object[] { detailSequence, queryParameter, harvesters };

				resultExpression = _buildHarvesterQueryDetachedMethodInfo
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

				var parameters = new object?[] { detailSequence, mainKeyExpression, queryParameter, harvesters, orderByToApply, detailKeys };

				resultExpression = _buildHarvesterQueryAttachedMethodInfo
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

		sealed class DetachedHarvester<T>(Query<T> query) : Harvester
		{
			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? context)
			{
				return query.GetResultEnumerable(dataContext, expressions, parameters, context).ToList();
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? context, CancellationToken cancellationToken)
			{
				return await query.GetResultEnumerable(dataContext, expressions, parameters, context).ToListAsync(cancellationToken).ConfigureAwait(false);
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				QueryHelper.CollectParametersAndValues(query.QueryInfo.Statement, parameters, values);
			}
		}

		sealed class Harvester<TKey, T>(Query<KeyDetailEnvelope<TKey, T>> query) : Harvester, IStepMaterializer
			where TKey : notnull
		{
			public override object Execute(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? context)
				=> BuildResult(query.GetResultEnumerable(dataContext, expressions, parameters, context));

			public override Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? context, CancellationToken cancellationToken)
				=> BuildResultAsync(query.GetResultEnumerable(dataContext, expressions, parameters, context), cancellationToken);

			public bool CanCombine => query.GetResultFromReader != null;

			public SqlStatement? GetCombinableStatement()
				=> query.QueryInfo.Statement;

			public void AddCombinableParameterValues(SqlParameterValues values, IQueryExpressions expressions, IDataContext dataContext, object?[]? parameters)
			{
				QueryRunner.SetParameters(query, expressions, dataContext, parameters, values);
			}

			public object MaterializeFromReader(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? context, DbDataReader dataReader)
				=> BuildResult(query.GetResultFromReader!(dataContext, expressions, parameters, context, dataReader));

			public Task<object> MaterializeFromReaderAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? context, DbDataReader dataReader, CancellationToken cancellationToken)
				=> BuildResultAsync(query.GetResultFromReader!(dataContext, expressions, parameters, context, dataReader), cancellationToken);

			// Both the sequential (GetResultEnumerable) and combined (GetResultFromReader) paths bucket the same
			// KeyDetailEnvelope stream into a HarvesterResult; only the source enumerable differs.
			static HarvesterResult<TKey, T> BuildResult(IEnumerable<KeyDetailEnvelope<TKey, T>> source)
			{
				var result = new HarvesterResult<TKey, T>();

				foreach (var e in source)
					result.Add(e.Key, e.Detail);

				return result;
			}

			static async Task<object> BuildResultAsync(IAsyncEnumerable<KeyDetailEnvelope<TKey, T>> source, CancellationToken cancellationToken)
			{
				var result = new HarvesterResult<TKey, T>();

				await foreach (var e in source.WithCancellation(cancellationToken).ConfigureAwait(false))
					result.Add(e.Key, e.Detail);

				return result;
			}

			public override void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
			{
				QueryHelper.CollectParametersAndValues(query.QueryInfo.Statement, parameters, values);
			}
		}
	}
}
