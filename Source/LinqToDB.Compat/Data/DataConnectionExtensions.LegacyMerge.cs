using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.DataProvider.DB2;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.DataProvider.Sybase;

// ReSharper disable PossibleMultipleEnumeration

namespace LinqToDB.Data
{
	/// <summary>
	/// Contains extension methods for <see cref="DataConnection"/> class.
	/// </summary>
	public static class DataConnectionExtensions
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

		class IdentitySupporter<T>(ITable<T> table) : IDisposable
			where T : class
		{
			public static IdentitySupporter<T>? Create(ITable<T> table, MergeOptions mergeOptions)
			{
				if (mergeOptions is MergeOptions.DoNotKeepIdentity)
					return null;

				var entity      = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T));
				var hasIdentity = entity.Columns.Any(c => c.IsIdentity);

				return hasIdentity ? SetIdentity(table, mergeOptions, true) : null;
			}

			static IdentitySupporter<T>? SetIdentity(ITable<T> table, MergeOptions mergeOptions, bool setOn)
			{
				var command = GetCommand(table, mergeOptions, setOn);

				if (command != null)
				{
					((DataConnection)table.DataContext).Execute(command);
					return setOn ? new IdentitySupporter<T>(table) : null;
				}

				return null;
			}

			internal static string? GetCommand(ITable<T> table, MergeOptions mergeOptions, bool setOn)
			{
				if (table.DataContext is DataConnection dc)
				{
					var tableName = table.GetTableName();
					var onOff     = setOn ? "ON" : "OFF";

					switch (dc)
					{
						case { DataProvider: DB2DataProvider       } : return $"SET IDENTITY_OVERRIDE {onOff} FOR {tableName}";
						case { DataProvider: SqlServerDataProvider } :
						case { DataProvider: SybaseDataProvider    } : return $"SET IDENTITY_INSERT {tableName} {onOff}";
					}
				}

				return null;
			}

			public void Dispose()
			{
				SetIdentity(table, default, false);
			}
		}

		class IdentitySupporterAsync<T>(ITable<T> table) : IAsyncDisposable
			where T : class
		{
			public static Task<IdentitySupporterAsync<T>?> CreateAsync(ITable<T> table)
			{
				var entity      = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T));
				var hasIdentity = entity.Columns.Any(c => c.IsIdentity);

				return hasIdentity ? SetIdentityAsync(table, true) : Task.FromResult<IdentitySupporterAsync<T>?>(null);
			}

			static async Task<IdentitySupporterAsync<T>?> SetIdentityAsync(ITable<T> table, bool setOn)
			{
				var command = IdentitySupporter<T>.GetCommand(table, default, setOn);

				if (command != null)
				{
					await ((DataConnection)table.DataContext).ExecuteAsync(command).ConfigureAwait(false);
					return setOn ? new IdentitySupporterAsync<T>(table) : null;
				}

				return null;
			}

			public async ValueTask DisposeAsync()
			{
				await SetIdentityAsync(table, false).ConfigureAwait(false);
			}
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
		public static int MergeData<T>(
			this DataConnection      dataConnection,
			IQueryable<T>            source,
			Expression<Func<T,bool>> predicate,
			string?                  tableName    = default,
			string?                  databaseName = default,
			string?                  schemaName   = default,
			string?                  serverName   = default,
			TableOptions             tableOptions = default,
			MergeOptions             mergeOptions = default
		)
			where T : class
		{
			return MergeData(dataConnection.GetTable<T>(), source, predicate, tableName, databaseName, schemaName, serverName, tableOptions, mergeOptions);
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
		public static int MergeData<T>(
			this DataConnection      dataConnection,
			Expression<Func<T,bool>> predicate,
			IEnumerable<T>           source,
			string?                  tableName    = default,
			string?                  databaseName = default,
			string?                  schemaName   = default,
			string?                  serverName   = default,
			TableOptions             tableOptions = default,
			MergeOptions             mergeOptions = default
		)
			where T : class
		{
			return MergeData(dataConnection.GetTable<T>(), predicate, source, tableName, databaseName, schemaName, serverName, tableOptions, mergeOptions);
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
		public static int MergeData<T>(
			this DataConnection dataConnection,
			bool                delete,
			IEnumerable<T>      source,
			string?             tableName    = default,
			string?             databaseName = default,
			string?             schemaName   = default,
			string?             serverName   = default,
			TableOptions        tableOptions = default,
			MergeOptions        mergeOptions = default
		)
			where T : class
		{
			return MergeData(dataConnection.GetTable<T>(), delete, source, tableName, databaseName, schemaName, serverName, tableOptions, mergeOptions);
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
		public static int MergeData<T>(
			this DataConnection dataConnection,
			IEnumerable<T>      source,
			string?             tableName    = default,
			string?             databaseName = default,
			string?             schemaName   = default,
			string?             serverName   = default,
			TableOptions        tableOptions = default,
			MergeOptions        mergeOptions = default
		)
			where T : class
		{
			return MergeData(dataConnection.GetTable<T>(), source, tableName, databaseName, schemaName, serverName, tableOptions, mergeOptions);
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
		public static int MergeData<T>(
			this ITable<T>           table,
			IQueryable<T>            source,
			Expression<Func<T,bool>> predicate,
			string?                  tableName    = default,
			string?                  databaseName = default,
			string?                  schemaName   = default,
			string?                  serverName   = default,
			TableOptions             tableOptions = default,
			MergeOptions             mergeOptions = default
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

			using var _ = IdentitySupporter<T>.Create(table, mergeOptions);

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
		public static int MergeData<T>(
			this ITable<T>           table,
			Expression<Func<T,bool>> predicate,
			IEnumerable<T>           source,
			string?                  tableName    = default,
			string?                  databaseName = default,
			string?                  schemaName   = default,
			string?                  serverName   = default,
			TableOptions             tableOptions = default,
			MergeOptions             mergeOptions = default
		)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);

#if NET6_0_OR_GREATER
			if (source.TryGetNonEnumeratedCount(out var count))
			{
				if (count == 0)
					return 0;
			}
			else
#endif
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

			using var _ = IdentitySupporter<T>.Create(target, mergeOptions);

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
		public static int MergeData<T>(
			this ITable<T> table,
			bool           delete,
			IEnumerable<T> source,
			string?        tableName    = default,
			string?        databaseName = default,
			string?        schemaName   = default,
			string?        serverName   = default,
			TableOptions   tableOptions = default,
			MergeOptions   mergeOptions = default
		)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);

#if NET6_0_OR_GREATER
			if (source.TryGetNonEnumeratedCount(out var count))
			{
				if (count == 0)
					return 0;
			}
			else
#endif
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

			Linq.IMergeable<T,T>? merge = null;

			if (withUpdate) merge = query.UpdateWhenMatched();
			                merge = (merge ?? query).InsertWhenNotMatched();
			if (delete)     merge = merge.DeleteWhenNotMatchedBySource();

			using var _ = IdentitySupporter<T>.Create(table, mergeOptions);

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
		public static int MergeData<T>(
			this ITable<T> table,
			IEnumerable<T> source,
			string?        tableName    = default,
			string?        databaseName = default,
			string?        schemaName   = default,
			string?        serverName   = default,
			TableOptions   tableOptions = default,
			MergeOptions   mergeOptions = default
		)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);

#if NET6_0_OR_GREATER
			if (source.TryGetNonEnumeratedCount(out var count))
			{
				if (count == 0)
					return 0;
			}
			else
#endif
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

			using var _ = IdentitySupporter<T>.Create(table, mergeOptions);

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
		public static Task<int> MergeDataAsync<T>(
			this DataConnection      dataConnection,
			IQueryable<T>            source,
			Expression<Func<T,bool>> predicate,
			string?                  tableName         = default,
			string?                  databaseName      = default,
			string?                  schemaName        = default,
			string?                  serverName        = default,
			TableOptions             tableOptions      = default,
			MergeOptions             mergeOptions      = default,
			CancellationToken        cancellationToken = default)
			where T : class
		{
			return MergeDataAsync(dataConnection.GetTable<T>(), source, predicate, tableName, databaseName, schemaName, serverName, tableOptions, mergeOptions, cancellationToken);
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
		public static Task<int> MergeDataAsync<T>(
			this DataConnection      dataConnection,
			Expression<Func<T,bool>> predicate,
			IEnumerable<T>           source,
			string?                  tableName         = default,
			string?                  databaseName      = default,
			string?                  schemaName        = default,
			string?                  serverName        = default,
			TableOptions             tableOptions      = default,
			MergeOptions             mergeOptions      = default,
			CancellationToken        cancellationToken = default)
			where T : class
		{
			return MergeDataAsync(dataConnection.GetTable<T>(), predicate, source, tableName, databaseName, schemaName, serverName, tableOptions, mergeOptions, cancellationToken);
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
		public static Task<int> MergeDataAsync<T>(
			this DataConnection dataConnection,
			bool                delete,
			IEnumerable<T>      source,
			string?             tableName         = default,
			string?             databaseName      = default,
			string?             schemaName        = default,
			string?             serverName        = default,
			TableOptions        tableOptions      = default,
			MergeOptions        mergeOptions      = default,
			CancellationToken   cancellationToken = default)
			where T : class
		{
			return MergeDataAsync(dataConnection.GetTable<T>(), delete, source, tableName, databaseName, schemaName, serverName, tableOptions, mergeOptions, cancellationToken);
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
		public static Task<int> MergeDataAsync<T>(
			this DataConnection dataConnection,
			IEnumerable<T>      source,
			string?             tableName         = default,
			string?             databaseName      = default,
			string?             schemaName        = default,
			string?             serverName        = default,
			TableOptions        tableOptions      = default,
			MergeOptions        mergeOptions      = default,
			CancellationToken   cancellationToken = default)
			where T : class
		{
			return MergeDataAsync(dataConnection.GetTable<T>(), source, tableName, databaseName, schemaName, serverName, tableOptions, mergeOptions, cancellationToken);
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
		public static async Task<int> MergeDataAsync<T>(
			this ITable<T>           table,
			IQueryable<T>            source,
			Expression<Func<T,bool>> predicate,
			string?                  tableName         = default,
			string?                  databaseName      = default,
			string?                  schemaName        = default,
			string?                  serverName        = default,
			TableOptions             tableOptions      = default,
			MergeOptions             mergeOptions      = default,
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

			await using var _ = await IdentitySupporterAsync<T>.CreateAsync(table).ConfigureAwait(false);

			return await query
				.InsertWhenNotMatched()
				.DeleteWhenNotMatchedBySourceAnd(predicate)
				.MergeAsync(cancellationToken)
				.ConfigureAwait(false);
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
		public static async Task<int> MergeDataAsync<T>(
			this ITable<T>           table,
			Expression<Func<T,bool>> predicate,
			IEnumerable<T>           source,
			string?                  tableName         = default,
			string?                  databaseName      = default,
			string?                  schemaName        = default,
			string?                  serverName        = default,
			TableOptions             tableOptions      = default,
			MergeOptions             mergeOptions      = default,
			CancellationToken        cancellationToken = default)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);

#if NET6_0_OR_GREATER
			if (source.TryGetNonEnumeratedCount(out var count))
			{
				if (count == 0)
					return 0;
			}
			else
#endif
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

			await using var _ = await IdentitySupporterAsync<T>.CreateAsync(table).ConfigureAwait(false);

			return await query
				.InsertWhenNotMatched()
				.DeleteWhenNotMatchedBySourceAnd(predicate)
				.MergeAsync(cancellationToken)
				.ConfigureAwait(false)
				;
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
		public static async Task<int> MergeDataAsync<T>(
			this ITable<T>    table,
			bool              delete,
			IEnumerable<T>    source,
			string?           tableName         = default,
			string?           databaseName      = default,
			string?           schemaName        = default,
			string?           serverName        = default,
			TableOptions      tableOptions      = default,
			MergeOptions      mergeOptions      = default,
			CancellationToken cancellationToken = default)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);

#if NET6_0_OR_GREATER
			if (source.TryGetNonEnumeratedCount(out var count))
			{
				if (count == 0)
					return 0;
			}
			else
#endif
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

			await using var _ = await IdentitySupporterAsync<T>.CreateAsync(table).ConfigureAwait(false);

			return await merge.MergeAsync(cancellationToken).ConfigureAwait(false);
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
		public static async Task<int> MergeDataAsync<T>(
			this ITable<T>    table,
			IEnumerable<T>    source,
			string?           tableName         = default,
			string?           databaseName      = default,
			string?           schemaName        = default,
			string?           serverName        = default,
			TableOptions      tableOptions      = default,
			MergeOptions      mergeOptions      = default,
			CancellationToken cancellationToken = default)
			where T : class
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var withUpdate = MergeWithUpdate(table);

#if NET6_0_OR_GREATER
			if (source.TryGetNonEnumeratedCount(out var count))
			{
				if (count == 0)
					return 0;
			}
			else
#endif
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

			await using var _ = await IdentitySupporterAsync<T>.CreateAsync(table).ConfigureAwait(false);

			return await query
				.InsertWhenNotMatched()
				.MergeAsync(cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
