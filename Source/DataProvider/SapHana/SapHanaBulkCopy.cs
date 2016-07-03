using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace LinqToDB.DataProvider.SapHana
{
	using System.Linq.Expressions;

	using Data;

	class SapHanaBulkCopy : BasicBulkCopy
	{
		public SapHanaBulkCopy(SapHanaDataProvider dataProvider, Type connectionType)
		{
			_dataProvider = dataProvider;
			_connectionType = connectionType;
		}

		readonly SapHanaDataProvider _dataProvider;
		readonly Type _connectionType;

		Func<IDbConnection,int,IDbTransaction,IDisposable> _bulkCopyCreator;
		Func<int,string,object>                            _columnMappingCreator;
		Action<object,Action<object>>                      _bulkCopySubscriber;

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection dataConnection,
			BulkCopyOptions options,
			IEnumerable<T> source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			var connection = dataConnection.Connection;

			if (connection == null )
				return MultipleRowsCopy(dataConnection, options, source);

			if (!(connection.GetType() == _connectionType || connection.GetType().IsSubclassOf(_connectionType)))
				return MultipleRowsCopy(dataConnection, options, source);

			var transaction = dataConnection.Transaction;
			var ed          = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns     = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var rc          = new BulkCopyRowsCopied();

			if (_bulkCopyCreator == null)
			{
				var clientNamespace    = _dataProvider.ConnectionNamespace;
				var bulkCopyType       = _connectionType.Assembly.GetType(clientNamespace + ".HanaBulkCopy", false);
				var bulkCopyOptionType = _connectionType.Assembly.GetType(clientNamespace + ".HanaBulkCopyOptions", false);
				var columnMappingType  = _connectionType.Assembly.GetType(clientNamespace + ".HanaBulkCopyColumnMapping", false);
				var transactionType    = _connectionType.Assembly.GetType(clientNamespace + ".HanaTransaction", false);

				if (bulkCopyType != null)
				{
					_bulkCopyCreator      = SapHanaCreateBulkCopyCreator(_connectionType, bulkCopyType, bulkCopyOptionType, transactionType);
					_columnMappingCreator = CreateColumnMappingCreator(columnMappingType);
				}
			}

			if (_bulkCopyCreator == null) 
				return MultipleRowsCopy(dataConnection, options, source);

			const int hanaOptions = 0; //default;

			using (var bc = _bulkCopyCreator(connection, hanaOptions, transaction))
			{
				dynamic dbc = bc;

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				{
					if (_bulkCopySubscriber == null)
					{
						_bulkCopySubscriber = CreateBulkCopySubscriber(bc, "HannaRowsCopied");
					}

					dbc.NotifyAfter = options.NotifyAfter;

					_bulkCopySubscriber(bc, arg =>
					{
						dynamic darg = arg;
						rc.RowsCopied = darg.RowsCopied;
						options.RowsCopiedCallback(rc);
						if (rc.Abort)
							darg.Abort = true;
					});
				}

				if (options.MaxBatchSize.HasValue)    dbc.BatchSize = options.MaxBatchSize.Value;
				if (options.BulkCopyTimeout.HasValue) dbc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

				var sqlBuilder = _dataProvider.CreateSqlBuilder();
				var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof (T));
				var tableName  = GetTableName(sqlBuilder, options, descriptor);

				dbc.DestinationTableName = tableName;

				for (var i = 0; i < columns.Count; i++)
					dbc.ColumnMappings.Add((dynamic) _columnMappingCreator(i, columns[i].ColumnName));

				var rd = new BulkCopyReader(_dataProvider, columns, source);

				TraceAction(
					dataConnection,
					"INSERT BULK " + tableName + Environment.NewLine,
					() => { dbc.WriteToServer(rd); return rd.Count; });

				if (rc.RowsCopied != rd.Count)
				{
					rc.RowsCopied = rd.Count;

					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
						options.RowsCopiedCallback(rc);
				}

				return rc;
			}
		}

		private static Func<IDbConnection, int, IDbTransaction, IDisposable> SapHanaCreateBulkCopyCreator(
			Type connectionType, Type bulkCopyType, Type bulkCopyOptionType, Type externalTransactionConnection)
		{
			var p1   = Expression.Parameter(typeof(IDbConnection),  "pc");
			var p2   = Expression.Parameter(typeof(int),            "po");
			var p3   = Expression.Parameter(typeof(IDbTransaction), "pt");
			var ctor = bulkCopyType.GetConstructor(new[]
			{
				connectionType, 
				bulkCopyOptionType, 
				externalTransactionConnection
			});

			if (ctor == null) return null;

			var l = Expression.Lambda<Func<IDbConnection, int, IDbTransaction, IDisposable>>(
				Expression.Convert(
					Expression.New(ctor,
						Expression.Convert(p1, connectionType),
						Expression.Convert(p2, bulkCopyOptionType),
						Expression.Convert(p3, externalTransactionConnection)),
					typeof(IDisposable)),
				p1, p2, p3);

			return l.Compile();
		}
	}
}
