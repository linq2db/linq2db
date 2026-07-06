using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Expressions.ExpressionVisitors;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Internal.Linq
{
	public abstract class Query
	{
		internal Func<IDataContext,IQueryExpressions,SqlCommandExecutionContext?,object?>                         GetElement      = null!;
		internal Func<IDataContext,IQueryExpressions,SqlCommandExecutionContext?,CancellationToken,Task<object?>> GetElementAsync = null!;

		// Eager-aware element selection (First/Single/…), set only by FirstSingleBuilder. Unlike GetElement, these route
		// through the combined eager engine (Query<T>.GetEagerEnumerable — one multi-result-set command when eligible,
		// else the sequential harvesters + base enumerable) and take the single element, so First/Single batch their
		// eager children the same way ToList()/foreach do. When set, the element execution paths use these INSTEAD of
		// GetElement and skip the InitHarvesters pre-init (GetEagerEnumerable runs the harvesters itself). Null for
		// scalar / ExecuteElement / non-query queries, which keep the pre-inited-harvesters GetElement path.
		internal Func<IDataContext,IQueryExpressions,object?[]?,object?>?                         GetEagerElement;
		internal Func<IDataContext,IQueryExpressions,object?[]?,CancellationToken,Task<object?>>? GetEagerElementAsync;

		#region Init

		// One logical query per Query. Multi-statement expansion is modelled by SqlCommandScenario at render time
		// (DataConnection.QueryRunner.GetCommand), not by a list here. Set in Init.
		internal QueryInfo QueryInfo = null!;

		public IReadOnlyCollection<QueryInfo> GetQueries()    => new[] { QueryInfo };
		public bool                           IsFinalized     { get; internal set; }
		public SqlErrorExpression?            ErrorExpression { get; internal set; }

		internal abstract void Init(IBuildContext parseContext);

		protected internal Query(IDataContext dataContext)
		{
			MappingSchema    = dataContext.MappingSchema;
			SqlOptimizer     = dataContext.GetSqlOptimizer(dataContext.Options);
			SqlProviderFlags = dataContext.SqlProviderFlags;
			DataOptions      = dataContext.Options;
		}

		#endregion

		#region Compare

		// ConfigurationID, ContextType, InlineParameters and IsEntityServiceProvided are pinned
		// by the QueryCache bucket key (ContextType + ConfigurationID + QueryFlags — the latter
		// encodes InlineParameters and HasEntityServiceInterceptor), so every entry inside a
		// bucket already matches them by construction. Compare only re-checks the expression.
		internal readonly MappingSchema    MappingSchema;
		internal readonly ISqlOptimizer    SqlOptimizer;
		internal readonly SqlProviderFlags SqlProviderFlags;
		internal readonly DataOptions      DataOptions;

		protected internal bool Compare(IDataContext dataContext, IQueryExpressions expressions, [NotNullWhen(true)] out IQueryExpressions? matchedQueryExpressions)
		{
			matchedQueryExpressions = null;

			if (CompareInfo == null)
				return false;

			var result = CompareInfo.MainExpression.EqualsTo(expressions.MainExpression, dataContext);

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

		Harvester[]? _harvesters;

		internal void SetHarvesters(List<Harvester>? harvesters)
		{
			_harvesters = harvesters?.ToArray();
		}

		internal bool IsAnyHarvesters()
		{
			if (_harvesters == null || _harvesters.Length == 0)
				return false;

			for (var i = 0; i < _harvesters.Length; i++)
			{
				if (!_harvesters[i].IsInlined)
					return true;
			}

			return false;
		}

		// Exposes the harvester array to QueryRunner.TryGetCombinedEagerEnumerable so the main query and its combinable
		// eager-load children can be assembled into one multi-result-set command.
		internal Harvester[]? HarvestersArray => _harvesters;

		internal SqlCommandExecutionContext? InitHarvesters(IDataContext dc, IQueryExpressions expressions, object?[]? ps)
		{
			// A query with no harvesters still needs a context when it carries compiled-query parameters (ps != null), so the
			// mapper/parameter accessors can read them from the context. Regular queries pass ps == null here and stay context-free.
			if (_harvesters == null)
				return ps is null ? null : new SqlCommandExecutionContext(0, ps);

			var context = new SqlCommandExecutionContext(_harvesters.Length, ps);
			for (var i = 0; i < _harvesters.Length; i++)
			{
				context.SetResult(i, _harvesters[i].Execute(dc, expressions, context));
			}

			return context;
		}

		internal async Task<SqlCommandExecutionContext?> InitHarvestersAsync(IDataContext dc, IQueryExpressions expressions, object?[]? ps, CancellationToken cancellationToken)
		{
			if (_harvesters == null)
				return ps is null ? null : new SqlCommandExecutionContext(0, ps);

			var context = new SqlCommandExecutionContext(_harvesters.Length, ps);
			for (var i = 0; i < _harvesters.Length; i++)
			{
				context.SetResult(i, await _harvesters[i].ExecuteAsync(dc, expressions, context, cancellationToken).ConfigureAwait(false));
			}

			return context;
		}

		#endregion
	}
}
