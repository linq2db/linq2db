using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;
using LinqToDB.Model;

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

			var tempTableDescriptor = GetTempTableDescriptor(db.MappingSchema, db.Options, setTable);

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

			var tempTableDescriptor = GetTempTableDescriptor(db.MappingSchema, db.Options, setTable);

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

			var tempTableDescriptor = GetTempTableDescriptor(db.MappingSchema, db.Options, setTable);

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

			var tempTableDescriptor = GetTempTableDescriptor(db.MappingSchema, db.Options, setTable);

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

			var tempTableDescriptor = GetTempTableDescriptor(db.MappingSchema, db.Options, setTable);

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

			var tempTableDescriptor = GetTempTableDescriptor(db.MappingSchema, db.Options, setTable);

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

			var tempTableDescriptor = GetTempTableDescriptor(db.MappingSchema, db.Options, setTable);

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

			var tempTableDescriptor = GetTempTableDescriptor(db.MappingSchema, db.Options, setTable);

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
					tempTableDescriptor = GetTempTableDescriptor(eq.DataContext.MappingSchema, eq.DataContext.Options, setTable);

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
					tempTableDescriptor = GetTempTableDescriptor(eq.DataContext.MappingSchema, eq.DataContext.Options, setTable);

				return TempTable<T>.CreateAsync(eq.DataContext, tempTableDescriptor, items, tableName, databaseName, schemaName, action, serverName, tableOptions, cancellationToken);
			}

			throw new ArgumentException($"The '{nameof(items)}' argument must be of type 'LinqToDB.Linq.IExpressionQuery'.");
		}

		private static EntityDescriptor GetTempTableDescriptor<T>(MappingSchema contextSchema, DataOptions options, Action<EntityMappingBuilder<T>> setTable)
		{
			var ms = new MappingSchema(contextSchema);
			var builder = new FluentMappingBuilder(ms);
			setTable(builder.Entity<T>());
			builder.Build();
			return ms.GetEntityDescriptor(typeof(T), options.ConnectionOptions.OnEntityDescriptorCreated);
		}

		#endregion
	}
}
