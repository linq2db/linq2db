using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Run step that materializes a temporary table for a <c>SqlValuesTable</c> whose row count
	/// at query-build time exceeded the user-specified <c>UseTempTable</c> threshold. The temp
	/// table is created with the name baked into the cached SQL, populated via BulkCopy, then
	/// dropped on teardown unless <c>DisposeWithConnection</c> routes lifetime to the data context.
	/// </summary>
	/// <typeparam name="T">Element type of the source enumerable.</typeparam>
	sealed class CreateTempTableForValuesRunStep<T> : QueryRunStep
		where T : notnull
	{
		readonly List<T> _items;
		readonly string  _tableName;
		readonly bool    _disposeWithConnection;
		TempTable<T>?    _tempTable;
		bool             _ownedByTracker;

		public CreateTempTableForValuesRunStep(List<T> items, string tableName, bool disposeWithConnection)
		{
			_items                 = items;
			_tableName             = tableName;
			_disposeWithConnection = disposeWithConnection;
		}

		public override void Setup(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters)
		{
			if (_tempTable != null)
				return;

			_tempTable = new TempTable<T>(
				dataContext,
				_tableName,
				_items,
				tableOptions: TableOptions.IsTemporary);

			if (_disposeWithConnection && dataContext is IDataContextDisposableTracker tracker)
			{
				tracker.Register(_tempTable);
				_ownedByTracker = true;
			}
		}

		public override async Task SetupAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, CancellationToken cancellationToken)
		{
			if (_tempTable != null)
				return;

			_tempTable = await TempTable<T>.CreateAsync(
				dataContext,
				_tableName,
				_items,
				tableOptions     : TableOptions.IsTemporary,
				cancellationToken: cancellationToken).ConfigureAwait(false);

			if (_disposeWithConnection && dataContext is IDataContextDisposableTracker tracker)
			{
				tracker.Register(_tempTable);
				_ownedByTracker = true;
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
	}
}
