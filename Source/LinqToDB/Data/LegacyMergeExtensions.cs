using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Data
{
	/// <summary>
	/// Contains extension methods for <see cref="DataConnection"/> class.
	/// </summary>
	[Obsolete("Legacy Merge API obsoleted and not recommended for use. See migration guide https://linq2db.github.io/articles/sql/merge/Merge-API-Migration.html or direct translation of old API to new one in code of this method https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Data/LegacyMergeExtensions.cs.")]
	public static class LegacyMergeExtensions
	{
		private static bool MergeWithUpdate<T>(ITable<T> table)
			where T : class
		{
			return table.DataContext
				.MappingSchema
				.GetEntityDescriptor(typeof(T), table.DataContext.Options.ConnectionOptions.OnEntityDescriptorCreated)
				.Columns
				.Any(c => c is { IsPrimaryKey: false, IsIdentity: false, SkipOnUpdate: false });
		}

		/// <summary>
		/// Executes following merge operations in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source.
		/// Method could be used only with SQL Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataConnection">Data connection instance.</param>
		/// <param name="source">Source data to merge into target table. All source data will be loaded from server for command generation.</param>
		/// <param name="predicate">Filter, applied both to source and delete operation. Required.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Returns number of affected target records.</returns>
		// don't remove without consent from @IT
		public static int Merge<T>(
			this DataConnection      dataConnection,
			IQueryable<T>            source,
			Expression<Func<T,bool>> predicate,
			string?                  tableName    = default,
			string?                  databaseName = default,
			string?                  schemaName   = default,
			string?                  serverName   = default,
			TableOptions             tableOptions = default
		)
			where T : class
		{
			return Merge(dataConnection.GetTable<T>(), source, predicate, tableName, databaseName, schemaName, serverName, tableOptions);
		}

		/// <summary>
		/// Executes following merge operations in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source.
		/// Method could be used only with SQL Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataConnection">Data connection instance.</param>
		/// <param name="predicate">Filter, applied to delete operation. Optional.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Returns number of affected target records.</returns>
		// don't remove without consent from @IT
		public static int Merge<T>(
			this DataConnection      dataConnection,
			Expression<Func<T,bool>> predicate,
			IEnumerable<T>           source,
			string?                  tableName    = default,
			string?                  databaseName = default,
			string?                  schemaName   = default,
			string?                  serverName   = default,
			TableOptions             tableOptions = default
		)
			where T : class
		{
			return Merge(dataConnection.GetTable<T>(), predicate, source, tableName, databaseName, schemaName, serverName, tableOptions);
		}

		/// <summary>
		/// Executes following merge operations in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source (optional).
		/// If delete operation enabled by <paramref name="delete"/> parameter - method could be used only for with Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataConnection">Data connection instance.</param>
		/// <param name="delete">If true, merge command will include delete by source operation without condition.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Returns number of affected target records.</returns>
		// don't remove without consent from @IT
		public static int Merge<T>(
			this DataConnection dataConnection,
			bool                delete,
			IEnumerable<T>      source,
			string?             tableName    = default,
			string?             databaseName = default,
			string?             schemaName   = default,
			string?             serverName   = default,
			TableOptions        tableOptions = default
		)
			where T : class
		{
			return Merge(dataConnection.GetTable<T>(), delete, source, tableName, databaseName, schemaName, serverName, tableOptions);
		}

		/// <summary>
		/// Executes following merge operations in specified order:
		/// - Update
		/// - Insert.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataConnection">Data connection instance.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Returns number of affected target records.</returns>
		// don't remove without consent from @IT
		public static int Merge<T>(
			this DataConnection dataConnection,
			IEnumerable<T>      source,
			string?             tableName    = default,
			string?             databaseName = default,
			string?             schemaName   = default,
			string?             serverName   = default,
			TableOptions        tableOptions = default
		)
			where T : class
		{
			return Merge(dataConnection.GetTable<T>(), source, tableName, databaseName, schemaName, serverName, tableOptions);
		}

		/// <summary>
		/// Executes following merge operations in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source.
		/// Method could be used only with SQL Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="source">Source data to merge into target table. All source data will be loaded from server for command generation.</param>
		/// <param name="predicate">Filter, applied both to source and delete operation. Required.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Returns number of affected target records.</returns>
		// don't remove without consent from @IT
		public static int Merge<T>(
			this ITable<T>           table,
			IQueryable<T>            source,
			Expression<Func<T,bool>> predicate,
			string?                  tableName    = default,
			string?                  databaseName = default,
			string?                  schemaName   = default,
			string?                  serverName   = default,
			TableOptions             tableOptions = default
		)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);
			var target     = table;

			if (tableName    != null) target = target.TableName   (tableName);
			if (databaseName != null) target = target.DatabaseName(databaseName);
			if (schemaName   != null) target = target.SchemaName  (schemaName);
			if (serverName   != null) target = target.ServerName  (serverName);
			if (tableOptions.IsSet()) target = target.TableOptions(tableOptions);

			var query = target
				.Merge()
				.Using(source.Where(predicate).AsEnumerable())
				.OnTargetKey();

			if (withUpdate)
				query = query.UpdateWhenMatched();

			return query
				.InsertWhenNotMatched()
				.DeleteWhenNotMatchedBySourceAnd(predicate)
				.Merge();
		}

		/// <summary>
		/// Executes following merge operations in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source.
		/// Method could be used only with SQL Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="predicate">Filter, applied to delete operation. Optional.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Returns number of affected target records.</returns>
		// don't remove without consent from @IT
		public static int Merge<T>(
			this ITable<T>           table,
			Expression<Func<T,bool>> predicate,
			IEnumerable<T>           source,
			string?                  tableName    = default,
			string?                  databaseName = default,
			string?                  schemaName   = default,
			string?                  serverName   = default,
			TableOptions             tableOptions = default
		)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);

			if (!source.Any())
				return 0;

			var target = table;

			if (tableName    != null) target = target.TableName   (tableName);
			if (databaseName != null) target = target.DatabaseName(databaseName);
			if (schemaName   != null) target = target.SchemaName  (schemaName);
			if (serverName   != null) target = target.ServerName  (serverName);
			if (tableOptions.IsSet()) target = target.TableOptions(tableOptions);

			var query = target
				.Merge()
				.Using(source)
				.OnTargetKey();

			if (withUpdate)
				query = query.UpdateWhenMatched();

			return query
				.InsertWhenNotMatched()
				.DeleteWhenNotMatchedBySourceAnd(predicate)
				.Merge();
		}

		/// <summary>
		/// Executes following merge operations in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source (optional).
		/// If delete operation enabled by <paramref name="delete"/> parameter - method could be used only with SQL Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="delete">If true, merge command will include delete by source operation without condition.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Returns number of affected target records.</returns>
		// don't remove without consent from @IT
		public static int Merge<T>(
			this ITable<T> table,
			bool           delete,
			IEnumerable<T> source,
			string?        tableName    = default,
			string?        databaseName = default,
			string?        schemaName   = default,
			string?        serverName   = default,
			TableOptions   tableOptions = default
		)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);

			if (!source.Any())
				return 0;

			var target = table;

			if (tableName    != null) target = target.TableName   (tableName);
			if (databaseName != null) target = target.DatabaseName(databaseName);
			if (schemaName   != null) target = target.SchemaName  (schemaName);
			if (serverName   != null) target = target.ServerName  (serverName);
			if (tableOptions.IsSet()) target = target.TableOptions(tableOptions);

			var query = target
				.Merge()
				.Using(source)
				.OnTargetKey();

			Linq.IMergeable<T, T>? merge = null;
			if (withUpdate) merge = query.UpdateWhenMatched();
			                merge = (merge ?? query).InsertWhenNotMatched();
			if (delete)     merge = merge.DeleteWhenNotMatchedBySource();

			return merge.Merge();
		}

		/// <summary>
		/// Executes following merge operations in specified order:
		/// - Update
		/// - Insert.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Returns number of affected target records.</returns>
		// don't remove without consent from @IT
		public static int Merge<T>(
			this ITable<T> table,
			IEnumerable<T> source,
			string?        tableName    = default,
			string?        databaseName = default,
			string?        schemaName   = default,
			string?        serverName   = default,
			TableOptions   tableOptions = default
		)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);

			if (!source.Any())
				return 0;

			var target = table;

			if (tableName    != null) target = target.TableName   (tableName);
			if (databaseName != null) target = target.DatabaseName(databaseName);
			if (schemaName   != null) target = target.SchemaName  (schemaName);
			if (serverName   != null) target = target.ServerName  (serverName);
			if (tableOptions.IsSet()) target = target.TableOptions(tableOptions);

			var query = target
				.Merge()
				.Using(source)
				.OnTargetKey();

			if (withUpdate)
				query = query.UpdateWhenMatched();

			return query
				.InsertWhenNotMatched()
				.Merge();
		}

		/// <summary>
		/// Executes following merge operations asynchronously in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source.
		/// Method could be used only with SQL Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataConnection">Data connection instance.</param>
		/// <param name="source">Source data to merge into target table. All source data will be loaded from server for command generation.</param>
		/// <param name="predicate">Filter, applied both to source and delete operation. Required.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		// don't remove without consent from @IT
		public static Task<int> MergeAsync<T>(
			this DataConnection      dataConnection,
			IQueryable<T>            source,
			Expression<Func<T,bool>> predicate,
			string?                  tableName         = default,
			string?                  databaseName      = default,
			string?                  schemaName        = default,
			string?                  serverName        = default,
			TableOptions             tableOptions      = default,
			CancellationToken        cancellationToken = default)
			where T : class
		{
			return MergeAsync(dataConnection.GetTable<T>(), source, predicate, tableName, databaseName, schemaName, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Executes following merge operations asynchronously in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source.
		/// Method could be used only with SQL Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataConnection">Data connection instance.</param>
		/// <param name="predicate">Filter, applied to delete operation. Optional.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		// don't remove without consent from @IT
		public static Task<int> MergeAsync<T>(
			this DataConnection      dataConnection,
			Expression<Func<T,bool>> predicate,
			IEnumerable<T>           source,
			string?                  tableName         = default,
			string?                  databaseName      = default,
			string?                  schemaName        = default,
			string?                  serverName        = default,
			TableOptions             tableOptions      = default,
			CancellationToken        cancellationToken = default)
			where T : class
		{
			return MergeAsync(dataConnection.GetTable<T>(), predicate, source, tableName, databaseName, schemaName, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Executes following merge operations asynchronously in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source (optional).
		/// If delete operation enabled by <paramref name="delete"/> parameter - method could be used only with SQL Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataConnection">Data connection instance.</param>
		/// <param name="delete">If true, merge command will include delete by source operation without condition.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		// don't remove without consent from @IT
		public static Task<int> MergeAsync<T>(
			this DataConnection dataConnection,
			bool                delete,
			IEnumerable<T>      source,
			string?             tableName         = default,
			string?             databaseName      = default,
			string?             schemaName        = default,
			string?             serverName        = default,
			TableOptions        tableOptions      = default,
			CancellationToken   cancellationToken = default)
			where T : class
		{
			return MergeAsync(dataConnection.GetTable<T>(), delete, source, tableName, databaseName, schemaName, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Executes following merge operations asynchronously in specified order:
		/// - Update
		/// - Insert.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataConnection">Data connection instance.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		// don't remove without consent from @IT
		public static Task<int> MergeAsync<T>(
			this DataConnection dataConnection,
			IEnumerable<T>      source,
			string?             tableName         = default,
			string?             databaseName      = default,
			string?             schemaName        = default,
			string?             serverName        = default,
			TableOptions        tableOptions      = default,
			CancellationToken   cancellationToken = default)
			where T : class
		{
			return MergeAsync(dataConnection.GetTable<T>(), source, tableName, databaseName, schemaName, serverName, tableOptions, cancellationToken);
		}

		/// <summary>
		/// Executes following merge operations asynchronously in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source.
		/// Method could be used only with SQL Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="source">Source data to merge into target table. All source data will be loaded from server for command generation.</param>
		/// <param name="predicate">Filter, applied both to source and delete operation. Required.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		// don't remove without consent from @IT
		public static Task<int> MergeAsync<T>(
			this ITable<T>           table,
			IQueryable<T>            source,
			Expression<Func<T,bool>> predicate,
			string?                  tableName         = default,
			string?                  databaseName      = default,
			string?                  schemaName        = default,
			string?                  serverName        = default,
			TableOptions             tableOptions      = default,
			CancellationToken        cancellationToken = default)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);
			var target     = table;

			if (tableName    != null) target = target.TableName   (tableName);
			if (databaseName != null) target = target.DatabaseName(databaseName);
			if (schemaName   != null) target = target.SchemaName  (schemaName);
			if (serverName   != null) target = target.ServerName  (serverName);
			if (tableOptions.IsSet()) target = target.TableOptions(tableOptions);

			var query = target
				.Merge()
				.Using(source.Where(predicate).AsEnumerable())
				.OnTargetKey();

			if (withUpdate)
				query = query.UpdateWhenMatched();

			return query
				.InsertWhenNotMatched()
				.DeleteWhenNotMatchedBySourceAnd(predicate)
				.MergeAsync(cancellationToken);
		}

		/// <summary>
		/// Executes following merge operations asynchronously in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source.
		/// Method could be used only with SQL Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="predicate">Filter, applied to delete operation. Optional.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		// don't remove without consent from @IT
		public static Task<int> MergeAsync<T>(
			this ITable<T>           table,
			Expression<Func<T,bool>> predicate,
			IEnumerable<T>           source,
			string?                  tableName         = default,
			string?                  databaseName      = default,
			string?                  schemaName        = default,
			string?                  serverName        = default,
			TableOptions             tableOptions      = default,
			CancellationToken        cancellationToken = default)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);

			if (!source.Any())
				return TaskCache.Zero;

			var target = table;

			if (tableName    != null) target = target.TableName   (tableName);
			if (databaseName != null) target = target.DatabaseName(databaseName);
			if (schemaName   != null) target = target.SchemaName  (schemaName);
			if (serverName   != null) target = target.ServerName  (serverName);
			if (tableOptions.IsSet()) target = target.TableOptions(tableOptions);

			var query = target
				.Merge()
				.Using(source)
				.OnTargetKey();

			if (withUpdate)
				query = query.UpdateWhenMatched();

			return query
				.InsertWhenNotMatched()
				.DeleteWhenNotMatchedBySourceAnd(predicate)
				.MergeAsync(cancellationToken);
		}

		/// <summary>
		/// Executes following merge operations asynchronously in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source (optional).
		/// If delete operation enabled by <paramref name="delete"/> parameter - method could be used only with SQL Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="delete">If true, merge command will include delete by source operation without condition.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		// don't remove without consent from @IT
		public static Task<int> MergeAsync<T>(
			this ITable<T>    table,
			bool              delete,
			IEnumerable<T>    source,
			string?           tableName         = default,
			string?           databaseName      = default,
			string?           schemaName        = default,
			string?           serverName        = default,
			TableOptions      tableOptions      = default,
			CancellationToken cancellationToken = default)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);

			if (!source.Any())
				return TaskCache.Zero;

			var target = table;

			if (tableName    != null) target = target.TableName   (tableName);
			if (databaseName != null) target = target.DatabaseName(databaseName);
			if (schemaName   != null) target = target.SchemaName  (schemaName);
			if (serverName   != null) target = target.ServerName  (serverName);
			if (tableOptions.IsSet()) target = target.TableOptions(tableOptions);

			var query = target
				.Merge()
				.Using(source)
				.OnTargetKey();

			Linq.IMergeable<T, T>? merge = null;
			if (withUpdate) merge = query.UpdateWhenMatched();
			                merge = (merge ?? query).InsertWhenNotMatched();
			if (delete)     merge = merge.DeleteWhenNotMatchedBySource();

			return merge.MergeAsync(cancellationToken);
		}

		/// <summary>
		/// Executes following merge operations asynchronously in specified order:
		/// - Update
		/// - Insert.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <param name="serverName">Optional name of linked server. If not specified, value from mapping will be used.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		// don't remove without consent from @IT
		public static Task<int> MergeAsync<T>(
			this ITable<T>    table,
			IEnumerable<T>    source,
			string?           tableName         = default,
			string?           databaseName      = default,
			string?           schemaName        = default,
			string?           serverName        = default,
			TableOptions      tableOptions      = default,
			CancellationToken cancellationToken = default)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);

			if (!source.Any())
				return TaskCache.Zero;

			var target = table;

			if (tableName    != null) target = target.TableName   (tableName);
			if (databaseName != null) target = target.DatabaseName(databaseName);
			if (schemaName   != null) target = target.SchemaName  (schemaName);
			if (serverName   != null) target = target.ServerName  (serverName);
			if (tableOptions.IsSet()) target = target.TableOptions(tableOptions);

			var query = target
				.Merge()
				.Using(source)
				.OnTargetKey();

			if (withUpdate)
				query = query.UpdateWhenMatched();

			return query
				.InsertWhenNotMatched()
				.MergeAsync(cancellationToken);
		}
	}
}
