using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Interceptors;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Expressions.ExpressionVisitors;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Internal.Linq
{
	public abstract class Query
	{
		internal Func<IDataContext,IQueryExpressions,object?[]?,object?[]?,object?>                         GetElement      = null!;
		internal Func<IDataContext,IQueryExpressions,object?[]?,object?[]?,CancellationToken,Task<object?>> GetElementAsync = null!;

		#region Init

		internal readonly List<QueryInfo> Queries = new (1);

		public IReadOnlyCollection<QueryInfo> GetQueries()    => Queries;
		public bool                           IsFinalized     { get; internal set; }
		public SqlErrorExpression?            ErrorExpression { get; internal set; }

		internal abstract void Init(IBuildContext parseContext);

		protected internal Query(IDataContext dataContext)
		{
			ConfigurationID         = dataContext.ConfigurationID;
			ContextType             = dataContext.GetType();
			MappingSchema           = dataContext.MappingSchema;
			SqlOptimizer            = dataContext.GetSqlOptimizer(dataContext.Options);
			SqlProviderFlags        = dataContext.SqlProviderFlags;
			DataOptions             = dataContext.Options;
			InlineParameters        = dataContext.InlineParameters;
			IsEntityServiceProvided = dataContext is IInterceptable<IEntityServiceInterceptor> { Interceptor: {} };
		}

		#endregion

		#region Compare

		internal readonly int              ConfigurationID;
		internal readonly Type             ContextType;
		internal readonly MappingSchema    MappingSchema;
		internal readonly bool             InlineParameters;
		internal readonly ISqlOptimizer    SqlOptimizer;
		internal readonly SqlProviderFlags SqlProviderFlags;
		internal readonly DataOptions      DataOptions;
		internal readonly bool             IsEntityServiceProvided;

		protected bool Compare(IDataContext dataContext, IQueryExpressions expressions, [NotNullWhen(true)] out IQueryExpressions? matchedQueryExpressions)
		{
			matchedQueryExpressions = null;

			if (CompareInfo == null)
				return false;

			var result =
				ConfigurationID         == dataContext.ConfigurationID                                                  &&
				InlineParameters        == dataContext.InlineParameters                                                 &&
				ContextType             == dataContext.GetType()                                                        &&
				IsEntityServiceProvided == dataContext is IInterceptable<IEntityServiceInterceptor> { Interceptor: {} } &&
				CompareInfo.MainExpression.EqualsTo(expressions.MainExpression, dataContext);

			if (!result)
				return false;

			var runtimeExpressions = new RuntimeExpressionsContainer(expressions.MainExpression);
			matchedQueryExpressions = runtimeExpressions;

			if (CompareInfo.DynamicAccessors != null)
			{
				List<(int, Expression)>? testedExpressions = null;

				foreach (var da in CompareInfo.DynamicAccessors)
				{
					var current = da.AccessorFunc(dataContext, da.MappingSchema);
					result = da.Used.EqualsTo(current, dataContext);
					if (!result)
						return false;

					testedExpressions ??= new List<(int, Expression)>();
					testedExpressions.Add((da.ExpressionId, current));
				}

				if (testedExpressions != null)
				{
					foreach (var (expressionId, expression) in testedExpressions)
					{
						runtimeExpressions.AddExpression(expressionId, expression);
					}
				}
			}

			if (CompareInfo.ComparisionFunctions != null)
			{
				foreach (var (main, other) in CompareInfo.ComparisionFunctions)
				{
					var value1 = main(matchedQueryExpressions, dataContext, null);
					var value2 = other(matchedQueryExpressions, dataContext, null);
					result = (value1 == null && value2 == null) || (value1 != null && value1.Equals(value2));

					if (!result)
						return false;
				}
			}

			return true;
		}

		internal QueryCacheCompareInfo? CompareInfo;

		internal List<ParameterAccessor>? ParameterAccessors { get; set; }

		internal List<SqlParameter>? BuiltParameters;

		internal void AddParameterAccessor(ParameterAccessor accessor)
		{
			(ParameterAccessors ??= []).Add(accessor);
		}

		internal void SetParametersAccessors(List<ParameterAccessor>? parameterAccessors)
		{
			ParameterAccessors = parameterAccessors;
		}

		#endregion

		#region Cache Support

		internal static readonly ConcurrentQueue<Action> CacheCleaners = new ();

		/// <summary>
		/// Clears query caches for all typed queries.
		/// </summary>
		public static void ClearCaches()
		{
			foreach (var cleaner in CacheCleaners)
			{
				cleaner();
			}
		}

		#endregion

		#region Eager Loading

		Preamble[]? _preambles;

		internal void SetPreambles(List<Preamble>? preambles)
		{
			_preambles = preambles?.ToArray();
		}

		internal bool IsAnyPreambles()
		{
			return _preambles?.Length > 0;
		}

		internal object?[]? InitPreambles(IDataContext dc, IQueryExpressions expressions, object?[]? ps)
		{
			if (_preambles == null)
				return null;

			var preambles = new object[_preambles.Length];
			for (var i = 0; i < preambles.Length; i++)
			{
				preambles[i] = _preambles[i].Execute(dc, expressions, ps, preambles);
			}

			return preambles;
		}

		internal async Task<object?[]?> InitPreamblesAsync(IDataContext dc, IQueryExpressions expressions, object?[]? ps, CancellationToken cancellationToken)
		{
			if (_preambles == null)
				return null;

			var preambles = new object[_preambles.Length];
			for (var i = 0; i < preambles.Length; i++)
			{
				preambles[i] = await _preambles[i].ExecuteAsync(dc, expressions, ps, preambles, cancellationToken).ConfigureAwait(false);
			}

			return preambles;
		}

		#endregion

		#region Run Steps

		// Stored side-effects bound to a prepared query (not eager loading). Run before / after the
		// main query execution. Distinct from preambles because eager-load mappers bake hard-coded
		// preamble-array indices at compile time — see ExpressionBuilder.EagerLoad.cs.

		QueryRunStep[]? _runSteps;

		internal void SetRunSteps(List<QueryRunStep>? steps)
		{
			_runSteps = steps == null || steps.Count == 0 ? null : steps.ToArray();
		}

		internal bool IsAnyRunSteps() => _runSteps != null && _runSteps.Length > 0;

		internal void RunSetup(IDataContext dc, IQueryExpressions expressions, object?[]? parameters)
		{
			if (_runSteps == null)
				return;

			foreach (var step in _runSteps)
				step.Setup(dc, expressions, parameters);
		}

		internal async Task RunSetupAsync(IDataContext dc, IQueryExpressions expressions, object?[]? parameters, CancellationToken cancellationToken)
		{
			if (_runSteps == null)
				return;

			foreach (var step in _runSteps)
				await step.SetupAsync(dc, expressions, parameters, cancellationToken).ConfigureAwait(false);
		}

		internal void RunTeardown(IDataContext dc)
		{
			if (_runSteps == null)
				return;

			List<Exception>? errors = null;
			foreach (var step in _runSteps)
			{
				try
				{
					step.Teardown(dc);
				}
				catch (Exception ex)
				{
					(errors ??= new List<Exception>()).Add(ex);
				}
			}

			if (errors is { Count: > 0 })
				throw new AggregateException(errors);
		}

		internal async Task RunTeardownAsync(IDataContext dc, CancellationToken cancellationToken)
		{
			if (_runSteps == null)
				return;

			List<Exception>? errors = null;
			foreach (var step in _runSteps)
			{
				try
				{
					await step.TeardownAsync(dc, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					(errors ??= new List<Exception>()).Add(ex);
				}
			}

			if (errors is { Count: > 0 })
				throw new AggregateException(errors);
		}

		#endregion
	}
}
