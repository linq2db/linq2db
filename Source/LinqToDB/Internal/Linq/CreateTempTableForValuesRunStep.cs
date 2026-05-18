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
	/// Run step that materializes a temporary table for a <c>SqlValuesTable</c> whose
	/// <c>UseTempTable</c> threshold was crossed at SQL-emit time. The temp table is created
	/// at execute time with the name baked into the cached SQL, populated via BulkCopy, then
	/// dropped on teardown unless <c>DisposeWithConnection</c> routes lifetime to the data
	/// context.
	/// <para>
	/// Source items are NOT captured at build time — they are re-evaluated from the per-execution
	/// parameter values via <see cref="SqlValuesTable.Source"/>. This keeps the run step
	/// cache-friendly: the same compiled <c>Query&lt;T&gt;</c> can be reused across executions
	/// with different <c>IEnumerable</c> sources of the same shape.
	/// </para>
	/// </summary>
	/// <typeparam name="T">Wrapped element type of the temp table — equals the user's element type
	/// for entity sources, or <see cref="ValueHolder{TInner}"/> for scalar sources.</typeparam>
	sealed class CreateTempTableForValuesRunStep<T> : QueryRunStep
		where T : notnull
	{
		readonly Query          _query;
		readonly SqlValuesTable _sqlValuesTable;
		readonly string         _tableName;
		readonly bool           _disposeWithConnection;
		readonly bool           _wrapScalarInValueHolder;
		readonly PropertyInfo?  _valueHolderValueProp;
		TempTable<T>?           _tempTable;
		bool                    _ownedByTracker;

		public CreateTempTableForValuesRunStep(
			Query          query,
			SqlValuesTable sqlValuesTable,
			string         tableName,
			bool           disposeWithConnection,
			bool           wrapScalarInValueHolder)
		{
			_query                   = query;
			_sqlValuesTable          = sqlValuesTable;
			_tableName               = tableName;
			_disposeWithConnection   = disposeWithConnection;
			_wrapScalarInValueHolder = wrapScalarInValueHolder;
			_valueHolderValueProp    = wrapScalarInValueHolder
				? typeof(T).GetProperty(nameof(ValueHolder<>.Value))
				: null;
		}

		public override void Setup(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters)
		{
			if (_tempTable != null)
				return;

			var items = MaterializeCurrentItems(dataContext, expressions, parameters);

			_tempTable = new TempTable<T>(
				dataContext,
				_tableName,
				items,
				tableOptions: TableOptions.IsTemporary);

			if (_disposeWithConnection && dataContext is IInfrastructure<IDisposableTracker>)
				_ownedByTracker = true;
		}

		public override async Task SetupAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, CancellationToken cancellationToken)
		{
			if (_tempTable != null)
				return;

			var items = MaterializeCurrentItems(dataContext, expressions, parameters);

			_tempTable = await TempTable<T>.CreateAsync(
				dataContext,
				_tableName,
				items,
				tableOptions     : TableOptions.IsTemporary,
				cancellationToken: cancellationToken).ConfigureAwait(false);

			if (_disposeWithConnection && dataContext is IInfrastructure<IDisposableTracker>)
				_ownedByTracker = true;
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

		List<T> MaterializeCurrentItems(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters)
		{
			// Build per-execution parameter values and evaluate the SqlValuesTable's Source to get
			// the current IEnumerable. Mirrors what BasicSqlBuilder.BuildSqlValuesTable does for
			// inline VALUES — keeps the run step parametric so cache hits with different inputs work.
			var paramValues = new SqlParameterValues();
			QueryRunner.SetParameters(_query, expressions, dataContext, parameters, paramValues);
			var context = new EvaluationContext(paramValues);

			if (_sqlValuesTable.Source?.EvaluateExpression(context) is not IEnumerable seq)
				throw new LinqToDBException("AsQueryable UseTempTable: source did not evaluate to an IEnumerable.");

			var list = new List<T>();

			if (_wrapScalarInValueHolder)
			{
				var valueProp = _valueHolderValueProp!;
				foreach (var rawItem in seq)
				{
					var holder = (T)Activator.CreateInstance(typeof(T))!;
					valueProp.SetValue(holder, rawItem);
					list.Add(holder);
				}
			}
			else
			{
				foreach (T item in seq)
					list.Add(item);
			}

			return list;
		}
	}
}
