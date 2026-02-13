using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Data;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

namespace LinqToDB
{
	/// <summary>
	/// Temporary table. Temporary table is a table, created when you create instance of this class and deleted when
	/// you dispose it. It uses regular tables even if underlying database supports temporary tables concept.
	/// </summary>
	/// <typeparam name="T">Table record mapping class.</typeparam>
	[PublicAPI]
	public class TempTable<T> : ITable<T>, ITableMutable<T>, IDisposable, IAsyncDisposable
		where T : notnull
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly ITable<T> _table;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly TempTableDescriptor? _tableDescriptor;

		/// <summary>
		/// Gets total number of records, inserted into table using BulkCopy.
		/// </summary>
		public long TotalCopied;

		/// <summary>
		/// Creates new temporary table.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		public TempTable(
			IDataContext db,
			string?      tableName    = default,
			string?      databaseName = default,
			string?      schemaName   = default,
			string?      serverName   = default,
			TableOptions tableOptions = default)
			: this(
				  db,
				  new CreateTempTableOptions(
					  TableName   : tableName,
					  DatabaseName: databaseName,
					  SchemaName  : schemaName,
					  ServerName  : serverName,
					  TableOptions: tableOptions))
		{
		}

		/// <summary>
		/// Creates new temporary table.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		public TempTable(
			IDataContext            db,
			CreateTempTableOptions? createOptions)
		{
			if (db == null) throw new ArgumentNullException(nameof(db));

			_table = db.CreateTable<T>(createOptions ?? CreateTableOptions.Default);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		public TempTable(IDataContext db,
			IEnumerable<T>   items,
			BulkCopyOptions? options      = default,
			string?          tableName    = default,
			string?          databaseName = default,
			string?          schemaName   = default,
			string?          serverName   = default,
			TableOptions     tableOptions = default)
			: this(db, new CreateTempTableOptions(tableName, databaseName, schemaName, serverName, TableOptions: tableOptions), items, options)
		{
		}

		/// <summary>
		/// Creates new temporary table.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="bulkCopyOptions">Optional BulkCopy options.</param>
		public TempTable(
			IDataContext            db,
			CreateTempTableOptions? createOptions,
			IEnumerable<T>          items,
			BulkCopyOptions?        bulkCopyOptions = default)
			: this(db, null, createOptions, items, bulkCopyOptions)
		{
		}

		/// <summary>
		/// Internal API to support table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="bulkCopyOptions">Optional BulkCopy options.</param>
		internal TempTable(
			IDataContext            db,
			TempTableDescriptor?    tableDescriptor,
			CreateTempTableOptions? createOptions,
			IEnumerable<T>          items,
			BulkCopyOptions?        bulkCopyOptions)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			_table           = db.CreateTable<T>(tableDescriptor?.EntityDescriptor, createOptions ?? CreateTableOptions.Default);
			_tableDescriptor = tableDescriptor;

			try
			{
				Copy(items, bulkCopyOptions);
			}
			catch
			{
				try
				{
					_table.DropTable();
				}
				catch
				{
					// ignore
				}

				throw;
			}
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		public TempTable(IDataContext db,
			string?          tableName,
			IEnumerable<T>   items,
			BulkCopyOptions? options      = default,
			string?          databaseName = default,
			string?          schemaName   = default,
			string?          serverName   = default,
			TableOptions     tableOptions = default)
			: this(db, items, options, tableName, databaseName, schemaName, serverName, tableOptions)
		{
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		public TempTable(
			IDataContext       db,
			IQueryable<T>      items,
			string?            tableName    = default,
			string?            databaseName = default,
			string?            schemaName   = default,
			Action<ITable<T>>? action       = default,
			string?            serverName   = default,
			TableOptions       tableOptions = default)
			: this(
				  db,
				  null,
				  new CreateTempTableOptions(
					  TableName   : tableName,
					  DatabaseName: databaseName,
					  SchemaName  : schemaName,
					  ServerName  : serverName,
					  TableOptions: tableOptions),
				  items,
				  action)
		{
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		public TempTable(
			IDataContext            db,
			CreateTempTableOptions? createOptions,
			IQueryable<T>           items,
			Action<ITable<T>>?      action = default)
			: this(db, null, createOptions, items, action)
		{
		}

		/// <summary>
		/// Internal API to support table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		internal TempTable(
			IDataContext            db,
			TempTableDescriptor?    tableDescriptor,
			CreateTempTableOptions? createOptions,
			IQueryable<T>           items,
			Action<ITable<T>>?      action)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			_table           = db.CreateTable<T>(tableDescriptor?.EntityDescriptor, createOptions ?? CreateTableOptions.Default);
			_tableDescriptor = tableDescriptor;

			try
			{
				action?.Invoke(_table);
				Insert(items);
			}
			catch
			{
				try
				{
					_table.DropTable();
				}
				catch
				{
					// ignore
				}

				throw;
			}
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		public TempTable(IDataContext db,
			string?            tableName,
			IQueryable<T>      items,
			string?            databaseName = default,
			string?            schemaName   = default,
			Action<ITable<T>>? action       = default,
			string?            serverName   = default,
			TableOptions       tableOptions = default)
			: this(db, items, tableName, databaseName, schemaName, action, serverName, tableOptions)
		{
		}

		/// <summary>
		/// Configures a temporary table that will be dropped when this instance is disposed.
		/// </summary>
		/// <param name="table">Table instance.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		TempTable(ITable<T> table, TempTableDescriptor? tableDescriptor)
		{
			_table           = table ?? throw new ArgumentNullException(nameof(table));
			_tableDescriptor = tableDescriptor;
		}

		/// <summary>
		/// Creates new temporary table.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public static Task<TempTable<T>> CreateAsync(
			IDataContext      db,
			string?           tableName         = default,
			string?           databaseName      = default,
			string?           schemaName        = default,
			string?           serverName        = default,
			TableOptions      tableOptions      = default,
			CancellationToken cancellationToken = default)
		{
			if (db == null) throw new ArgumentNullException(nameof(db));

			return CreateAsync(
				db,
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public static Task<TempTable<T>> CreateAsync(
			IDataContext            db,
			CreateTempTableOptions? createOptions,
			CancellationToken       cancellationToken = default)
		{
			if (db == null) throw new ArgumentNullException(nameof(db));

			return CreateAsync(db, null, createOptions, cancellationToken);
		}

		/// <summary>
		/// Internal API to support table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Creates new temporary table.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		internal static async Task<TempTable<T>> CreateAsync(
			IDataContext            db,
			TempTableDescriptor?    tableDescriptor,
			CreateTempTableOptions? createOptions,
			CancellationToken       cancellationToken)
		{
			if (db == null) throw new ArgumentNullException(nameof(db));

			return new TempTable<T>(await db
				.CreateTableAsync<T>(tableDescriptor, createOptions ?? CreateTableOptions.Default, token: cancellationToken)
				.ConfigureAwait(false),
				tableDescriptor);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public static Task<TempTable<T>> CreateAsync(
			IDataContext      db,
			IEnumerable<T>    items,
			BulkCopyOptions?  options           = default,
			string?           tableName         = default,
			string?           databaseName      = default,
			string?           schemaName        = default,
			string?           serverName        = default,
			TableOptions      tableOptions      = default,
			CancellationToken cancellationToken = default)
		{
			return CreateAsync(
				db,
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				options,
				cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="bulkCopyOptions">Optional BulkCopy options.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public static Task<TempTable<T>> CreateAsync(
			IDataContext            db,
			CreateTempTableOptions? createOptions,
			IEnumerable<T>          items,
			BulkCopyOptions?        bulkCopyOptions   = default,
			CancellationToken       cancellationToken = default)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			return CreateAsync(db, null, createOptions, items, bulkCopyOptions, cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public static Task<TempTable<T>> CreateAsync(IDataContext db,
			string?           tableName,
			IEnumerable<T>    items,
			BulkCopyOptions?  options           = default,
			string?           databaseName      = default,
			string?           schemaName        = default,
			string?           serverName        = default,
			TableOptions      tableOptions      = default,
			CancellationToken cancellationToken = default)
		{
			return CreateAsync(
				db,
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				options,
				cancellationToken);
		}

		/// <summary>
		/// Internal API to support table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="bulkCopyOptions">Optional BulkCopy options.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		internal static async Task<TempTable<T>> CreateAsync(
			IDataContext            db,
			TempTableDescriptor?    tableDescriptor,
			CreateTempTableOptions? createOptions,
			IEnumerable<T>          items,
			BulkCopyOptions?        bulkCopyOptions,
			CancellationToken       cancellationToken)
		{
			var table = await CreateAsync(db, tableDescriptor, createOptions, cancellationToken)
				.ConfigureAwait(false);

			try
			{
				await table.CopyAsync(items, bulkCopyOptions, cancellationToken)
					.ConfigureAwait(false);
			}
			catch
			{
				try
				{
					await table.DisposeAsync().ConfigureAwait(false);
				}
				catch
				{
					// ignore
				}

				throw;
			}

			return table;
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional asynchronous action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public static Task<TempTable<T>> CreateAsync(
			IDataContext           db,
			IQueryable<T>          items,
			string?                tableName         = default,
			string?                databaseName      = default,
			string?                schemaName        = default,
			Func<ITable<T>, Task>? action            = default,
			string?                serverName        = default,
			TableOptions           tableOptions      = default,
			CancellationToken      cancellationToken = default)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			return CreateAsync(
				db,
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				action,
				cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="action">Optional asynchronous action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public static Task<TempTable<T>> CreateAsync(
			IDataContext            db,
			CreateTempTableOptions? createOptions,
			IQueryable<T>           items,
			Func<ITable<T>, Task>?  action            = default,
			CancellationToken       cancellationToken = default)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			return CreateAsync(db, null, createOptions, items, action, cancellationToken);
		}

		/// <summary>
		/// Internal API to support table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="action">Optional asynchronous action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		internal static async Task<TempTable<T>> CreateAsync(
			IDataContext            db,
			TempTableDescriptor?    tableDescriptor,
			CreateTempTableOptions? createOptions,
			IQueryable<T>           items,
			Func<ITable<T>, Task>?  action,
			CancellationToken       cancellationToken)
		{
			var table = await CreateAsync(db, tableDescriptor, createOptions, cancellationToken)
				.ConfigureAwait(false);

			try
			{
				if (action != null)
					await action(table)
						.ConfigureAwait(false);

				await table.InsertAsync(items, cancellationToken)
					.ConfigureAwait(false);
			}
			catch
			{
				try
				{
					await table.DisposeAsync().ConfigureAwait(false);
				}
				catch
				{
					// ignore
				}

				throw;
			}

			return table;
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional asynchronous action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public static Task<TempTable<T>> CreateAsync(IDataContext db,
			string?               tableName,
			IQueryable<T>         items,
			string?               databaseName      = default,
			string?               schemaName        = default,
			Func<ITable<T>,Task>? action            = default,
			string?               serverName        = default,
			TableOptions          tableOptions      = default,
			CancellationToken     cancellationToken = default)
		{
			return CreateAsync(
				db,
				null,
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				action,
				cancellationToken);
		}

		/// <summary>
		/// Insert new records into table using BulkCopy.
		/// </summary>
		/// <param name="items">Records to insert into table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <returns>Number of records, inserted into table.</returns>
		public long Copy(IEnumerable<T> items, BulkCopyOptions? options = null)
		{
			var count = options != null ?
				_table.BulkCopy(options, items) :
				_table.BulkCopy(items);

			TotalCopied += count.RowsCopied;

			return count.RowsCopied;
		}

		/// <summary>
		/// Insert new records into table using BulkCopy.
		/// </summary>
		/// <param name="items">Records to insert into table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Number of records, inserted into table.</returns>
		public async Task<long> CopyAsync(IEnumerable<T> items, BulkCopyOptions? options = null, CancellationToken cancellationToken = default)
		{
			var count = options != null ?
				await _table.BulkCopyAsync(options, items, cancellationToken).ConfigureAwait(false) :
				await _table.BulkCopyAsync(items, cancellationToken).ConfigureAwait(false);

			TotalCopied += count.RowsCopied;

			return count.RowsCopied;
		}

		/// <summary>
		/// Insert data into table using records, returned by provided query.
		/// </summary>
		/// <param name="items">Query with records to insert into temporary table.</param>
		/// <returns>Number of records, inserted into table.</returns>
		public long Insert(IQueryable<T> items)
		{
			var count = items.Insert(_table, e => e);

			TotalCopied += count;

			return count;
		}

		/// <summary>
		/// Insert data into table using records, returned by provided query.
		/// </summary>
		/// <param name="items">Query with records to insert into temporary table.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Number of records, inserted into table.</returns>
		public async Task<long> InsertAsync(IQueryable<T> items, CancellationToken cancellationToken = default)
		{
			var count = await items.InsertAsync(_table, e => e, cancellationToken).ConfigureAwait(false);

			TotalCopied += count;

			return count;
		}

		#region ITable<T> implementation

		public string?      ServerName   => _table.ServerName;
		public string?      DatabaseName => _table.DatabaseName;
		public string?      SchemaName   => _table.SchemaName;
		public string       TableName    => _table.TableName;
		public TableOptions TableOptions => _table.TableOptions;
		public string?      TableID      => _table.TableID;

		#endregion

		#region ITableMutable<T> implementation

		ITable<T> ITableMutable<T>.ChangeServerName     (string?          serverName)      => ((ITableMutable<T>)_table).ChangeServerName     (serverName);
		ITable<T> ITableMutable<T>.ChangeDatabaseName   (string?          databaseName)    => ((ITableMutable<T>)_table).ChangeDatabaseName   (databaseName);
		ITable<T> ITableMutable<T>.ChangeSchemaName     (string?          schemaName)      => ((ITableMutable<T>)_table).ChangeSchemaName     (schemaName);
		ITable<T> ITableMutable<T>.ChangeTableName      (string           tableName)       => ((ITableMutable<T>)_table).ChangeTableName      (tableName);
		ITable<T> ITableMutable<T>.ChangeTableOptions   (TableOptions     options)         => ((ITableMutable<T>)_table).ChangeTableOptions   (options);
		ITable<T> ITableMutable<T>.ChangeTableDescriptor(EntityDescriptor tableDescriptor) => ((ITableMutable<T>)_table).ChangeTableDescriptor(tableDescriptor);
		ITable<T> ITableMutable<T>.ChangeTableID        (string?          tableID)         => ((ITableMutable<T>)_table).ChangeTableID        (tableID);

		#endregion

		#region IQueryProvider

		IQueryable IQueryProvider.CreateQuery(Expression expression)
		{
			return _table.CreateQuery(expression);
		}

		IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
		{
			return _table.CreateQuery<TElement>(expression);
		}

		object? IQueryProvider.Execute(Expression expression)
		{
			return _table.Execute(expression);
		}

		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			return _table.Execute<TResult>(expression);
		}

		#endregion

		#region IQueryProviderAsync

		Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			return _table.ExecuteAsync<TResult>(expression, cancellationToken);
		}

		Task<IAsyncEnumerable<TResult>> IQueryProviderAsync.ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			return _table.ExecuteAsyncEnumerable<TResult>(expression, cancellationToken);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Expression IQueryProviderAsync.Expression => ((IQueryable)_table).Expression;

		#endregion

		#region IExpressionQuery<T>

		public Expression Expression => _table.Expression;

		#endregion

		#region IExpressionQuery

		/// <summary>
		/// Gets data connection, associated with current table.
		/// </summary>
		public IDataContext DataContext => _table.DataContext;

		public QueryDebugView DebugView => _table.DebugView;

		IReadOnlyList<QuerySql> IExpressionQuery.GetSqlQueries(SqlGenerationOptions? options) => _table.GetSqlQueries(options);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Expression IExpressionQuery.Expression => ((IExpressionQuery)_table).Expression;

		#endregion

		#region IQueryable

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Expression IQueryable.Expression => ((IQueryable)_table).Expression;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Type IQueryable.ElementType => _table.ElementType;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IQueryProvider IQueryable.Provider    => _table.Provider;

		#endregion

		#region IEnumerable<T>

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return _table.GetEnumerator();
		}

		#endregion

		#region IEnumerable

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_table).GetEnumerator();
		}

		#endregion

		public virtual void Dispose()
		{
			try
			{
				_table.DropTable(throwExceptionIfNotExists: false);
			}
			finally
			{
				// Restore MappingSchema if it was changed by FluentMapping.
				//
				if (_tableDescriptor != null)
					_table.DataContext.SetMappingSchema(_tableDescriptor.PrevMappingSchema);
			}
		}

		public virtual ValueTask DisposeAsync()
		{
			try
			{
				return new ValueTask(_table.DropTableAsync(throwExceptionIfNotExists: false));
			}
			finally
			{
				// Restore MappingSchema if it was changed by FluentMapping.
				//
				if (_tableDescriptor != null)
					_table.DataContext.SetMappingSchema(_tableDescriptor.PrevMappingSchema);
			}
		}
	}
}
