using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Cache;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq.Builder;

namespace LinqToDB.Internal.Linq
{
	sealed class CompiledTable<T>
		where T : notnull
	{
		public CompiledTable(LambdaExpression lambda, Expression expression)
		{
			_lambda     = lambda;
			_expression = expression;
		}

		readonly LambdaExpression _lambda;
		readonly Expression       _expression;

		static bool ReplaceAsyncWithSync(MethodCallExpression methodCall, out MethodCallExpression newMethodCall)
		{
			newMethodCall = methodCall;
			var returnType = methodCall.Method.ReturnType;
			if (!typeof(Task).IsAssignableFrom(returnType))
			{
				return true;
			}

			var methodName = methodCall.Method.Name;
			if (!methodName.EndsWith("Async", StringComparison.Ordinal))
			{
				return true;
			}

			var newMethodName = methodName.Substring(0, methodName.Length - "Async".Length);
			var methods = GetSimilarMethods(methodCall.Type.DeclaringType)
				.Concat(GetSimilarMethods(typeof(Queryable)))
				.ToList();

			if (methods.Count == 0)
			{
				return false;
			}

			var sourceParametersArray = methodCall.Method.GetParameters();

			var destArguments         = methodCall.Arguments;

			ICollection<ParameterInfo> sourceParameters = sourceParametersArray;

			if (sourceParametersArray.Length > 0 && sourceParametersArray[^1].ParameterType == typeof(CancellationToken))
			{
				sourceParameters = sourceParametersArray.Take(sourceParametersArray.Length - 1).ToList();
				destArguments = destArguments.Take(destArguments.Count - 1).ToList().AsReadOnly();
			}

			var         sourceGenericArguments = methodCall.Method.GetGenericArguments();
			MethodInfo? targetMethod           = null;

			foreach (var method in methods)
			{
				if (methodCall.Method.IsGenericMethod)
				{
					if (!method.IsGenericMethod)
					{
						continue;
					}

					var genericArgs = method.GetGenericArguments();
					if (sourceGenericArguments.Length != genericArgs.Length)
						continue;

					var candidateMethod = method.MakeGenericMethod(sourceGenericArguments);

					if (TypeHelper.IsEqualParameters(sourceParameters, candidateMethod.GetParameters()))
					{
						targetMethod = candidateMethod;
						break;
					}
				}
				else
				{
					if (method.IsGenericMethod)
					{
						continue;
					}
					if (TypeHelper.IsEqualParameters(sourceParameters, method.GetParameters()))
					{
						targetMethod = method;
						break;
					}
				}
			}

			if (targetMethod == null)
			{
				return false;
			}

			newMethodCall = Expression.Call(targetMethod, destArguments);
			return true;

			List<MethodInfo> GetSimilarMethods(Type? methodsContainer)
			{
				if (methodsContainer == null)
					return [];

				var methodInfos = methodsContainer.GetMethods()
					.Where(m => m.Name == newMethodName)
					.ToList();
				return methodInfos;
			}
		}

		Query<T> GetInfo(IDataContext dataContext, object?[] parameterValues)
		{
			var configurationID = dataContext.ConfigurationID;
			var dataOptions     = dataContext.Options;

			var result = QueryRunner.Cache<T>.QueryCache.GetOrCreate(
				(
					operation: "CT",
					configurationID,
					expression : _expression,
					queryFlags : dataContext.GetQueryFlags()
				),
				(dataContext, lambda: _lambda, dataOptions, parameterValues),
				static (o, key, ctx) =>
				{
					o.SlidingExpiration = ctx.dataOptions.LinqOptions.CacheSlidingExpirationOrDefault;

					var correctedExpression = key.expression;

					if (key.expression is MethodCallExpression methodCall)
					{
						if (!ReplaceAsyncWithSync(methodCall, out var newMethodCall))
						{
							throw new InvalidOperationException("Cannot convert async method call to sync.");
						}

						correctedExpression = newMethodCall;
					}

					var optimizationContext = new ExpressionTreeOptimizationContext(ctx.dataContext);
					var exposed = ExpressionBuilder.ExposeExpression(correctedExpression, ctx.dataContext,
						optimizationContext, ctx.parameterValues, optimizeConditions : false, compactBinary : true);

					var query             = new Query<T>(ctx.dataContext);
					var expressions       = (IQueryExpressions)new RuntimeExpressionsContainer(exposed);
					var parametersContext = new ParametersContext(expressions, optimizationContext, ctx.dataContext);

					var validateSubqueries = !ExpressionBuilder.NeedsSubqueryValidation(ctx.dataContext);
					query = new ExpressionBuilder(query, validateSubqueries, optimizationContext, parametersContext, ctx.dataContext, exposed, ctx.parameterValues)
						.Build<T>(ref expressions);

					if (query.ErrorExpression != null)
					{
						if (!validateSubqueries)
						{
							query = new Query<T>(ctx.dataContext);

							query = new ExpressionBuilder(query, true, optimizationContext, parametersContext, ctx.dataContext, exposed, ctx.parameterValues)
								.Build<T>(ref expressions);
						}

						if (query.ErrorExpression != null)
							throw query.ErrorExpression.CreateException();
					}

					query.CompiledExpressions = expressions;

					return query;
				})!;

			return result;
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Method used by two-parameter call in generated expression")]
		public IQueryable<T> Create(object[] parameters, object[] preambles)
		{
			var db    = (IDataContext)parameters[0];
			var query = GetInfo(db, parameters);

			return new Table<T>(db, _expression) { Info = query, Parameters = parameters };
		}

		public T Execute(object[] parameters, object[] preambles)
		{
			var db    = (IDataContext)parameters[0];
			var query = GetInfo(db, parameters);

			return (T)query.GetElement(db, query.CompiledExpressions!, parameters, preambles)!;
		}

		public async Task<T> ExecuteAsync(object[] parameters, object[] preambles)
		{
			var db    = (IDataContext)parameters[0];
			var query = GetInfo(db, parameters);

			return (T)(await query.GetElementAsync(db, query.CompiledExpressions!, parameters, preambles, default).ConfigureAwait(false))!;
		}
	}
}
