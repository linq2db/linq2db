using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Run-step that decides at execute time, based on the actual materialized row count, whether
	/// to create a temporary table for a <see cref="SqlValuesTable"/> (above its
	/// <see cref="SqlValuesTable.TempTableSpec"/> threshold) or fall back to inline VALUES (below).
	/// Registered at AST build time by <c>EnumerableBuilder.BuildConfigured</c> and run by
	/// <c>Query.InitQueries</c> before <c>StartLoadTransaction</c> — temp tables created inside
	/// the implicit transaction get dropped on commit in some providers (SQLite +
	/// System.Data.SQLite), so Setup must precede the transaction.
	/// <para>
	/// Under the default lifetime, the temp table is created on each execute and dropped in
	/// Teardown — source items are re-evaluated from <see cref="SqlValuesTable.Source"/> per
	/// execute using the current parameter values, so the same cached <c>Query&lt;T&gt;</c>
	/// serves executions with different <c>IEnumerable</c> values of the same shape.
	/// </para>
	/// <para>
	/// When <see cref="LinqToDB.TempTableSpec.DisposeWithConnection"/> is set, the temp table
	/// is created once on first execute and reused across subsequent executes of the same
	/// cached <c>Query&lt;T&gt;</c>; the table's lifetime is tied to the surrounding data
	/// context, not the single execute. Subsequent executes still record a
	/// <c>UseTempTable</c> decision on the new <see cref="QueryExecutionContext"/> (so the
	/// SQL builder emits a temp-table reference rather than re-running the threshold check
	/// against the live parameter values), but they do not re-read the source — the
	/// first-execute data stays in the table for the rest of the data context's lifetime.
	/// </para>
	/// <para>
	/// Setup records the decision on the per-execute <see cref="QueryExecutionContext"/>;
	/// the SQL builder reads it during emission and either emits a temp-table reference or
	/// the inline VALUES clause built from the rows captured here.
	/// </para>
	/// </summary>
	/// <typeparam name="T">Element type of the temp table — equals the user's element type
	/// for entity sources (AsQueryable, entity / composite-PK Contains), or
	/// <see cref="ValueHolder{TInner}"/> for scalar sources.</typeparam>
	sealed class CreateTempTableForValuesRunStep<T> : QueryRunStep
		where T : notnull
	{
		readonly Query             _ownerQuery;
		readonly SqlValuesTable    _sqlValuesTable;
		readonly string            _tableName;
		readonly TempTableSpec     _spec;
		readonly int               _threshold;
		readonly bool              _wrapScalarInValueHolder;
		readonly PropertyInfo?     _valueHolderValueProp;
		TempTable<T>?              _tempTable;
		bool                       _ownedByTracker;

		public CreateTempTableForValuesRunStep(Query ownerQuery, SqlValuesTable sqlValuesTable, string tableName, bool wrapScalarInValueHolder)
		{
			_ownerQuery              = ownerQuery;
			_sqlValuesTable          = sqlValuesTable;
			_tableName               = tableName;
			_spec                    = sqlValuesTable.TempTableSpec
				?? throw new InvalidOperationException("CreateTempTableForValuesRunStep requires SqlValuesTable.TempTableSpec to be set.");
			_threshold               = _spec.Threshold
				?? throw new InvalidOperationException("CreateTempTableForValuesRunStep requires SqlValuesTable.TempTableSpec.Threshold to be set.");
			_wrapScalarInValueHolder = wrapScalarInValueHolder;
			_valueHolderValueProp    = wrapScalarInValueHolder
				? typeof(T).GetProperty(nameof(ValueHolder<>.Value))
				: null;
		}

		public override string TempTableName => _tableName;

		bool DisposeWithConnection => _spec.DisposeWithConnection;

		public override void Setup(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, QueryExecutionContext executionContext)
		{
			if (_tempTable != null)
			{
				// Subsequent execute under DisposeWithConnection — the temp table already
				// exists on the connection from a prior execute and remains populated. Record
				// the UseTempTable decision so the SQL builder emits a temp-table reference
				// (rather than re-running the threshold check against the current parameter
				// values, which could pick the inline-VALUES branch and skip the populated
				// temp table entirely).
				executionContext.RecordTempTableDecision(
					_tableName,
					new QueryExecutionContext.TempTableDecision(QueryExecutionContext.TempTableDecisionKind.UseTempTable, null));
				return;
			}

			var source = ResolveSourceAsCollection(dataContext, expressions, parameters);

			if (source.Count > _threshold)
			{
				_tempTable = new TempTable<T>(
					dataContext,
					_tableName,
					ToTypedEnumerable(source),
					options     : _spec.BulkCopyOptions,
					tableOptions: TableOptions.IsTemporary);

				if (DisposeWithConnection && dataContext is IInfrastructure<IDisposableTracker>)
					_ownedByTracker = true;

				executionContext.RecordTempTableDecision(
					_tableName,
					new QueryExecutionContext.TempTableDecision(QueryExecutionContext.TempTableDecisionKind.UseTempTable, null));
			}
			else
			{
				executionContext.RecordTempTableDecision(
					_tableName,
					new QueryExecutionContext.TempTableDecision(QueryExecutionContext.TempTableDecisionKind.UseInlineValues, source));
			}
		}

		public override async Task SetupAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, QueryExecutionContext executionContext, CancellationToken cancellationToken)
		{
			if (_tempTable != null)
			{
				// See sync Setup for rationale — record the UseTempTable decision so the SQL
				// builder emits a temp-table reference against the prior-execute's populated
				// temp table, rather than re-deciding from the live parameter values.
				executionContext.RecordTempTableDecision(
					_tableName,
					new QueryExecutionContext.TempTableDecision(QueryExecutionContext.TempTableDecisionKind.UseTempTable, null));
				return;
			}

			var source = ResolveSourceAsCollection(dataContext, expressions, parameters);

			if (source.Count > _threshold)
			{
				_tempTable = await TempTable<T>.CreateAsync(
					dataContext,
					_tableName,
					ToTypedEnumerable(source),
					options          : _spec.BulkCopyOptions,
					tableOptions     : TableOptions.IsTemporary,
					cancellationToken: cancellationToken).ConfigureAwait(false);

				if (DisposeWithConnection && dataContext is IInfrastructure<IDisposableTracker>)
					_ownedByTracker = true;

				executionContext.RecordTempTableDecision(
					_tableName,
					new QueryExecutionContext.TempTableDecision(QueryExecutionContext.TempTableDecisionKind.UseTempTable, null));
			}
			else
			{
				executionContext.RecordTempTableDecision(
					_tableName,
					new QueryExecutionContext.TempTableDecision(QueryExecutionContext.TempTableDecisionKind.UseInlineValues, source));
			}
		}

		public override void Teardown(IDataContext dataContext)
		{
			if (_ownedByTracker)
				return;

			var tt = _tempTable;
			_tempTable = null;
			tt?.Dispose();
		}

		public override async Task TeardownAsync(IDataContext dataContext, CancellationToken cancellationToken)
		{
			if (_ownedByTracker)
				return;

			var tt = _tempTable;
			_tempTable = null;
			if (tt != null)
				await tt.DisposeAsync().ConfigureAwait(false);
		}

		// Evaluates the source IEnumerable with the current parameter values. If the result is
		// already an ICollection (the common case — user passed a List/Array/etc.), returns it
		// directly without copying. Otherwise materializes once into a List<object?> so we get a
		// stable Count for the threshold check and a replayable sequence for both consumers
		// (TempTable BulkCopy and inline-VALUES emission).
		ICollection ResolveSourceAsCollection(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters)
		{
			// Mirror QueryRunnerBase.SetCommand: build SqlParameterValues from the owner Query's
			// ParameterAccessors + current parameters, then evaluate Source.
			var paramValues = new SqlParameterValues();
			QueryRunner.SetParameters(_ownerQuery, expressions, dataContext, parameters, paramValues);
			var context = new EvaluationContext(paramValues);

			if (_sqlValuesTable.Source?.EvaluateExpression(context) is not IEnumerable seq)
				throw new LinqToDBException("AsQueryable UseTempTable: source did not evaluate to an IEnumerable.");

			if (seq is ICollection coll)
				return coll;

			// Non-collection source (yielding IEnumerable, LINQ chain, etc.) — materialize once.
			var list = new List<object?>();
			foreach (var item in seq)
				list.Add(item);

			return list;
		}

		// Lazy adapter for TempTable<T> BulkCopy: yields typed items as the bulk-copy iterates
		// (wrapping scalars in ValueHolder<T> when needed; entity instances pass through).
		// No upfront List<T> allocation.
		IEnumerable<T> ToTypedEnumerable(ICollection source)
		{
			if (_wrapScalarInValueHolder)
			{
				// Scalar source: skip null items. Storing NULL rows in the temp table breaks
				// NOT IN semantics (`col NOT IN (NULL, ...)` is always NULL) and is redundant
				// for IN with LikeClr semantics — the SqlExpressionConvertVisitor adds
				// `OR col IS NULL` / `AND col IS NOT NULL` separately when the lookup carries
				// null. Mirrors SqlPredicate.InList's flat-IN emission which also skips null
				// values from the value list.
				var valueProp = _valueHolderValueProp!;
				foreach (var rawItem in source)
				{
					if (rawItem == null)
						continue;

					var holder = ActivatorExt.CreateInstance<T>();
					valueProp.SetValue(holder, rawItem);
					yield return holder;
				}
			}
			else
			{
				// Entity-mode source: items go straight to BulkCopy via the entity / anonymous
				// type's MappingSchema column accessors. The user's column conversions
				// (ValueConverter, DataType overrides) propagate naturally because the temp
				// table inherits the same EntityDescriptor.
				//
				// Null items are skipped — for the Contains case, a null entry in the lookup
				// collection compares as NULL under SQL equality, so it would never match an
				// outer row anyway (matches the OR-AND chain's behavior). For the AsQueryable
				// entity case, a null record would otherwise produce an all-NULL row in the
				// temp table which is almost never what the caller intended; skipping is the
				// lenient choice that also keeps both modes consistent with the scalar branch
				// above.
				foreach (T item in source)
				{
					if (item == null)
						continue;

					yield return item;
				}
			}
		}
	}
}
