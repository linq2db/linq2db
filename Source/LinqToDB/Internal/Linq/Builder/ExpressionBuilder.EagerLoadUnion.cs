using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class ExpressionBuilder
	{
		/// <summary>
		/// Processes a single CteUnion eager-load expression.
		/// Wraps the parent query in a CTE so child queries can reference it efficiently.
		/// Falls back to Default strategy if the expression is not suitable for CteUnion.
		/// </summary>
		Expression ProcessEagerLoadingCteUnion(
			IBuildContext          buildContext,
			SqlEagerLoadExpression eagerLoad,
			ParameterExpression    queryParameter,
			List<Preamble>         preambles,
			Expression[]           previousKeys)
		{
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
				// No dependencies — identical to Default for the detached case
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

					// Apply predicate to child query for Concat/Union discriminator filtering
					var childElementType = TypeHelper.GetEnumerableElementType(correctedSequence.Type)
						?? correctedSequence.Type;
					var childParam = Expression.Parameter(childElementType, "p_pred");
					var predicateLambda = Expression.Lambda(
						correctedPredicate.Transform(
							(clonedParentContext.ElementType, childParam),
							static (ctx, e) => e is ContextRefExpression cre && cre.BuildContext.ElementType == ctx.ElementType
								? ctx.childParam
								: e),
						childParam);

					if (typeof(IQueryable).IsAssignableFrom(correctedSequence.Type))
					{
						correctedSequence = Expression.Call(
							Methods.Queryable.Where.MakeGenericMethod(childElementType),
							correctedSequence, Expression.Quote(predicateLambda));
					}
					else
					{
						correctedSequence = Expression.Call(
							Methods.Enumerable.Where.MakeGenericMethod(childElementType),
							correctedSequence, predicateLambda);
					}
				}

				var orderByToApply = CollectOrderBy(correctedSequence);

				// Build key expressions (same as Default)
				Expression mainKeyExpression;
				Expression detailKeyExpression;

				if (mainKeys.Length == 1)
				{
					mainKeyExpression   = mainKeys[0];
					detailKeyExpression = detailKeys[0];
				}
				else
				{
					mainKeyExpression   = GenerateKeyExpression(mainKeys, 0);
					detailKeyExpression = GenerateKeyExpression(detailKeys, 0);
				}

				var keyType         = mainKeyExpression.Type;
				var keyDetailType   = typeof(KeyDetailEnvelope<,>).MakeGenericType(keyType, detailType);
				var mainParameter   = Expression.Parameter(mainType, "m");
				var detailParameter = Expression.Parameter(detailType, "d");

				var keyDetailExpression = Expression.New(
					keyDetailType.GetConstructor([keyType, detailType])!,
					detailKeyExpression, detailParameter);

				var clonedParentContextRef = new ContextRefExpression(
					typeof(IQueryable<>).MakeGenericType(clonedParentContext.ElementType), clonedParentContext);

				Expression sourceQuery = clonedParentContextRef;

				if (!typeof(IQueryable<>).IsSameOrParentOf(sourceQuery.Type))
				{
					sourceQuery = Expression.Call(
						Methods.Queryable.AsQueryable.MakeGenericMethod(mainType), sourceQuery);
				}

				// SELECT DISTINCT on parent keys
				sourceQuery = Expression.Call(
					Methods.LinqToDB.SelectDistinct.MakeGenericMethod(mainType), sourceQuery);

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
					.MakeGenericMethod(keyType, detailType)
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
	}
}
