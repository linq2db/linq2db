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
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Mapping;

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Internal.Linq
{
	public abstract class Query
	{
		internal Func<IDataContext,IQueryExpressions,object?[]?,object?[]?,QueryExecutionContext?,object?>                         GetElement      = null!;
		internal Func<IDataContext,IQueryExpressions,object?[]?,object?[]?,QueryExecutionContext?,CancellationToken,Task<object?>> GetElementAsync = null!;

		#region Init

		internal readonly List<QueryInfo> Queries = new (1);

		public IReadOnlyCollection<QueryInfo> GetQueries()    => Queries;
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

		Preamble[]? _preambles;

		internal void SetPreambles(List<Preamble>? preambles)
		{
			_preambles = preambles?.ToArray();
		}

		internal bool IsAnyPreambles()
		{
			return _preambles?.Length > 0;
		}

		internal object?[]? InitPreambles(IDataContext dc, IQueryExpressions expressions, object?[]? ps, QueryExecutionContext? execContext)
		{
			if (_preambles == null)
				return null;

			var preambles = new object[_preambles.Length];
			for (var i = 0; i < preambles.Length; i++)
			{
				preambles[i] = _preambles[i].Execute(dc, expressions, ps, preambles, execContext);
			}

			return preambles;
		}

		internal async Task<object?[]?> InitPreamblesAsync(IDataContext dc, IQueryExpressions expressions, object?[]? ps, QueryExecutionContext? execContext, CancellationToken cancellationToken)
		{
			if (_preambles == null)
				return null;

			var preambles = new object[_preambles.Length];
			for (var i = 0; i < preambles.Length; i++)
			{
				preambles[i] = await _preambles[i].ExecuteAsync(dc, expressions, ps, preambles, execContext, cancellationToken).ConfigureAwait(false);
			}

			return preambles;
		}

		#endregion

		#region Init Queries

		// Side-effecting steps that must run BEFORE the implicit transaction (StartLoadTransaction)
		// — typically creating temp tables for AsQueryable.UseTempTable. Derived lazily on first
		// execute from the AST (every SqlValuesTable carrying TempTableSpec metadata
		// becomes one run-step, deduped by TempTableName so self-join siblings share one
		// CREATE/INSERT/DROP cycle). The result is cached on this Query instance — subsequent
		// executes reuse the same run-step instances. Distinct from preambles, which run INSIDE
		// the transaction (preambles need read-consistency with the main query; init queries
		// need to be visible to the main query and can't be transactional themselves — temp
		// tables created inside transactions get dropped on commit in some providers, notably
		// SQLite).
		QueryRunStep[]? _initQueries;
		// Set by EnsureInitQueriesCompiled AFTER _initQueries has been published. Read with
		// Volatile to pair with the Volatile.Write in the compile path — any thread that sees
		// _initQueriesCompiled == true is guaranteed to also see the populated _initQueries
		// (or null if the AST scan found no temp-table candidates), with no risk of skipping
		// Setup for temp-table run-steps that another thread is still constructing.
		volatile bool _initQueriesCompiled;
		readonly Lock _initQueriesLock = new();

		internal void InitQueries(IDataContext dc, IQueryExpressions expressions, object?[]? parameters, QueryExecutionContext execContext)
		{
			EnsureInitQueriesCompiled();

			if (_initQueries == null)
				return;

			foreach (var step in _initQueries)
				execContext.EnsureSetup(step, dc, expressions, parameters);
		}

		internal async Task InitQueriesAsync(IDataContext dc, IQueryExpressions expressions, object?[]? parameters, QueryExecutionContext execContext, CancellationToken cancellationToken)
		{
			EnsureInitQueriesCompiled();

			if (_initQueries == null)
				return;

			foreach (var step in _initQueries)
				await execContext.EnsureSetupAsync(step, dc, expressions, parameters, cancellationToken).ConfigureAwait(false);
		}

		void EnsureInitQueriesCompiled()
		{
			// Double-checked locking: outer fast-path is a volatile read; inner re-check inside
			// the lock handles the race-to-the-lock case. Publish _initQueries BEFORE flipping
			// _initQueriesCompiled — concurrent reads must never see compiled=true with
			// _initQueries still null (Query<T> instances are shared across threads via the
			// static Query{T}._queryCache, so first-call races are routine, not edge-case).
			if (_initQueriesCompiled)
				return;

			lock (_initQueriesLock)
			{
				if (_initQueriesCompiled)
					return;

				if (Queries.Count > 0)
				{
					// Walk the AST once for SqlValuesTables carrying UseTempTable metadata.
					// Self-join siblings share a TempTableName via
					// ExpressionBuilder.GetOrAssignTempTableName, so dedup-by-name collapses
					// them to one run-step.
					Dictionary<string, SqlValuesTable>? candidates = null;
					var visitor = new SqlQueryActionVisitor();
					try
					{
						visitor.Visit(Queries[0].Statement, visitAll: false, element =>
						{
							if (element is SqlValuesTable { TempTableSpec.Threshold: not null, TempTableName: { } name } vt)
							{
								candidates ??= new();
								candidates.TryAdd(name, vt);
							}
						});
					}
					finally
					{
						visitor.Cleanup();
					}

					if (candidates != null)
					{
						var steps = new QueryRunStep[candidates.Count];
						var i     = 0;
						foreach (var kvp in candidates)
							steps[i++] = CreateTempTableForValuesRunStepFactory.Create(this, kvp.Value, kvp.Key, MappingSchema);

						// Publish the run-step array BEFORE flipping the compiled flag below.
						_initQueries = steps;
					}
				}

				_initQueriesCompiled = true;
			}
		}

		#endregion
	}
}
