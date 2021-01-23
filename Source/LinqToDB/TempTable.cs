using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Async;
	using Data;
	using Expressions;
	using Extensions;
	using Linq;
	using Mapping;

	/// <summary>
	/// Temporary table. Temporary table is a table, created when you create instance of this class and deleted when
	/// you dispose it. It uses regular tables even if underlying database supports temporary tables concept.
	/// </summary>
	/// <typeparam name="T">Table record mapping class.</typeparam>
	[PublicAPI]
	public class TempTable<T> : ITable<T>, ITableMutable<T>, IDisposable
#if !NETFRAMEWORK
		, IAsyncDisposable
#endif
	{
		readonly ITable<T> _table;

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
		public TempTable(IDataContext db,
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
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			_table = db.CreateTable<T>(tableName, databaseName, schemaName, serverName: serverName, tableOptions: tableOptions);

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
			IQueryable<T>      items,
			string?            tableName    = default,
			string?            databaseName = default,
			string?            schemaName   = default,
			Action<ITable<T>>? action       = default,
			string?            serverName   = default,
			TableOptions       tableOptions = default)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			_table = db.CreateTable<T>(tableName, databaseName, schemaName, serverName: serverName, tableOptions: tableOptions);

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
		protected TempTable(ITable<T> table)
		{
			_table = table ?? throw new ArgumentNullException(nameof(table));
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
		public static async Task<TempTable<T>> CreateAsync(IDataContext db,
			string?           tableName         = default,
			string?           databaseName      = default,
			string?           schemaName        = default,
			string?           serverName        = default,
			TableOptions      tableOptions      = default,
			CancellationToken cancellationToken = default)
		{
			if (db == null) throw new ArgumentNullException(nameof(db));

			return new TempTable<T>(await db
				.CreateTableAsync<T>(tableName, databaseName, schemaName, serverName: serverName, tableOptions: tableOptions, token: cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext));
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
		public static async Task<TempTable<T>> CreateAsync(IDataContext db,
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

			var table = await CreateAsync(db, tableName, databaseName, schemaName, serverName, tableOptions, cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			try
			{
				await table.CopyAsync(items, options, cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
			catch
			{
				try
				{
#if NETFRAMEWORK
					table.Dispose();
#else
					await table.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
#endif
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
		public static async Task<TempTable<T>> CreateAsync(IDataContext db,
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

			var table = await CreateAsync(db, tableName, databaseName, schemaName, serverName, tableOptions, cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			try
			{
				if (action != null)
					await action(table)
						.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				await table.InsertAsync(items, cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
			catch
			{
				try
				{
#if NETFRAMEWORK
					table.Dispose();
#else
					await table.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
#endif
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
			return CreateAsync(db, items, tableName, databaseName, schemaName, action, serverName, tableOptions, cancellationToken);
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
				await _table.BulkCopyAsync(options, items, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext) :
				await _table.BulkCopyAsync(items, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			TotalCopied += count.RowsCopied;

			return count.RowsCopied;
		}

		static readonly ConcurrentDictionary<Type,Expression<Func<T,T>>> _setterDic = new ConcurrentDictionary<Type,Expression<Func<T,T>>>();

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

			var count = await items.InsertAsync(_table, l, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			TotalCopied += count;

			return count;
		}

		private Expression<Func<T,T>> GenerateInsertSetter(IQueryable<T> items)
		{
			var type = typeof(T);
			var ed   = _table.DataContext.MappingSchema.GetEntityDescriptor(type);
			var p    = Expression.Parameter(type, "t");

			return _setterDic.GetOrAdd(type, t =>
			{
				if (t.IsAnonymous())
				{
					var nctor = (NewExpression?)items.Expression.Find(e => e.NodeType == ExpressionType.New && e.Type == t)!;

					MemberInfo[]    members;
					ConstructorInfo ctor;

					if (nctor == null)
					{
						ctor    = t.GetConstructors().Single();
						members = t.GetPublicInstanceValueMembers();
					}
					else
					{
						ctor    = nctor.Constructor;
						members = nctor.Members
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

		public string GetTableName()
		{
			return _table.GetTableName();
		}

		#endregion

		#region ITableMutable<T> implementation

		ITable<T> ITableMutable<T>.ChangeServerName  (string? serverName)        => ((ITableMutable<T>)_table).ChangeServerName  (serverName);
		ITable<T> ITableMutable<T>.ChangeDatabaseName(string? databaseName)      => ((ITableMutable<T>)_table).ChangeDatabaseName(databaseName);
		ITable<T> ITableMutable<T>.ChangeSchemaName  (string? schemaName)        => ((ITableMutable<T>)_table).ChangeSchemaName  (schemaName);
		ITable<T> ITableMutable<T>.ChangeTableName   (string tableName)          => ((ITableMutable<T>)_table).ChangeTableName   (tableName);
		ITable<T> ITableMutable<T>.ChangeTableOptions(TableOptions tableOptions) => ((ITableMutable<T>)_table).ChangeTableOptions(tableOptions);

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

		object IQueryProvider.Execute(Expression expression)
		{
			return _table.Execute(expression);
		}

		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			return _table.Execute<TResult>(expression);
		}

		#endregion

		#region IQueryProviderAsync

		Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken token)
		{
			return _table.ExecuteAsync<TResult>(expression, token);
		}

		Task<IAsyncEnumerable<TResult>> IQueryProviderAsync.ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken token)
		{
			return _table.ExecuteAsyncEnumerable<TResult>(expression, token);
		}

		#endregion

		#region IExpressionQuery<T>

		Expression IExpressionQuery<T>.Expression
		{
			get => _table.Expression;
			set => _table.Expression = value;
		}

		#endregion

		#region IExpressionQuery

		/// <summary>
		/// Gets data connection, associated with current table.
		/// </summary>
		public IDataContext DataContext => _table.DataContext;

		string       IExpressionQuery.SqlText    => _table.SqlText;
		Expression   IExpressionQuery.Expression => ((IExpressionQuery)_table).Expression;

		#endregion

		#region IQueryable

		Expression IQueryable.Expression => ((IQueryable)_table).Expression;

		Type           IQueryable.ElementType => _table.ElementType;
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
			_table.DropTable();
		}

#if !NETFRAMEWORK
		public ValueTask DisposeAsync()
		{
			return new ValueTask(_table.DropTableAsync());
		}
#endif
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
			string?           tableName,
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
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.</param>
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

			setTable(db.MappingSchema.GetFluentMappingBuilder().Entity<T>());

			return new TempTable<T>(db, items, tableName, databaseName, schemaName, action, serverName, tableOptions);
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
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.</param>
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

			setTable(db.MappingSchema.GetFluentMappingBuilder().Entity<T>());

			return new TempTable<T>(db, tableName, items, databaseName, schemaName, action, serverName, tableOptions);
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
			string?           tableName,
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
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.</param>
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

			setTable(db.MappingSchema.GetFluentMappingBuilder().Entity<T>());

			return TempTable<T>.CreateAsync(db, items, tableName, databaseName, schemaName, action, serverName, tableOptions, cancellationToken);
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
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.</param>
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

			setTable(db.MappingSchema.GetFluentMappingBuilder().Entity<T>());

			return TempTable<T>.CreateAsync(db, tableName, items, databaseName, schemaName, action, serverName, tableOptions, cancellationToken);
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
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.</param>
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
				setTable?.Invoke(eq.DataContext.MappingSchema.GetFluentMappingBuilder().Entity<T>());
				return new TempTable<T>(eq.DataContext, items, tableName, databaseName, schemaName, action, serverName, tableOptions);
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
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.</param>
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
				setTable?.Invoke(eq.DataContext.MappingSchema.GetFluentMappingBuilder().Entity<T>());
				return TempTable<T>.CreateAsync(eq.DataContext, items, tableName, databaseName, schemaName, action, serverName, tableOptions, cancellationToken);
			}

			throw new ArgumentException($"The '{nameof(items)}' argument must be of type 'LinqToDB.Linq.IExpressionQuery'.");
		}

		#endregion
	}
}
