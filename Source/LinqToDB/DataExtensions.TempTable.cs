using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

namespace LinqToDB
{
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
			this IDataContext db,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = TableOptions.IsTemporary)
			where T : class
		{
			return db.CreateTempTable<T>(
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions));
		}

		/// <summary>
		/// Creates new temporary table.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext       db,
			CreateTempTableOptions? createOptions)
			where T : class
		{
			return new TempTable<T>(db, createOptions);
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
			return db.CreateTempTable(
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				options);
		}

		/// <summary>
		/// Creates new temporary table.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="bulkCopyOptions">Optional BulkCopy options.</param>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext       db,
			CreateTempTableOptions? createOptions,
			IEnumerable<T>          items,
			BulkCopyOptions?        bulkCopyOptions = default)
			where T : class
		{
			return new TempTable<T>(db, createOptions, items, bulkCopyOptions);
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
			return db.CreateTempTable(
				new CreateTempTableOptions(
					TableName   :tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				setTable,
				options);
		}
		
		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="bulkCopyOptions">Optional BulkCopy options.</param>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext               db,
			CreateTempTableOptions?         createOptions,
			IEnumerable<T>                  items,
			Action<EntityMappingBuilder<T>> setTable,
			BulkCopyOptions?                bulkCopyOptions = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(setTable);

			var tempTableDescriptor = GetTempTableDescriptor(db, setTable);
			
			return new TempTable<T>(db, tempTableDescriptor, createOptions, items, bulkCopyOptions);
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
			return db.CreateTempTable(
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				options);
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
			return db.CreateTempTable(
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				setTable,
				options);
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
			return db.CreateTempTable(
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				action);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext       db,
			CreateTempTableOptions? createOptions,
			IQueryable<T>           items,
			Action<ITable<T>>?      action = default)
			where T : class
		{
			return new TempTable<T>(db, createOptions, items, action);
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
			return db.CreateTempTable(
				new CreateTempTableOptions(
					TableName   :tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				setTable,
				action);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		public static TempTable<T> CreateTempTable<T>(
			this IDataContext               db,
			CreateTempTableOptions?         createOptions,
			IQueryable<T>                   items,
			Action<EntityMappingBuilder<T>> setTable,
			Action<ITable<T>>?              action = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(setTable);

			var tempTableDescriptor = GetTempTableDescriptor(db, setTable);

			return new TempTable<T>(db, tempTableDescriptor, createOptions, items, action);
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
			return db.CreateTempTable(
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				action);
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
			return db.CreateTempTable(
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				setTable,
				action);
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
			return db.CreateTempTableAsync<T>(
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
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext       db,
			CreateTempTableOptions? createOptions,
			CancellationToken       cancellationToken = default)
			where T : class
		{
			return TempTable<T>.CreateAsync(db, createOptions, cancellationToken);
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
			return db.CreateTempTableAsync(
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
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext       db,
			CreateTempTableOptions? createOptions,
			IEnumerable<T>          items,
			BulkCopyOptions?        bulkCopyOptions   = default,
			CancellationToken       cancellationToken = default)
			where T : class
		{
			return TempTable<T>.CreateAsync(db, createOptions, items, bulkCopyOptions, cancellationToken);
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
			return db.CreateTempTableAsync(
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				setTable,
				options,
				cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="bulkCopyOptions">Optional BulkCopy options.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext               db,
			CreateTempTableOptions?         createOptions,
			IEnumerable<T>                  items,
			Action<EntityMappingBuilder<T>> setTable,
			BulkCopyOptions?                bulkCopyOptions   = default,
			CancellationToken               cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(setTable);

			var tempTableDescriptor = GetTempTableDescriptor(db, setTable);

			return TempTable<T>.CreateAsync(db, tempTableDescriptor, createOptions, items, bulkCopyOptions, cancellationToken);
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
			return db.CreateTempTableAsync(
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
			return db.CreateTempTableAsync(
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				setTable,
				options,
				cancellationToken);
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
			this IDataContext      db,
			IQueryable<T>          items,
			string?                tableName         = default,
			string?                databaseName      = default,
			string?                schemaName        = default,
			Func<ITable<T>, Task>? action            = default,
			string?                serverName        = default,
			TableOptions           tableOptions      = TableOptions.IsTemporary,
			CancellationToken      cancellationToken = default)
			where T : class
		{
			return db.CreateTempTableAsync(
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
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext       db,
			CreateTempTableOptions? createOptions,
			IQueryable<T>           items,
			Func<ITable<T>, Task>?  action            = default,
			CancellationToken       cancellationToken = default)
			where T : class
		{
			return TempTable<T>.CreateAsync(db, createOptions, items, action, cancellationToken);
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
			Func<ITable<T>, Task>?          action            = default,
			string?                         serverName        = default,
			TableOptions                    tableOptions      = TableOptions.IsTemporary,
			CancellationToken               cancellationToken = default)
			where T : class
		{
			return db.CreateTempTableAsync(
				new CreateTempTableOptions(
					TableName   :tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				setTable,
				action,
				cancellationToken);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.
		/// Note that context mapping schema must be writable to allow it.
		/// You can enable writable <see cref="MappingSchema"/> using <see cref="DataOptionsExtensions.UseEnableContextSchemaEdit(DataOptions, bool)"/> configuration helper
		/// or enable writeable schemata globally using <see cref="Common.Configuration.Linq.EnableContextSchemaEdit" /> option.
		/// Latter option is not recommended as it will affect performance significantly.</param>
		/// <param name="action">Optional asynchronous action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public static Task<TempTable<T>> CreateTempTableAsync<T>(
			this IDataContext               db,
			CreateTempTableOptions?         createOptions,
			IQueryable<T>                   items,
			Action<EntityMappingBuilder<T>> setTable,
			Func<ITable<T>, Task>?          action            = default,
			CancellationToken               cancellationToken = default)
			 where T : class
		{
			ArgumentNullException.ThrowIfNull(setTable);

			var tempTableDescriptor = GetTempTableDescriptor(db, setTable);

			return TempTable<T>.CreateAsync(db, tempTableDescriptor, createOptions, items, action, cancellationToken);
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
			this IDataContext      db,
			string?                tableName,
			IQueryable<T>          items,
			string?                databaseName      = default,
			string?                schemaName        = default,
			Func<ITable<T>, Task>? action            = default,
			string?                serverName        = default,
			TableOptions           tableOptions      = TableOptions.IsTemporary,
			CancellationToken      cancellationToken = default)
			where T : class
		{
			return db.CreateTempTableAsync(
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
			Func<ITable<T>, Task>?          action            = default,
			string?                         serverName        = default,
			TableOptions                    tableOptions      = TableOptions.IsTemporary,
			CancellationToken               cancellationToken = default)
			where T : class
		{
			return db.CreateTempTableAsync(
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				setTable,
				action,
				cancellationToken);
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
			return items.IntoTempTable(
				db,
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				options);
		}

		/// <summary>
		/// Creates new temporary table.
		/// </summary>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="bulkCopyOptions">Optional BulkCopy options.</param>
		public static TempTable<T> IntoTempTable<T>(
			this IEnumerable<T>     items,
			IDataContext            db,
			CreateTempTableOptions? createOptions,
			BulkCopyOptions?        bulkCopyOptions = default)
			where T : class
		{
			return new TempTable<T>(db, createOptions, items, bulkCopyOptions);
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
			if (items is not IExpressionQuery eq)
			{
				throw new ArgumentException(
					$"The '{nameof(items)}' argument must be of type 'LinqToDB.Linq.IExpressionQuery'.",
					nameof(items)
				);
			}

			TempTableDescriptor? tempTableDescriptor = null;
			if (setTable != null)
				tempTableDescriptor = GetTempTableDescriptor(eq.DataContext, setTable);

			return new TempTable<T>(
				eq.DataContext,
				tempTableDescriptor,
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				items,
				action);
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
			return items.IntoTempTableAsync(
				db,
				new CreateTempTableOptions(
					TableName   : tableName,
					DatabaseName: databaseName,
					SchemaName  : schemaName,
					ServerName  : serverName,
					TableOptions: tableOptions),
				options,
				cancellationToken);
		}
		
		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="db">Database connection instance.</param>
		/// <param name="createOptions">Options for temporary table creation.</param>
		/// <param name="bulkCopyOptions">Optional BulkCopy options.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public static Task<TempTable<T>> IntoTempTableAsync<T>(
			this IEnumerable<T>     items,
			IDataContext            db,
			CreateTempTableOptions? createOptions,
			BulkCopyOptions?        bulkCopyOptions   = default,
			CancellationToken       cancellationToken = default)
			where T : class
		{
			return TempTable<T>.CreateAsync(db, createOptions, items, bulkCopyOptions, cancellationToken);
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
			Func<ITable<T>, Task>?           action            = default,
			Action<EntityMappingBuilder<T>>? setTable          = default,
			CancellationToken                cancellationToken = default)
			where T : class
		{
			if (items is not IExpressionQuery eq)
			{
				throw new ArgumentException(
					$"The '{nameof(items)}' argument must be of type 'LinqToDB.Linq.IExpressionQuery'.",
					nameof(items)
				);
			}

			TempTableDescriptor? tempTableDescriptor = null;
			if (setTable != null)
				tempTableDescriptor = GetTempTableDescriptor(eq.DataContext, setTable);

			return TempTable<T>.CreateAsync(
				eq.DataContext,
				tempTableDescriptor,
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

		private static TempTableDescriptor GetTempTableDescriptor<T>(IDataContext dataContext, Action<EntityMappingBuilder<T>> setTable)
		{
			var oldSchema = dataContext.MappingSchema;
			var newSchema = new MappingSchema("#TempTable", dataContext.MappingSchema);
			var builder   = new FluentMappingBuilder(newSchema);

			setTable(builder.Entity<T>());
			builder.Build();

			dataContext.SetMappingSchema(newSchema);

			return new TempTableDescriptor(
				newSchema.GetEntityDescriptor(typeof(T), dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated),
				oldSchema);
		}

		#endregion
	}
}
