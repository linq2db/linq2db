#nullable disable
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using LinqToDB.Configuration;

namespace LinqToDB.DataProvider.SqlServer
{
	using System.Data;
	using System.Linq.Expressions;
	using Data;
	using SqlProvider;

	class SqlServerBulkCopy : BasicBulkCopy
	{
		public SqlServerBulkCopy(SqlServerDataProvider dataProvider, Type connectionType)
		{
			_dataProvider = dataProvider;
		}

		readonly SqlServerDataProvider                                       _dataProvider;
		readonly Type                                                        _connectionType;
		Func<IDbConnection, SqlBulkCopyOptions, IDbTransaction, IDisposable> _bulkCopyFactory;
		Func<int, string, object>                                            _columnMappingFactory;
		Action<object, Action<object>>                                       _bulkCopySubscriber;

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			[JetBrains.Annotations.NotNull] ITable<T> table,
			BulkCopyOptions options,
			IEnumerable<T>  source)
		{
			if (!(table?.DataContext is DataConnection dataConnection))
				throw new ArgumentNullException(nameof(dataConnection));

			var connection = dataConnection.Connection;
			if (connection is DbConnection dbConnection)
			{
				if (Proxy.GetUnderlyingObject(dbConnection).GetType() == _connectionType)
				{
					var ed      = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
					var columns = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
					var sb      = _dataProvider.CreateSqlBuilder(dataConnection.MappingSchema);
					var rd      = new BulkCopyReader(_dataProvider, dataConnection.MappingSchema, columns, source);
					var sqlopt  = SqlBulkCopyOptions.Default;
					var rc      = new BulkCopyRowsCopied();

					if (_bulkCopyFactory == null)
					{
						var clientNamespace    = _dataProvider.ConnectionNamespace;
						var bulkCopyType       = _connectionType.Assembly.GetType(clientNamespace + ".SqlBulkCopy",              true);
						var bulkCopyOptionType = _connectionType.Assembly.GetType(clientNamespace + ".SqlBulkCopyOptions",       true);
						var transactionType    = _connectionType.Assembly.GetType(clientNamespace + ".SqlTransaction",           true);
						var columnMappingType  = _connectionType.Assembly.GetType(clientNamespace + ".SqlBulkCopyColumnMapping", true);

						_bulkCopyFactory      = CreateBulkCopyFactory(_connectionType, bulkCopyType, bulkCopyOptionType, transactionType);
						_columnMappingFactory = CreateColumnMappingCreator(columnMappingType);
					}

					if (options.CheckConstraints       == true) sqlopt |= SqlBulkCopyOptions.CheckConstraints;
					if (options.KeepIdentity           == true) sqlopt |= SqlBulkCopyOptions.KeepIdentity;
					if (options.TableLock              == true) sqlopt |= SqlBulkCopyOptions.TableLock;
					if (options.KeepNulls              == true) sqlopt |= SqlBulkCopyOptions.KeepNulls;
					if (options.FireTriggers           == true) sqlopt |= SqlBulkCopyOptions.FireTriggers;
					if (options.UseInternalTransaction == true) sqlopt |= SqlBulkCopyOptions.UseInternalTransaction;

					using (var bc = _bulkCopyFactory(connection, sqlopt, dataConnection.Transaction))
					{
						if (_bulkCopySubscriber == null)
						{
							_bulkCopySubscriber = CreateBulkCopySubscriber(bc, "SqlRowsCopied");
						}

						dynamic dbc = bc;
						if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
						{
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

						if (options.MaxBatchSize.   HasValue) dbc.BatchSize       = options.MaxBatchSize.   Value;
						if (options.BulkCopyTimeout.HasValue) dbc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

						var sqlBuilder = _dataProvider.CreateSqlBuilder(dataConnection.MappingSchema);
						var tableName  = GetTableName(sqlBuilder, options, table);

						dbc.DestinationTableName = tableName;

						for (var i = 0; i < columns.Count; i++)
							dbc.ColumnMappings.Add((dynamic)_columnMappingFactory(i, sb.Convert(columns[i].ColumnName, ConvertType.NameToQueryField).ToString()));

						TraceAction(
							dataConnection,
							() => "INSERT BULK " + tableName + "("+ string.Join(", ", ((IEnumerable<dynamic>)dbc.ColumnMappings).Select(x => x.DestinationColumn)) + Environment.NewLine,
							() => { dbc.WriteToServer(rd); return rd.Count; });
					}

					if (rc.RowsCopied != rd.Count)
					{
						rc.RowsCopied = rd.Count;

						if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
							options.RowsCopiedCallback(rc);
					}

					return rc;
				}
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			BulkCopyRowsCopied ret;

			var helper = new MultipleRowsHelper<T>(table, options);

			if (options.KeepIdentity == true)
				helper.DataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " ON");

			switch (((SqlServerDataProvider)helper.DataConnection.DataProvider).Version)
			{
				case SqlServerVersion.v2000 :
				case SqlServerVersion.v2005 : ret = MultipleRowsCopy2(helper, source, ""); break;
				default                     : ret = MultipleRowsCopy1(helper, source);     break;
			}

			if (options.KeepIdentity == true)
				helper.DataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " OFF");

			return ret;
		}

		[Flags]
		private enum SqlBulkCopyOptions
		{
			AllowEncryptedValueModifications = 64,
			CheckConstraints = 2,
			Default = 0,
			FireTriggers = 16,
			KeepIdentity = 1,
			KeepNulls = 8,
			TableLock = 4,
			UseInternalTransaction = 32
		}

		static Func<IDbConnection, SqlBulkCopyOptions, IDbTransaction, IDisposable> CreateBulkCopyFactory(
			Type connectionType, Type bulkCopyType, Type bulkCopyOptionType, Type externalTransactionConnection)
		{
			var p1   = Expression.Parameter(typeof(IDbConnection),      "pc");
			var p2   = Expression.Parameter(typeof(SqlBulkCopyOptions), "po");
			var p3   = Expression.Parameter(typeof(IDbTransaction),     "pt");
			var ctor = bulkCopyType.GetConstructor(new[]
			{
				connectionType,
				bulkCopyOptionType,
				externalTransactionConnection
			});

			var l = Expression.Lambda<Func<IDbConnection, SqlBulkCopyOptions, IDbTransaction, IDisposable>>(
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
