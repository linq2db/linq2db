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
	/// <c>UseTempTable</c> threshold was crossed at SQL-emission time
	/// (see <c>BasicSqlBuilder.BuildSqlValuesTable</c>). Created and registered by the SQL
	/// builder; <c>EnumerableBuilder</c> only stamps the threshold metadata.
	/// <para>
	/// Source items are re-evaluated from the <see cref="SqlValuesTable.Source"/> at execute
	/// time using the per-execution parameter values. Cache-friendly: the same compiled
	/// <c>Query&lt;T&gt;</c> serves executions with different <c>IEnumerable</c> values of the
	/// same shape.
	/// </para>
	/// </summary>
	/// <typeparam name="T">Wrapped element type of the temp table — equals the user's element type
	/// for entity sources, or <see cref="ValueHolder{TInner}"/> for scalar sources.</typeparam>
	sealed class CreateTempTableForValuesRunStep<T> : QueryRunStep
		where T : notnull
	{
		readonly Query          _ownerQuery;
		readonly SqlValuesTable _sqlValuesTable;
		readonly bool           _wrapScalarInValueHolder;
		readonly PropertyInfo?  _valueHolderValueProp;
		TempTable<T>?           _tempTable;
		bool                    _ownedByTracker;

		public CreateTempTableForValuesRunStep(Query ownerQuery, SqlValuesTable sqlValuesTable, bool wrapScalarInValueHolder)
		{
			_ownerQuery              = ownerQuery;
			_sqlValuesTable          = sqlValuesTable;
			_wrapScalarInValueHolder = wrapScalarInValueHolder;
			_valueHolderValueProp    = wrapScalarInValueHolder
				? typeof(T).GetProperty(nameof(ValueHolder<>.Value))
				: null;
		}

		string TableName              => _sqlValuesTable.TempTableName ?? throw new InvalidOperationException("TempTableName must be set before Setup runs.");
		bool   DisposeWithConnection  => _sqlValuesTable.TempTableDisposeWithConnection;

		public override void Setup(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters)
		{
			if (_tempTable != null)
				return;

			var items = MaterializeCurrentItems(dataContext, expressions, parameters);

			_tempTable = new TempTable<T>(
				dataContext,
				TableName,
				items,
				tableOptions: TableOptions.IsTemporary);

			if (DisposeWithConnection && dataContext is IInfrastructure<IDisposableTracker>)
				_ownedByTracker = true;
		}

		public override async Task SetupAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, CancellationToken cancellationToken)
		{
			if (_tempTable != null)
				return;

			var items = MaterializeCurrentItems(dataContext, expressions, parameters);

			_tempTable = await TempTable<T>.CreateAsync(
				dataContext,
				TableName,
				items,
				tableOptions     : TableOptions.IsTemporary,
				cancellationToken: cancellationToken).ConfigureAwait(false);

			if (DisposeWithConnection && dataContext is IInfrastructure<IDisposableTracker>)
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
			// Mirror QueryRunnerBase.SetCommand: build SqlParameterValues from the owner Query's
			// ParameterAccessors + current parameters, then evaluate Source.
			var paramValues = new SqlParameterValues();
			QueryRunner.SetParameters(_ownerQuery, expressions, dataContext, parameters, paramValues);
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
