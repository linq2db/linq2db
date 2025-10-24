using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
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
			public ContextRefExpression?                    RootContext         { get; set; }
			public ParameterExpression?                     ValueParameter      { get; set; }
			public Expression[]?                            FilterExpressions    { get; set; }
			public Expression?                              ValueExpression     { get; set; }
			public Expression[]?                            Items               { get; set; }
			public ITranslationContext.OrderByInformation[] OrderBy             { get; set; } = [];
			public bool                                     IsDistinct          { get; set; }
			public bool                                     IsGroupBy           { get; set; }
			public ContextRefExpression?                    SqlContext          { get; set; }
			public SelectQuery?                             SelectQuery         => SqlContext?.BuildContext.SelectQuery;

			public bool TranslateExpression(Expression expression, [NotNullWhen(true)] out ISqlExpression? sql, [NotNullWhen(false)] out SqlErrorExpression? error)
			{
				error = null;
				sql = null;

				if (SqlContext == null)
				{
					error = SqlErrorExpression.EnsureError(expression);
					return false;
				}

				var builder = SqlContext.BuildContext.Builder;

				Expression translated;
				if (expression is ContextRefExpression { BuildContext: GroupByBuilder.GroupByContext groupByContext })
				{
					translated = builder.BuildSqlExpression(groupByContext, SequenceHelper.CreateRef(groupByContext.Element));
				}
				else
				{
					translated = builder.BuildSqlExpression(SqlContext.BuildContext, expression);
				}

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

			public bool TranslateLambdaExpression(LambdaExpression lambdaExpression, [NotNullWhen(true)] out ISqlExpression? sql, [NotNullWhen(false)] out SqlErrorExpression? error)
			{
				error = null;
				sql = null;
				if (RootContext == null)
				{
					error = SqlErrorExpression.EnsureError(lambdaExpression);
					return false;
				}

				var newLambda = SimplifyEntityLambda(lambdaExpression, 0);

				return TranslateExpression(newLambda.Body, out sql, out error);
			}
		}

		public Expression? BuildArrayAggregationFunction(
			Expression                                                                                                      methodsChain,
			Expression                                                                                                      functionExpression,
			ITranslationContext.AllowedAggregationOperators                                                                 allowedOperations,
			Func<IAggregationContext, (ISqlExpression? sqlExpr, SqlErrorExpression? error, Expression? fallbackExpression)> functionFactory)
		{
			if (_buildVisitor.BuildContext == null)
				return null;

			List<Expression>?                             filterExpression = null;
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
						if (!IsAllowedOperation(ITranslationContext.AllowedAggregationOperators.Distinct))
						{
							return null;
						}

						// Distinct should be the first method in the chain
						if (i != 0)
						{
							var orderByCount = chain.Take(i).Count(m => m.IsQueryable(_orderByNames));

							if (i != orderByCount)
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

						filterExpression ??= new List<Expression>();
						filterExpression.Add(filter);
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
				RootContext         = rootContext,
				SqlContext          = rootContext,
				FilterExpressions    = filterExpression?.ToArray(),
				ValueParameter      = valueParameter,
				ValueExpression     = null,
				Items               = arrayElements,
				OrderBy             = orderBy?.ToArray() ?? [],
				IsDistinct          = isDistinct,
				IsGroupBy           = isGroupBy
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

		static Expression UnwrapEnumerableCasting(Expression expression)
		{
			if (expression is MethodCallExpression methodCall)
			{
				if (methodCall.IsQueryable(nameof(Queryable.AsQueryable)) || methodCall.IsQueryable(nameof(Enumerable.AsEnumerable)))
				{
					return UnwrapEnumerableCasting(methodCall.Arguments[0]);
				}
			}
			return expression;
		}

		static Expression EnsureEnumerableType(Expression expression, Type targetType)
		{
			if (expression.Type == targetType)
				return expression;

			if (targetType.IsAssignableFrom(expression.Type))
				return expression;

			var unwrapped = UnwrapEnumerableCasting(expression);
			if (!ReferenceEquals(unwrapped, expression))
				return EnsureEnumerableType(unwrapped, targetType);

			if (typeof(IQueryable<>).IsSameOrParentOf(targetType))
			{
				var elementType = TypeHelper.GetEnumerableElementType(targetType);
				return Expression.Call(Methods.Queryable.AsQueryable.MakeGenericMethod(elementType), expression);
			}

			if (typeof(IEnumerable<>).IsSameOrParentOf(targetType))
			{
				var elementType = TypeHelper.GetEnumerableElementType(targetType);
				return Expression.Call(Methods.Enumerable.AsEnumerable.MakeGenericMethod(elementType), expression);
			}

			return Expression.Convert(expression, targetType);
		}

		static Expression UnwrapAggregateExecutors(Expression expression, out bool isInAggregation)
		{
			var unwrapped = expression.Transform(e =>
			{
				if (e is ContextRefExpression { BuildContext: AggregateExecuteBuilder.AggregateRootContext aggregateRootContext })
				{
					var sequence = aggregateRootContext.SequenceExpression;
					if (!e.Type.IsAssignableFrom(sequence.Type))
					{
						sequence = EnsureEnumerableType(sequence, e.Type);
					}

					return sequence;
				}

				return e;
			});

			isInAggregation = !ReferenceEquals(unwrapped, expression);

			return unwrapped;
		}

		public static Expression BuildAggregateExecuteExpression(MethodCallExpression methodCall, Expression sequenceExpression, Expression dataSequence, IReadOnlyList<MethodCallExpression>? chain, out bool isInAggregation)
		{
			if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));

			var argIndex = methodCall.Arguments.IndexOf(sequenceExpression);
			if (argIndex < 0)
				throw new ArgumentException("The provided sequence expression is not an argument of the method call.", nameof(sequenceExpression));

			var sequenceElementType = TypeHelper.GetEnumerableElementType(sequenceExpression.Type);
			var elementType         = TypeHelper.GetEnumerableElementType(dataSequence.Type);
			var sourceParam         = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(elementType), "source");
			var resultType          = methodCall.Type;

			var sourceParamTyped = EnsureEnumerableType(sourceParam, dataSequence.Type);

			var body = chain == null ? sourceParamTyped : chain[0].Replace(dataSequence, sourceParamTyped);

			Expression[] arguments = [..methodCall.Arguments.Take(argIndex).Select(a => a.Unwrap()), body, ..methodCall.Arguments.Skip(argIndex + 1).Select(a => a.Unwrap())];

			Type[] typeArguments = methodCall.Method.IsGenericMethod
				? methodCall.Method.GetGenericArguments().Length == 1 ? [sequenceElementType] : [sequenceElementType, resultType]
				: [];

			var aggregationBody = Expression.Call(methodCall.Method.DeclaringType!, methodCall.Method.Name,
				typeArguments,
				arguments);

			var aggregationLambda = Expression.Lambda(aggregationBody, sourceParam);

			var sequenceArgument = UnwrapAggregateExecutors(dataSequence, out isInAggregation);
			if (!typeof(IQueryable<>).IsSameOrParentOf(sequenceArgument.Type))
				sequenceArgument = Expression.Call(Methods.Queryable.AsQueryable.MakeGenericMethod(elementType), sequenceArgument);

			var method            = typeof(LinqExtensions).GetMethod(nameof(LinqExtensions.AggregateExecute));
			var genericMethod     = method!.MakeGenericMethod([elementType, resultType]);
			var executeExpression = Expression.Call(genericMethod, sequenceArgument, aggregationLambda);

			return executeExpression;
		}

		public Expression? BuildAggregationFunction( 
			Expression                                                                                                      sequenceExpression,
			Expression                                                                                                      functionExpression,
			ITranslationContext.AllowedAggregationOperators                                                                 allowedOperations,
			Func<IAggregationContext, (ISqlExpression? sqlExpr, SqlErrorExpression? error, Expression? fallbackExpression)> functionFactory)
		{
			List<Expression>?                             filterExpression = null;
			Expression?                                   buildRoot        = null;
			Expression?                                   valueExpression  = null;
			List<ITranslationContext.OrderByInformation>? orderBy          = null;
			bool                                          isDistinct       = false;
			bool                                          isGroupBy        = false;

			List<MethodCallExpression>? chain = null;

			var current = sequenceExpression.UnwrapConvert();

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
				var aggregation = BuildAggregateExecuteExpression((MethodCallExpression)functionExpression, sequenceExpression, current, chain!, out var isInAggregation);

				if (isInAggregation)
					return new MarkerExpression(aggregation, MarkerType.AggregationFallback);

				return aggregation;
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
						if (!IsAllowedOperation(ITranslationContext.AllowedAggregationOperators.Distinct))
						{
							buildRoot = method;
						}
						else
						{
							// Distinct should be the first method in the chain
							if (i != 0)
							{
								var orderByCount = chain.Take(i).Count(m => m.IsQueryable(_orderByNames));

								if (i != orderByCount)
									return null;
							}
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
						}
						else
						{
							var filter = SequenceHelper.PrepareBody(method.Arguments[1].UnwrapLambda(), currentRef.BuildContext);
							filterExpression ??= new List<Expression>();
							filterExpression.Add(filter);
						}
					}
					else if (method.IsQueryable(_orderByNames))
					{
						if (!IsAllowedOperation(ITranslationContext.AllowedAggregationOperators.OrderBy))
						{
							buildRoot = method;
						}
						else
						{
							var orderByExpression = SequenceHelper.PrepareBody(method.Arguments[1].UnwrapLambda(), currentRef.BuildContext);

							orderBy ??= new List<ITranslationContext.OrderByInformation>();

							orderBy.Add(new ITranslationContext.OrderByInformation(
								orderByExpression.UnwrapConvert(),
								method.Method.Name is nameof(Queryable.OrderByDescending) or nameof(Queryable.ThenByDescending),
								Sql.NullsPosition.None
							));
						}
					}
					else
					{
						buildRoot = method;
						break;
					}
				}
			}

			if (buildRoot != current || (contextRef.BuildContext is not GroupByBuilder.GroupByContext && contextRef.BuildContext is not AggregateExecuteBuilder.AggregateRootContext))
			{
				var aggregation = BuildAggregateExecuteExpression((MethodCallExpression)functionExpression, sequenceExpression, buildRoot, chain, out var isInAggregation);

				if (isInAggregation)
					return new MarkerExpression(aggregation, MarkerType.AggregationFallback);

				return aggregation;
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
				RootContext       = rootRef,
				SqlContext        = sqlContext,
				FilterExpressions = filterExpression?.ToArray(),
				ValueParameter    = null,
				ValueExpression   = valueExpression,
				Items             = null,
				OrderBy           = orderBy?.ToArray() ?? [],
				IsDistinct        = isDistinct,
				IsGroupBy         = isGroupBy
			};

			var result = functionFactory(aggregationInfo);
			if (result.sqlExpr != null)
			{
				return CreatePlaceholder(sqlContext.BuildContext, result.sqlExpr, functionExpression, functionExpression.Type);
			}

			if (result.fallbackExpression != null)
			{
				return result.fallbackExpression;
			}

			return result.error;

			bool IsAllowedOperation(ITranslationContext.AllowedAggregationOperators operation)
			{
				return allowedOperations.HasFlag(operation);
			}
		}
	}
}
