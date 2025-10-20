using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class ExpressionBuilder
	{
		static readonly string[] _orderByNames = [nameof(Queryable.OrderBy), nameof(Queryable.OrderByDescending), nameof(Queryable.ThenBy), nameof(Queryable.ThenByDescending)];
		static readonly string[] _allowedNames = [nameof(Queryable.Select), nameof(Queryable.Where), nameof(Queryable.Distinct), nameof(Queryable.OrderBy), .._orderByNames];

		class AggregationContext : IAggregationContext
		{
			public ContextRefExpression?                    RootContext      { get; set; }
			public ParameterExpression?                     ValueParameter   { get; set; }
			public Expression?                              FilterExpression { get; set; }
			public Expression?                              ValueExpression  { get; set; }
			public Expression[]?                            Items            { get; set; }
			public ITranslationContext.OrderByInformation[] OrderBy          { get; set; } = [];
			public bool                                     IsDistinct       { get; set; }
			public bool                                     IsGroupBy        { get; set; }
			public ContextRefExpression?                    SqlContext       { get; set; }

			public bool TranslateExpression(Expression expression, [NotNullWhen(true)] out ISqlExpression? sql, out SqlErrorExpression? error)
			{
				error = null;
				sql = null;

				if (SqlContext == null)
					return false;

				var builder = SqlContext.BuildContext.Builder;

				var translated = builder.BuildSqlExpression(SqlContext.BuildContext, expression);

				if (translated is not SqlPlaceholderExpression placeholder)
				{
					error = SqlErrorExpression.EnsureError(expression);
					return false;
				}

				sql = placeholder.Sql;

				if (sql is SqlSearchCondition)
				{
					var optimizer         = new SqlExpressionOptimizerVisitor(true);
					var evaluationContext = new EvaluationContext();
					var nullability       = NullabilityContext.GetContext(SqlContext?.BuildContext.SelectQuery);
					var optimized = optimizer.Optimize(evaluationContext, nullability, null, builder.DataOptions, RootContext?.BuildContext.MappingSchema ?? builder.MappingSchema, sql, true, true);
					sql = (ISqlExpression)optimized;
				}

				return true;
			}

			public LambdaExpression SimplifyEntityLambda(LambdaExpression lambda, int parameterIndex)
			{
				if (RootContext == null)
					throw new InvalidOperationException("Root context is not set for aggregation function.");

				var paramToReplace = lambda.Parameters[parameterIndex];
				var newBody = lambda.Body.Transform(e =>
				{
					if (e == paramToReplace)
					{
						var contextTyped = RootContext.WithType(e.Type);
						return contextTyped;
					}

					return e;
				});

				var newParameters = lambda.Parameters.ToList();
				newParameters.RemoveAt(parameterIndex);

				return Expression.Lambda(newBody, newParameters);
			}

			public bool TranslateLambdaExpression(LambdaExpression lambdaExpression, [NotNullWhen(true)] out ISqlExpression? sql, out SqlErrorExpression? error)
			{
				error = null;
				sql = null;
				if (RootContext == null)
					return false;

				var newLambda = SimplifyEntityLambda(lambdaExpression, 0);

				return TranslateExpression(newLambda.Body, out sql, out error);
			}
		}

		public Expression? BuildArrayAggregationFunction(
			Expression methodsChain,
			Expression functionExpression,
			ITranslationContext.AllowedAggregationOperators allowedOperations,
			Func<IAggregationContext, (ISqlExpression? sqlExpr, SqlErrorExpression? error)> functionFactory)
		{
			if (_buildVisitor.BuildContext == null)
				return null;

			Expression?                                   filterExpression = null;
			List<ITranslationContext.OrderByInformation>? orderBy          = null;
			bool                                          isDistinct       = false;
			bool                                          isGroupBy        = false;

			List<MethodCallExpression>? chain = null;

			var current = methodsChain.UnwrapConvert();

			NewArrayExpression?   newArrayExpression = null;

			var orderDefined = false;

			while (true)
			{
				if (current is NewArrayExpression newArray)
				{
					newArrayExpression = (NewArrayExpression?)BuildTraverseExpression(newArray);
					break;
				}

				if (current is MethodCallExpression methodCall)
				{
					if (methodCall.IsQueryable(nameof(Queryable.AsQueryable)) || methodCall.IsQueryable(nameof(Enumerable.AsEnumerable)))
					{
						current = methodCall.Arguments[0];
						continue;
					}

					if (methodCall.IsQueryable(_allowedNames))
					{
						current = methodCall.Arguments[0];

						if (methodCall.IsQueryable(_orderByNames))
						{
							if (orderDefined)
								continue;
							if (methodCall.Method.Name.StartsWith(nameof(Queryable.OrderBy)))
								orderDefined = true;
						}

						chain ??= new List<MethodCallExpression>();
						chain.Add(methodCall);
						continue;
					}
				}

				break;
			}

			if (newArrayExpression == null)
			{
				return null;
			}

			var         arrayElements          = newArrayExpression.Expressions.ToArray();
			var         valueParameter         = Expression.Parameter(arrayElements[0].Type.GetElementType() ?? arrayElements[0].Type, "e");
			Expression? currentValueExpression = valueParameter;

			if (chain != null)
			{
				for (int i = chain.Count - 1; i >= 0; i--)
				{
					var method = chain[i];

					if (method.IsQueryable(nameof(Queryable.Distinct)))
					{
						// Distinct should be the first method in the chain
						if (i != 0)
						{
							return null;
						}

						if (!IsAllowedOperation(ITranslationContext.AllowedAggregationOperators.Distinct))
						{
							return null;
						}

						isDistinct = true;
					}
					else if (method.IsQueryable(nameof(Queryable.Select)))
					{
						// do not support complex projections
						if (method.Arguments.Count != 2)
						{
							return null;
						}

						var lambda = method.Arguments[1].UnwrapLambda();
						currentValueExpression = lambda.GetBody(currentValueExpression);

					}
					else if (method.IsQueryable(nameof(Queryable.Where)))
					{
						if (!IsAllowedOperation(ITranslationContext.AllowedAggregationOperators.Filter))
						{
							return null;
						}

						var        lambda = method.Arguments[1].UnwrapLambda();
						var filter = lambda.GetBody(currentValueExpression);

						if (filterExpression == null)
							filterExpression = filter;
						else
							filterExpression = Expression.AndAlso(filterExpression, filter);
					}
					else if (method.IsQueryable(_orderByNames))
					{
						if (!IsAllowedOperation(ITranslationContext.AllowedAggregationOperators.OrderBy))
						{
							return null;
						}

						Expression orderByExpression;

						var lambda = method.Arguments[1].UnwrapLambda();
						orderByExpression = lambda.GetBody(currentValueExpression);

						orderBy ??= new List<ITranslationContext.OrderByInformation>();

						orderBy.Add(new ITranslationContext.OrderByInformation(
							orderByExpression.UnwrapConvert(),
							method.Method.Name is nameof(Queryable.OrderByDescending) or nameof(Queryable.ThenByDescending),
							Sql.NullsPosition.None
						));
					}
					else
					{
						return null;
					}
				}
			}

			var sqlContext = _buildVisitor.BuildContext;
			var rootContext = SequenceHelper.CreateRef(sqlContext);

			var aggregationInfo = new AggregationContext
			{
				RootContext      = rootContext,
				SqlContext       = rootContext,
				FilterExpression = filterExpression,
				ValueParameter   = valueParameter,
				ValueExpression  = null,
				Items            = arrayElements,
				OrderBy          = orderBy?.ToArray() ?? [],
				IsDistinct       = isDistinct,
				IsGroupBy        = isGroupBy
			};

			if (sqlContext != null)
			{
				var result = functionFactory(aggregationInfo);
				if (result.sqlExpr != null)
				{
					return CreatePlaceholder(sqlContext, result.sqlExpr, functionExpression, functionExpression.Type);
				}

				return result.error;
			}

			return null;

			bool IsAllowedOperation(ITranslationContext.AllowedAggregationOperators operation)
			{
				return allowedOperations.HasFlag(operation);
			}
		}

		public Expression? BuildAggregationFunction( 
			Expression                                                                       methodsChain,
			Expression                                                                       functionExpression,
			ITranslationContext.AllowedAggregationOperators                                  allowedOperations,
			Func<IAggregationContext, (ISqlExpression? sqlExpr, SqlErrorExpression? error)>  functionFactory)
		{
			Expression?                                   filterExpression = null;
			Expression?                                   buildRoot        = null;
			Expression?                                   valueExpression  = null;
			List<ITranslationContext.OrderByInformation>? orderBy          = null;
			bool                                          isDistinct       = false;
			bool                                          isGroupBy        = false;

			List<MethodCallExpression>? chain = null;

			var current = methodsChain.UnwrapConvert();

			ContextRefExpression? contextRef         = null;

			var orderDefined = false;

			while (true)
			{
				if (current is ContextRefExpression refExpression)
				{
					var root = BuildTraverseExpression(current);
					if (ExpressionEqualityComparer.Instance.Equals(root, current))
					{
						contextRef = refExpression;
						break;
					}

					current = root;
					continue;
				}

				if (current is MethodCallExpression methodCall)
				{
					if (methodCall.IsQueryable(nameof(Queryable.AsQueryable)) || methodCall.IsQueryable(nameof(Enumerable.AsEnumerable)))
					{
						current = methodCall.Arguments[0];
						continue;
					}

					if (methodCall.IsQueryable(_allowedNames))
					{
						current = methodCall.Arguments[0];

						if (methodCall.IsQueryable(_orderByNames))
						{
							if (orderDefined)
								continue;
							if (methodCall.Method.Name.StartsWith(nameof(Queryable.OrderBy)))
								orderDefined = true;
						}

						chain ??= new List<MethodCallExpression>();
						chain.Add(methodCall);
						continue;
					}
				}

				break;
			}

			if (contextRef == null)
			{
				var buildResult = TryBuildSequence(new BuildInfo(_buildVisitor.BuildContext, methodsChain, new SelectQuery()));
				if (buildResult.BuildContext == null)
					return buildResult.ErrorExpression;

				contextRef = SequenceHelper.CreateRef(buildResult.BuildContext);
			}

			var currentRef = contextRef;

			buildRoot = current;

			if (chain != null)
			{
				for (int i = chain.Count - 1; i >= 0; i--)
				{
					var method = chain[i];

					if (method.IsQueryable(nameof(Queryable.Distinct)))
					{
						// Distinct should be the first method in the chain
						if (i != 0)
						{
							buildRoot = method;
							break;
						}

						if (!IsAllowedOperation(ITranslationContext.AllowedAggregationOperators.Distinct))
						{
							buildRoot = method;
							break;
						}

						isDistinct = true;
					}
					else if (method.IsQueryable(nameof(Queryable.Select)))
					{
						// do not support complex projections
						if (method.Arguments.Count != 2)
						{
							buildRoot = method;
							break;
						}

						var body = SequenceHelper.PrepareBody(method.Arguments[1].UnwrapLambda(), currentRef.BuildContext);

						var selectContext = new SelectContext(currentRef.BuildContext, body, currentRef.BuildContext, false);
						currentRef = new ContextRefExpression(selectContext.ElementType, selectContext);

					}
					else if (method.IsQueryable(nameof(Queryable.Where)))
					{
						if (!IsAllowedOperation(ITranslationContext.AllowedAggregationOperators.Filter))
						{
							buildRoot = method;
							break;
						}

						var filter = SequenceHelper.PrepareBody(method.Arguments[1].UnwrapLambda(), currentRef.BuildContext);
						if (filterExpression == null)
							filterExpression = filter;
						else
							filterExpression = Expression.AndAlso(filterExpression, filter);
					}
					else if (method.IsQueryable(_orderByNames))
					{
						if (!IsAllowedOperation(ITranslationContext.AllowedAggregationOperators.OrderBy))
						{
							buildRoot = method;
							break;
						}

						var orderByExpression = SequenceHelper.PrepareBody(method.Arguments[1].UnwrapLambda(), currentRef.BuildContext);

						orderBy ??= new List<ITranslationContext.OrderByInformation>();

						orderBy.Add(new ITranslationContext.OrderByInformation(
							orderByExpression.UnwrapConvert(),
							method.Method.Name is nameof(Queryable.OrderByDescending) or nameof(Queryable.ThenByDescending),
							Sql.NullsPosition.None
						));
					}
					else
					{
						buildRoot = method;
						break;
					}
				}
			}

			var sqlContext = currentRef;
			var rootRef    = sqlContext;

			if (contextRef.BuildContext is GroupByBuilder.GroupByContext groupByCtx)
			{
				isGroupBy  = true;
				sqlContext = SequenceHelper.CreateRef(groupByCtx.SubQuery);
			}

			valueExpression = currentRef;

			var aggregationInfo = new AggregationContext
			{
				RootContext      = rootRef,
				SqlContext       = sqlContext,
				FilterExpression = filterExpression,
				ValueParameter   = null,
				ValueExpression  = valueExpression,
				Items            = null,
				OrderBy          = orderBy?.ToArray() ?? [],
				IsDistinct       = isDistinct,
				IsGroupBy        = isGroupBy
			};

			var result = functionFactory(aggregationInfo);
			if (result.sqlExpr != null)
			{
				return CreatePlaceholder(sqlContext.BuildContext, result.sqlExpr, functionExpression, functionExpression.Type);
			}

			return result.error;

			bool IsAllowedOperation(ITranslationContext.AllowedAggregationOperators operation)
			{
				return allowedOperations.HasFlag(operation);
			}
		}
	}
}
