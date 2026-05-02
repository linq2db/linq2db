using System;
using System.Diagnostics;
using System.Linq.Expressions;

using LinqToDB.Interceptors;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.Logging;
using LinqToDB.Linq;
using LinqToDB.Metrics;

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Internal.Linq
{
	public sealed class Query<T> : Query
	{
		#region Init

		public Query(IDataContext dataContext)
			: base(dataContext)
		{
			DoNotCache = NoLinqCache.IsNoCache;
		}

		internal override void Init(IBuildContext parseContext)
		{
			var statement = parseContext.GetResultStatement();

			Queries.Add(new QueryInfo
			{
				Statement          = statement,
			});
		}

		#endregion

		#region Properties & Fields

		internal bool DoNotCache;

		internal IQueryExpressions? CompiledExpressions;

		internal Func<IDataContext,IQueryExpressions,object?[]?,object?[]?,IResultEnumerable<T>> GetResultEnumerable = null!;

		#endregion

		#region Query

		static Query()
		{
			CacheCleaners.Enqueue(ClearCache);
		}

		/// <summary>
		/// Empties LINQ query cache for <typeparamref name="T"/> entity type.
		/// </summary>
		public static void ClearCache() => QueryCache.Default.ClearForType(typeof(T));

		/// <summary>
		/// Count of queries which has not been found in cache.
		/// </summary>
		public static long CacheMissCount => QueryCache.Default.GetMissCount(typeof(T));

		static Expression ExposeAndPrepareExpression(Expression expr, IDataContext dataContext, ExpressionTreeOptimizationContext optimizationContext)
		{
			var        beforeExpose = expr;
			Expression exposed;
			var        iteration    = 0;
			do
			{
				exposed = ExpressionBuilder.ExposeExpression(beforeExpose, dataContext, optimizationContext, null,
					optimizeConditions: true, compactBinary: false /* binary already compacted by AggregateExpression*/);

				if (iteration > 0 && ReferenceEquals(beforeExpose, exposed))
				{
					// no changes, no need to continue
					break;
				}

				if (dataContext is IInterceptable<IQueryExpressionInterceptor> { Interceptor: { } interceptor })
				{
					var processed = interceptor.ProcessExpression(exposed, new QueryExpressionArgs(dataContext, exposed, QueryExpressionArgs.ExpressionKind.ExposedQuery));
					if (!ReferenceEquals(processed, exposed))
					{
						// Doe exposing again after interceptor processing
						exposed      = processed;
						beforeExpose = exposed;

						if (++iteration > 10)
						{
							// guard from infinite loop
							break;
						}

						continue;
					}
				}

				break;
			} while (true);

			return exposed;
		}

		public static Query<T> GetQuery(IDataContext dataContext, ref IQueryExpressions expressions, out bool dependsOnParameters)
		{
			using var mt = ActivityService.Start(ActivityID.GetQueryTotal);

			ExpressionTreeOptimizationContext optimizationContext;
			DataOptions                       dataOptions;
			var                               queryFlags = QueryFlags.None;
			Query<T>?                         query;
			bool                              useCache;

			using (ActivityService.Start(ActivityID.GetQueryFind))
			{
				var expr = expressions.MainExpression;

				using (ActivityService.Start(ActivityID.GetQueryFindExpose))
				{
					optimizationContext = new ExpressionTreeOptimizationContext(dataContext);

					// I hope fast tree optimization for unbalanced Binary Expressions. See Issue447Tests.
					//
					expr = BinaryExpressionAggregatorVisitor.Instance.Visit(expr);

					dependsOnParameters = false;

					if (dataContext is IExpressionPreprocessor preprocessor)
						expr = preprocessor.ProcessExpression(expr);

					if (dataContext is IInterceptable<IQueryExpressionInterceptor> { Interceptor: { } interceptor })
						expr = interceptor.ProcessExpression(expr, new QueryExpressionArgs(dataContext, expr, QueryExpressionArgs.ExpressionKind.Query));
				}

				dataOptions = dataContext.Options;

				useCache = !dataOptions.LinqOptions.DisableQueryCache;

				var runtimeExpressions = ReferenceEquals(expr, expressions.MainExpression) ? expressions : new RuntimeExpressionsContainer(expr);

				if (useCache)
				{
					queryFlags = dataContext.GetQueryFlags();
					using (ActivityService.Start(ActivityID.GetQueryFindFind))
					{
						var found = QueryCache.Default.Find(typeof(T), dataContext, runtimeExpressions, queryFlags);
						if (found != null)
						{
							expressions = found.Expressions;
							return (Query<T>)found.Query;
						}
					}
				}

				// Expose expression, call all needed invocations.
				// After execution there should be no constants which contains IDataContext reference, no constants with ExpressionQueryImpl
				// Parameters with SqlQueryDependentAttribute will be transferred to constants
				// No LambdaExpressions which are located in constants, they will be expanded and injected into tree
				//
				var exposed = ExposeAndPrepareExpression(expr, dataContext, optimizationContext);

				// simple trees do not mutate
				var isExposed = !ReferenceEquals(exposed, expr);

				expr = exposed;
				var currentQueries = new RuntimeExpressionsContainer(expr);
				expressions = currentQueries;

				if (isExposed && useCache)
				{
					dependsOnParameters = true;

					queryFlags |= QueryFlags.ExpandedQuery;

					// search again
					using (ActivityService.Start(ActivityID.GetQueryFindFind))
					{
						var findResult = QueryCache.Default.Find(typeof(T), dataContext, currentQueries, queryFlags);
						if (findResult != null)
						{
							expressions = findResult.Expressions;
							return (Query<T>)findResult.Query;
						}
					}
				}

				if (useCache)
				{
					// Cache missed, Build query
					//
					QueryCache.Default.IncrementMissCount(typeof(T));
				}
			}

			using (var mc = ActivityService.Start(ActivityID.GetQueryCreate))
			{
				query = CreateQuery(
					optimizationContext,
					 new ParametersContext(expressions, optimizationContext, dataContext),
					dataContext,
					ref expressions
				);
			}

			if (useCache && !query.DoNotCache)
			{
				QueryCache.Default.TryAdd(typeof(T), dataContext, query, expressions, queryFlags);
			}

			return query;
		}

		internal static Query<T> CreateQuery(ExpressionTreeOptimizationContext optimizationContext, ParametersContext parametersContext, IDataContext dataContext, ref IQueryExpressions expressions)
		{
			var query = new Query<T>(dataContext);

			try
			{
				var validateSubqueries = !ExpressionBuilder.NeedsSubqueryValidation(dataContext);
				query = new ExpressionBuilder(query, validateSubqueries, optimizationContext, parametersContext, dataContext, expressions.MainExpression, null).Build<T>(ref expressions);
				if (query.ErrorExpression != null)
				{
					if (!validateSubqueries)
					{
						query = new Query<T>(dataContext);
						query = new ExpressionBuilder(query, true, optimizationContext, parametersContext, dataContext, expressions.MainExpression, null).Build<T>(ref expressions);
					}

					if (query.ErrorExpression != null)
						throw query.ErrorExpression.CreateException();
				}
			}
			catch
			{
				var linqOptions = optimizationContext.DataContext.Options.LinqOptions;

				if (linqOptions.GenerateExpressionTest)
				{
					var testFile = ExpressionTestGenerator.GenerateSourceFile(dataContext, expressions.MainExpression);

					if (dataContext.GetTraceSwitch().TraceInfo)
					{
						dataContext.WriteTraceLine(
							$"Expression test code generated: \'{testFile}\'.",
							dataContext.GetTraceSwitch().DisplayName,
							TraceLevel.Info);
					}
				}
				else
				{
					dataContext.WriteTraceLine(
						"""
						To generate test code to diagnose the problem set 'LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true'.
						Or specify LINQ options during 'DataContextOptions' building 'options.UseGenerateExpressionTest(true)'
						""",
						dataContext.GetTraceSwitch().DisplayName,
						TraceLevel.Error);
				}

				throw;
			}

			return query;
		}

		#endregion
	}
}
