using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
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
		internal Func<IDataContext,IQueryExpressions,object?[]?,object?[]?,object?>                         GetElement      = null!;
		internal Func<IDataContext,IQueryExpressions,object?[]?,object?[]?,CancellationToken,Task<object?>> GetElementAsync = null!;

		#region Init

		internal readonly List<QueryInfo> Queries = new (1);

		public IReadOnlyCollection<QueryInfo> GetQueries()    => Queries;
		public bool                           IsFinalized     { get; internal set; }

		/// <summary>
		/// Query contains a data-modifying CTE (<see cref="LinqExtensions.InsertWithOutputQuery{TTarget}(ITable{TTarget}, System.Linq.Expressions.Expression{Func{TTarget}})"/>).
		/// </summary>
		internal bool HasDataModification { get; set; }
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

			var result = CompareInfo.MainExpression.EqualsTo(expressions.MainExpression);

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
					result = da.Used.EqualsTo(current);
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
					result = ExpressionEqualityComparer.CompareValues(value1, value2);

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
			if (_preambles == null || _preambles.Length == 0)
				return false;

			for (var i = 0; i < _preambles.Length; i++)
			{
				if (!_preambles[i].IsInlined)
					return true;
			}

			return false;
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

		internal LoadTransaction? StartLoadTransaction(IDataContext dataContext)
		{
			var dc = GetTransactionOwner(dataContext);

			if (dc == null)
				return null;

			if (dataContext is DataContext ctx)
				return new LoadTransaction(ctx.BeginTransaction(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel), HasDataModification);

			return new LoadTransaction(dc.BeginTransaction(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel), HasDataModification);
		}

		internal async Task<LoadTransaction?> StartLoadTransactionAsync(IDataContext dataContext, CancellationToken cancellationToken)
		{
			var dc = GetTransactionOwner(dataContext);

			if (dc == null)
				return null;

			if (dataContext is DataContext ctx)
				return new LoadTransaction(
					await ctx.BeginTransactionAsync(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel, cancellationToken)
						.ConfigureAwait(false),
					HasDataModification);

			return new LoadTransaction(
				await dc.BeginTransactionAsync(dc.DataProvider.SqlProviderFlags.DefaultMultiQueryIsolationLevel, cancellationToken)
					.ConfigureAwait(false),
				HasDataModification);
		}

		/// <summary>
		/// The implicit transaction around the preambles of an eager-loading query. Disposing it without
		/// <see cref="Complete"/> rolls it back. A data-modifying query commits it once the preambles ran,
		/// so the insert and the child queries persist together.
		/// </summary>
		internal sealed class LoadTransaction : IDisposable, IAsyncDisposable
		{
			readonly object _transaction;
			readonly bool   _commit;

			// Once committed, the connection's current transaction may be one the user started while
			// enumerating, and disposing ours would dispose it.
			bool _committed;

			public LoadTransaction(DataContextTransaction transaction, bool commit)
			{
				_transaction = transaction;
				_commit      = commit;
			}

			public LoadTransaction(DataConnectionTransaction transaction, bool commit)
			{
				_transaction = transaction;
				_commit      = commit;
			}

			public void Complete()
			{
				if (!_commit)
					return;

				if (_transaction is DataContextTransaction contextTransaction)
					contextTransaction.CommitTransaction();
				else
					((DataConnectionTransaction)_transaction).Commit();

				_committed = true;
			}

			public async Task CompleteAsync(CancellationToken cancellationToken)
			{
				if (!_commit)
					return;

				if (_transaction is DataContextTransaction contextTransaction)
					await contextTransaction.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
				else
					await ((DataConnectionTransaction)_transaction).CommitAsync(cancellationToken).ConfigureAwait(false);

				_committed = true;
			}

			public void Dispose()
			{
				if (!_committed)
					((IDisposable)_transaction).Dispose();
			}

			public ValueTask DisposeAsync()
			{
				return _committed ? default : ((IAsyncDisposable)_transaction).DisposeAsync();
			}
		}

		DataConnection? GetTransactionOwner(IDataContext dataContext)
		{
			// Do not start implicit transaction if there is no preambles
			//
			if (!IsAnyPreambles())
				return null;

			var dc = dataContext switch
			{
				DataConnection dataConnection => dataConnection,
				DataContext    context        => context.GetDataConnection(),
				_                             => null,
			};

			if (dc == null)
				return null;

			// transaction will be maintained by TransactionScope
			//
			if (TransactionScopeHelper.IsInsideTransactionScope)
				return null;

			if (dc.TransactionAsync != null || dc.CurrentCommand?.Transaction != null)
				return null;

			return dc;
		}

		#endregion
	}
}
