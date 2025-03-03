using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB
{
	// TODO: v6: obsolete methods with setTable parameter
	// IT: ??? how to use anonymous types then?
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
		readonly ITable<T>         _table;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly EntityDescriptor? _tableDescriptor;

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
		{
			if (db == null) throw new ArgumentNullException(nameof(db));

			_table = db.CreateTable<T>(tableName, databaseName, schemaName, serverName: serverName, tableOptions: tableOptions);
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
			: this(db, null, items, options, tableName, databaseName, schemaName, serverName, tableOptions)
		{
		}

		/// <summary>
		/// Internal API to support table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		internal TempTable(
			IDataContext      db,
			EntityDescriptor? tableDescriptor,
			IEnumerable<T>    items,
			BulkCopyOptions?  options,
			string?           tableName,
			string?           databaseName,
			string?           schemaName,
			string?           serverName,
			TableOptions      tableOptions)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			_table           = db.CreateTable<T>(tableDescriptor, tableName, databaseName, schemaName, serverName: serverName, tableOptions: tableOptions);
			_tableDescriptor = tableDescriptor;

			try
			{
				Copy(items, options);
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
		public TempTable(IDataContext db,
			IQueryable<T>             items,
			string?                   tableName    = default,
			string?                   databaseName = default,
			string?                   schemaName   = default,
			Action<ITable<T>>?        action       = default,
			string?                   serverName   = default,
			TableOptions              tableOptions = default)
			: this(db, null, items, tableName, databaseName, schemaName, action, serverName, tableOptions)
		{
		}

		/// <summary>
		/// Internal API to support table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		internal TempTable(
			IDataContext       db,
			EntityDescriptor?  tableDescriptor,
			IQueryable<T>      items,
			string?            tableName,
			string?            databaseName,
			string?            schemaName,
			Action<ITable<T>>? action,
			string?            serverName,
			TableOptions       tableOptions)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			_table           = db.CreateTable<T>(tableDescriptor, tableName, databaseName, schemaName, serverName: serverName, tableOptions: tableOptions);
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
		/// Configures a temporary table that will be dropped when this instance is disposed
		/// </summary>
		/// <param name="table">Table instance.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		protected TempTable(ITable<T> table, EntityDescriptor? tableDescriptor)
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

			return CreateAsync(db, null, tableName, databaseName, schemaName, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Internal API to support table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Creates new temporary table.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		internal static async Task<TempTable<T>> CreateAsync(
			IDataContext      db,
			EntityDescriptor? tableDescriptor,
			string?           tableName,
			string?           databaseName,
			string?           schemaName,
			string?           serverName,
			TableOptions      tableOptions,
			CancellationToken cancellationToken)
		{
			if (db == null) throw new ArgumentNullException(nameof(db));

			return new TempTable<T>(await db
				.CreateTableAsync<T>(tableDescriptor, tableName, databaseName, schemaName, serverName: serverName, tableOptions: tableOptions, token: cancellationToken)
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
		public static Task<TempTable<T>> CreateAsync(IDataContext db,
			IEnumerable<T>    items,
			BulkCopyOptions?  options           = default,
			string?           tableName         = default,
			string?           databaseName      = default,
			string?           schemaName        = default,
			string?           serverName        = default,
			TableOptions      tableOptions      = default,
			CancellationToken cancellationToken = default)
		{
			return CreateAsync(db, tableName, items, options, databaseName, schemaName, serverName, tableOptions, cancellationToken);
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
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			return CreateAsync(db, null, tableName, items, options, databaseName, schemaName, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Internal API to support table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		internal static async Task<TempTable<T>> CreateAsync(
			IDataContext      db,
			EntityDescriptor? tableDescriptor,
			string?           tableName,
			IEnumerable<T>    items,
			BulkCopyOptions?  options,
			string?           databaseName,
			string?           schemaName,
			string?           serverName,
			TableOptions      tableOptions,
			CancellationToken cancellationToken)
		{
			var table = await CreateAsync(db, tableDescriptor, tableName, databaseName, schemaName, serverName, tableOptions, cancellationToken)
				.ConfigureAwait(false);

			try
			{
				await table.CopyAsync(items, options, cancellationToken)
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
		public static Task<TempTable<T>> CreateAsync(IDataContext db,
			IQueryable<T>         items,
			string?               tableName         = default,
			string?               databaseName      = default,
			string?               schemaName        = default,
			Func<ITable<T>,Task>? action            = default,
			string?               serverName        = default,
			TableOptions          tableOptions      = default,
			CancellationToken     cancellationToken = default)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			return CreateAsync(db, null, items, tableName, databaseName, schemaName, action, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Internal API to support table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional asynchronous action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. If not specified, value from mapping will be used.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		internal static async Task<TempTable<T>> CreateAsync(
			IDataContext          db,
			EntityDescriptor?     tableDescriptor,
			IQueryable<T>         items,
			string?               tableName,
			string?               databaseName,
			string?               schemaName,
			Func<ITable<T>,Task>? action,
			string?               serverName,
			TableOptions          tableOptions,
			CancellationToken     cancellationToken)
		{
			var table = await CreateAsync(db, tableDescriptor, tableName, databaseName, schemaName, serverName, tableOptions, cancellationToken)
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
			return CreateAsync(db, null, items, tableName, databaseName, schemaName, action, serverName, tableOptions, cancellationToken);
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

		static readonly ConcurrentDictionary<Type,Expression<Func<T,T>>> _setterDic = new ();

		/// <summary>
		/// Insert data into table using records, returned by provided query.
		/// </summary>
		/// <param name="items">Query with records to insert into temporary table.</param>
		/// <returns>Number of records, inserted into table.</returns>
		public long Insert(IQueryable<T> items)
		{
			var l = GenerateInsertSetter(items ?? throw new ArgumentNullException(nameof(items)));

			var count = items.Insert(_table, l);

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
			var l = GenerateInsertSetter(items ?? throw new ArgumentNullException(nameof(items)));

			var count = await items.InsertAsync(_table, l, cancellationToken).ConfigureAwait(false);

			TotalCopied += count;

			return count;
		}

		private Expression<Func<T,T>> GenerateInsertSetter(IQueryable<T> items)
		{
			var type = typeof(T);
			var ed   = _tableDescriptor ?? _table.DataContext.MappingSchema.GetEntityDescriptor(type, _table.DataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var p    = Expression.Parameter(type, "t");

			return _setterDic.GetOrAdd(type, t =>
			{
				if (t.IsAnonymous())
				{
					var nctor = (NewExpression?)items.Expression.Find(t, static (t, e) => e.NodeType == ExpressionType.New && e.Type == t);

					MemberInfo[]    members;
					ConstructorInfo ctor;

					if (nctor == null)
					{
						ctor    = t.GetConstructors().Single();
						members = t.GetPublicInstanceValueMembers();
					}
					else
					{
						ctor    = nctor.Constructor!;
						members = nctor.Members!
							.Select(m => m is MethodInfo info ? info.GetPropertyInfo()! : m)
							.ToArray();
					}

					return Expression.Lambda<Func<T,T>>(
						Expression.New(
							ctor,
							members.Select(m => ExpressionHelper.PropertyOrField(p, m.Name)),
							members),
						p);
				}

				return Expression.Lambda<Func<T,T>>(
					Expression.MemberInit(
						Expression.New(t),
						ed.Columns.Select(c => Expression.Bind(c.MemberInfo, Expression.MakeMemberAccess(p, c.MemberInfo)))),
					p);
			});
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
			_table.DropTable(throwExceptionIfNotExists: false);
		}

		public virtual ValueTask DisposeAsync()
		{
			return new ValueTask(_table.DropTableAsync(throwExceptionIfNotExists: false));
		}
	}

	public static partial class DataExtensions
	{
		#region CreateTempTable

		/// <summary>
		/// Creates new temporary table.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			this         IDataContext db,
			string?      tableName    = default,
			string?      databaseName = default,
			string?      schemaName   = default,
			string?      serverName   = default,
			TableOptions tableOptions = TableOptions.IsTemporary)
			where T : class
		{
			return new TempTable<T>(db, tableName, databaseName, schemaName, serverName, tableOptions);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext db,
			IEnumerable<T>    items,
			BulkCopyOptions?  options      = default,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = TableOptions.IsTemporary)
			where T : class
		{
			return new TempTable<T>(db, items, options, tableName, databaseName, schemaName, serverName, tableOptions);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext               db,
			IEnumerable<T>                  items,
			Action<EntityMappingBuilder<T>> setTable,
			BulkCopyOptions?                options      = default,
			string?                         tableName    = default,
			string?                         databaseName = default,
			string?                         schemaName   = default,
			string?                         serverName   = default,
			TableOptions                    tableOptions = TableOptions.IsTemporary)
			where T : class
		{
			if (setTable == null) throw new ArgumentNullException(nameof(setTable));

			var tempTableDescriptor = GetTempTableDescriptor(db, setTable);

			return new TempTable<T>(db, tempTableDescriptor, items, options, tableName, databaseName, schemaName, serverName, tableOptions);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext db,
			string            tableName,
			IEnumerable<T>    items,
			BulkCopyOptions?  options      = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = TableOptions.IsTemporary)
			where T : class
		{
			return new TempTable<T>(db, tableName, items, options, databaseName, schemaName, serverName, tableOptions);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext               db,
			string                          tableName,
			IEnumerable<T>                  items,
			Action<EntityMappingBuilder<T>> setTable,
			BulkCopyOptions?                options      = default,
			string?                         databaseName = default,
			string?                         schemaName   = default,
			string?                         serverName   = default,
			TableOptions                    tableOptions = TableOptions.IsTemporary)
			where T : class
		{
			if (setTable == null) throw new ArgumentNullException(nameof(setTable));

			var tempTableDescriptor = GetTempTableDescriptor(db, setTable);

			return new TempTable<T>(db, tempTableDescriptor, items, options, tableName, databaseName, schemaName, serverName, tableOptions);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext  db,
			IQueryable<T>      items,
			string?            tableName    = default,
			string?            databaseName = default,
			string?            schemaName   = default,
			Action<ITable<T>>? action       = default,
			string?            serverName   = default,
			TableOptions       tableOptions = TableOptions.IsTemporary)
			where T : class
		{
			return new TempTable<T>(db, items, tableName, databaseName, schemaName, action, serverName, tableOptions);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query. Table mapping could be changed
		/// using fluent mapper.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext               db,
			IQueryable<T>                   items,
			Action<EntityMappingBuilder<T>> setTable,
			string?                         tableName    = default,
			string?                         databaseName = default,
			string?                         schemaName   = default,
			Action<ITable<T>>?              action       = default,
			string?                         serverName   = default,
			TableOptions                    tableOptions = TableOptions.IsTemporary)
			where T : class
		{
			if (setTable == null) throw new ArgumentNullException(nameof(setTable));

			var tempTableDescriptor = GetTempTableDescriptor(db, setTable);

			return new TempTable<T>(db, tempTableDescriptor, items, tableName, databaseName, schemaName, action, serverName, tableOptions);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext  db,
			string?            tableName,
			IQueryable<T>      items,
			string?            databaseName = default,
			string?            schemaName   = default,
			Action<ITable<T>>? action       = default,
			string?            serverName   = default,
			TableOptions       tableOptions = TableOptions.IsTemporary)
			where T : class
		{
			return new TempTable<T>(db, tableName, items, databaseName, schemaName, action, serverName, tableOptions);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query. Table mapping could be changed
		/// using fluent mapper.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext               db,
			string?                         tableName,
			IQueryable<T>                   items,
			Action<EntityMappingBuilder<T>> setTable,
			string?                         databaseName = default,
			string?                         schemaName   = default,
			Action<ITable<T>>?              action       = default,
			string?                         serverName   = default,
			TableOptions                    tableOptions = TableOptions.IsTemporary)
			where T : class
		{
			if (setTable == null) throw new ArgumentNullException(nameof(setTable));

			var tempTableDescriptor = GetTempTableDescriptor(db, setTable);

			return new TempTable<T>(db, tempTableDescriptor, items, tableName, databaseName, schemaName, action, serverName, tableOptions);
		}

		/// <summary>
		/// Creates new temporary table.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext db,
			string?           tableName         = default,
			string?           databaseName      = default,
			string?           schemaName        = default,
			string?           serverName        = default,
			TableOptions      tableOptions      = TableOptions.IsTemporary,
			CancellationToken cancellationToken = default)
			where T : class
		{
			return TempTable<T>.CreateAsync(db, tableName, databaseName, schemaName, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext db,
			IEnumerable<T>    items,
			BulkCopyOptions?  options           = default,
			string?           tableName         = default,
			string?           databaseName      = default,
			string?           schemaName        = default,
			string?           serverName        = default,
			TableOptions      tableOptions      = TableOptions.IsTemporary,
			CancellationToken cancellationToken = default)
			where T : class
		{
			return TempTable<T>.CreateAsync(db, items, options, tableName, databaseName, schemaName, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext               db,
			IEnumerable<T>                  items,
			Action<EntityMappingBuilder<T>> setTable,
			BulkCopyOptions?                options           = default,
			string?                         tableName         = default,
			string?                         databaseName      = default,
			string?                         schemaName        = default,
			string?                         serverName        = default,
			TableOptions                    tableOptions      = TableOptions.IsTemporary,
			CancellationToken               cancellationToken = default)
			where T : class
		{
			if (setTable == null) throw new ArgumentNullException(nameof(setTable));

			var tempTableDescriptor = GetTempTableDescriptor(db, setTable);

			return TempTable<T>.CreateAsync(db, tempTableDescriptor, tableName, items, options, databaseName, schemaName, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext db,
			string            tableName,
			IEnumerable<T>    items,
			BulkCopyOptions?  options           = default,
			string?           databaseName      = default,
			string?           schemaName        = default,
			string?           serverName        = default,
			TableOptions      tableOptions      = TableOptions.IsTemporary,
			CancellationToken cancellationToken = default)
			where T : class
		{
			return TempTable<T>.CreateAsync(db, tableName, items, options, databaseName, schemaName, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext               db,
			string                          tableName,
			IEnumerable<T>                  items,
			Action<EntityMappingBuilder<T>> setTable,
			BulkCopyOptions?                options           = default,
			string?                         databaseName      = default,
			string?                         schemaName        = default,
			string?                         serverName        = default,
			TableOptions                    tableOptions      = TableOptions.IsTemporary,
			CancellationToken               cancellationToken = default)
			where T : class
		{
			if (setTable == null) throw new ArgumentNullException(nameof(setTable));

			var tempTableDescriptor = GetTempTableDescriptor(db, setTable);

			return TempTable<T>.CreateAsync(db, tempTableDescriptor, tableName, items, options, databaseName, schemaName, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional asynchronous action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext     db,
			IQueryable<T>         items,
			string?               tableName         = default,
			string?               databaseName      = default,
			string?               schemaName        = default,
			Func<ITable<T>,Task>? action            = default,
			string?               serverName        = default,
			TableOptions          tableOptions      = TableOptions.IsTemporary,
			CancellationToken     cancellationToken = default)
			where T : class
		{
			return TempTable<T>.CreateAsync(db, items, tableName, databaseName, schemaName, action, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query. Table mapping could be changed
		/// using fluent mapper.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional asynchronous action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext               db,
			IQueryable<T>                   items,
			Action<EntityMappingBuilder<T>> setTable,
			string?                         tableName         = default,
			string?                         databaseName      = default,
			string?                         schemaName        = default,
			Func<ITable<T>,Task>?           action            = default,
			string?                         serverName        = default,
			TableOptions                    tableOptions      = TableOptions.IsTemporary,
			CancellationToken               cancellationToken = default)
			where T : class
		{
			if (setTable == null) throw new ArgumentNullException(nameof(setTable));

			var tempTableDescriptor = GetTempTableDescriptor(db, setTable);

			return TempTable<T>.CreateAsync(db, tempTableDescriptor, items, tableName, databaseName, schemaName, action, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional asynchronous action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext     db,
			string?               tableName,
			IQueryable<T>         items,
			string?               databaseName      = default,
			string?               schemaName        = default,
			Func<ITable<T>,Task>? action            = default,
			string?               serverName        = default,
			TableOptions          tableOptions      = TableOptions.IsTemporary,
			CancellationToken     cancellationToken = default)
			where T : class
		{
			return TempTable<T>.CreateAsync(db, tableName, items, databaseName, schemaName, action, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query. Table mapping could be changed
		/// using fluent mapper.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional asynchronous action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext               db,
			string?                         tableName,
			IQueryable<T>                   items,
			Action<EntityMappingBuilder<T>> setTable,
			string?                         databaseName      = default,
			string?                         schemaName        = default,
			Func<ITable<T>,Task>?           action            = default,
			string?                         serverName        = default,
			TableOptions                    tableOptions      = TableOptions.IsTemporary,
			CancellationToken               cancellationToken = default)
			where T : class
		{
			if (setTable == null) throw new ArgumentNullException(nameof(setTable));

			var tempTableDescriptor = GetTempTableDescriptor(db, setTable);

			return TempTable<T>.CreateAsync(db, tempTableDescriptor, items, tableName, databaseName, schemaName, action, serverName, tableOptions, cancellationToken);
		}

		#endregion

		#region IntoTempTable

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> IntoTempTable<T>(
			this IEnumerable<T> items,
			IDataContext        db,
			string?             tableName    = default,
			string?             databaseName = default,
			string?             schemaName   = default,
			string?             serverName   = default,
			TableOptions        tableOptions = TableOptions.IsTemporary,
			BulkCopyOptions?    options      = default)
			where T : class
		{
			return new TempTable<T>(db, items, options, tableName, databaseName, schemaName, serverName, tableOptions);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation, but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> IntoTempTable<T>(
			this IQueryable<T>               items,
			string?                          tableName    = default,
			string?                          databaseName = default,
			string?                          schemaName   = default,
			string?                          serverName   = default,
			TableOptions                     tableOptions = TableOptions.IsTemporary,
			Action<ITable<T>>?               action       = default,
			Action<EntityMappingBuilder<T>>? setTable     = default)
			where T : class
		{
			if (items is IExpressionQuery eq)
			{
				EntityDescriptor? tempTableDescriptor = null;
				if (setTable != null)
					tempTableDescriptor = GetTempTableDescriptor(eq.DataContext, setTable);

				return new TempTable<T>(eq.DataContext, tempTableDescriptor, items, tableName, databaseName, schemaName, action, serverName, tableOptions);
			}

			throw new ArgumentException($"The '{nameof(items)}' argument must be of type 'LinqToDB.Linq.IExpressionQuery'.");
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static Task<TempTable<T>> IntoTempTableAsync<T>(
			this IEnumerable<T> items,
			IDataContext        db,
			string?             tableName         = default,
			string?             databaseName      = default,
			string?             schemaName        = default,
			string?             serverName        = default,
			TableOptions        tableOptions      = TableOptions.IsTemporary,
			BulkCopyOptions?    options           = default,
			CancellationToken   cancellationToken = default)
			where T : class
		{
			return TempTable<T>.CreateAsync(db, items, options, tableName, databaseName, schemaName, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query. Table mapping could be changed
		/// using fluent mapper.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table schema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional asynchronous action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Optional Table options. Default is <see cref="TableOptions.IsTemporary"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static Task<TempTable<T>> IntoTempTableAsync<T>(
			this IQueryable<T>               items,
			string?                          tableName         = default,
			string?                          databaseName      = default,
			string?                          schemaName        = default,
			string?                          serverName        = default,
			TableOptions                     tableOptions      = TableOptions.IsTemporary,
			Func<ITable<T>,Task>?            action            = default,
			Action<EntityMappingBuilder<T>>? setTable          = default,
			CancellationToken                cancellationToken = default)
			where T : class
		{
			if (items is IExpressionQuery eq)
			{
				EntityDescriptor? tempTableDescriptor = null;
				if (setTable != null)
					tempTableDescriptor = GetTempTableDescriptor(eq.DataContext, setTable);

				return TempTable<T>.CreateAsync(eq.DataContext, tempTableDescriptor, items, tableName, databaseName, schemaName, action, serverName, tableOptions, cancellationToken);
			}

			throw new ArgumentException($"The '{nameof(items)}' argument must be of type 'LinqToDB.Linq.IExpressionQuery'.");
		}

		private static EntityDescriptor GetTempTableDescriptor<T>(IDataContext dataContext, Action<EntityMappingBuilder<T>> setTable)
		{
			var ms      = new MappingSchema(dataContext.MappingSchema);
			var builder = new FluentMappingBuilder(ms);

			setTable(builder.Entity<T>());
			builder.Build();

			dataContext.AddMappingSchema(ms);

			return ms.GetEntityDescriptor(typeof(T), dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);
		}

		#endregion
	}
}
