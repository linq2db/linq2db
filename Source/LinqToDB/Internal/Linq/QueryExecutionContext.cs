using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Per-execute shared context for one <c>ExpressionQuery</c> execution call. Created at the
	/// top of each execute path and threaded through <c>InitQueries</c> (temp-table run-step
	/// Setup, runs before <c>StartLoadTransaction</c>), <c>InitPreambles</c> (eager-loading,
	/// runs inside the transaction), the main query's <c>GetElement</c> /
	/// <c>GetResultEnumerable</c>, and every <see cref="QueryRunStep"/> that fires during the
	/// call. Carries:
	/// <list type="bullet">
	///   <item>The live set of temp-table run-steps so multiple runners share one Setup/Teardown
	///   cycle (idempotent via dedup on <see cref="QueryRunStep.TempTableName"/>).</item>
	///   <item>The per-<see cref="SqlValuesTable.TempTableName"/> decision a run-step's Setup made
	///   at execute time (use the temp table vs. fall back to inline VALUES), so the SQL builder
	///   can read it during emission without re-materializing the source.</item>
	/// </list>
	/// </summary>
	sealed class QueryExecutionContext : IDisposable, IAsyncDisposable
	{
		Dictionary<string, QueryRunStep>?           _activeSteps;
		Dictionary<string, TempTableDecision>?      _tempTableDecisions;
		bool                                        _disposed;

		// Teardown needs an IDataContext but neither Dispose nor DisposeAsync take one. We capture
		// the context the FIRST setup ran against; all later setups in the same execute call see
		// the same DataContext (a single execute is single-DC by construction).
		IDataContext? _teardownContext;

		/// <summary>
		/// Per-temp-table decision that the run-step's Setup made at execute time. The SQL builder
		/// reads this when emitting <see cref="SqlValuesTable"/>: if the entry exists and is
		/// <see cref="TempTableDecisionKind.UseTempTable"/>, emit a temp-table reference; otherwise
		/// the SQL builder iterates <see cref="TempTableDecision.SourceItems"/> with the
		/// <see cref="SqlValuesTable.ValueBuilders"/> to emit an inline VALUES clause.
		/// </summary>
		internal enum TempTableDecisionKind
		{
			UseTempTable,
			UseInlineValues,
		}

		/// <summary>
		/// <para><see cref="SourceItems"/> is the raw materialized source — kept as the user's
		/// original <see cref="ICollection"/> when it already was one (no copy), or a
		/// freshly-materialized <see cref="List{T}"/> otherwise. The SQL builder iterates this and
		/// applies the corresponding <see cref="SqlValuesTable.ValueBuilders"/> only when it
		/// actually needs to emit VALUES rows.</para>
		/// <para><see langword="null"/> when <see cref="Kind"/> is
		/// <see cref="TempTableDecisionKind.UseTempTable"/> — the items were handed to the
		/// <see cref="TempTable{T}"/> constructor in Setup and the SQL builder only emits the
		/// table reference.</para>
		/// </summary>
		internal readonly record struct TempTableDecision(TempTableDecisionKind Kind, ICollection? SourceItems);

		internal void RecordTempTableDecision(string tempTableName, TempTableDecision decision)
		{
			ObjectDisposedException.ThrowIf(_disposed, this);
			(_tempTableDecisions ??= new())[tempTableName] = decision;
		}

		internal bool TryGetTempTableDecision(string tempTableName, out TempTableDecision decision)
		{
			if (_tempTableDecisions != null && _tempTableDecisions.TryGetValue(tempTableName, out decision))
				return true;

			decision = default;
			return false;
		}

		/// <summary>
		/// Idempotent setup: if no other runner has fired Setup for this step's
		/// <see cref="QueryRunStep.TempTableName"/> yet, runs <see cref="QueryRunStep.Setup"/>
		/// and records the step for teardown. Subsequent calls with the same name (e.g. the
		/// main runner after the preamble runner already created the table) are no-ops.
		/// Steps without a temp-table name are run unconditionally (no dedup key).
		/// </summary>
		public void EnsureSetup(QueryRunStep step, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters)
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			_teardownContext ??= dataContext;

			var name = step.TempTableName;
			if (name != null)
			{
				_activeSteps ??= new();
				if (!_activeSteps.TryAdd(name, step))
					return;
			}

			step.Setup(dataContext, expressions, parameters, this);
		}

		public async Task EnsureSetupAsync(QueryRunStep step, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, CancellationToken cancellationToken)
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			_teardownContext ??= dataContext;

			var name = step.TempTableName;
			if (name != null)
			{
				_activeSteps ??= new();
				if (!_activeSteps.TryAdd(name, step))
					return;
			}

			await step.SetupAsync(dataContext, expressions, parameters, this, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>Tears down every step registered through <see cref="EnsureSetup"/> in this scope.</summary>
		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			if (_activeSteps == null)
				return;

			List<Exception>? errors = null;
			foreach (var step in _activeSteps.Values)
			{
				try
				{
					step.Teardown(_teardownContext!);
				}
				catch (Exception ex)
				{
					(errors ??= new()).Add(ex);
				}
			}

			_activeSteps = null;

			if (errors is { Count: > 0 })
				throw new AggregateException(errors);
		}

		public async ValueTask DisposeAsync()
		{
			if (_disposed)
				return;

			_disposed = true;

			if (_activeSteps == null)
				return;

			List<Exception>? errors = null;
			foreach (var step in _activeSteps.Values)
			{
				try
				{
					await step.TeardownAsync(_teardownContext!, CancellationToken.None).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					(errors ??= new()).Add(ex);
				}
			}

			_activeSteps = null;

			if (errors is { Count: > 0 })
				throw new AggregateException(errors);
		}
	}
}
