using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Data
{
	using Linq;

	[PublicAPI]
	public static class DataConnectionExtensions
	{
		#region SetCommand

		public static CommandInfo SetCommand(this DataConnection dataConnection, string commandText)
		{
			return new CommandInfo(dataConnection, commandText);
		}

		public static CommandInfo SetCommand(DataConnection dataConnection, string commandText, params DataParameter[] parameters)
		{
			return new CommandInfo(dataConnection, commandText, parameters);
		}

		public static CommandInfo SetCommand(DataConnection dataConnection, string commandText, DataParameter parameter)
		{
			return new CommandInfo(dataConnection, commandText, parameter);
		}

		public static CommandInfo SetCommand(DataConnection dataConnection, string commandText, object parameters)
		{
			return new CommandInfo(dataConnection, commandText, parameters);
		}

		#endregion

		#region Query with object reader

		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql)
		{
			return new CommandInfo(connection, sql).Query(objectReader);
		}

		public static IEnumerable<T> QueryProc<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryProc(objectReader);
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).Query(objectReader);
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).Query(objectReader);
		}

		#endregion

		#region Query

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql)
		{
			return new CommandInfo(connection, sql).Query<T>();
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).Query<T>();
		}

		public static IEnumerable<T> QueryProc<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryProc<T>();
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql, DataParameter parameter)
		{
			return new CommandInfo(connection, sql, parameter).Query<T>();
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).Query<T>();
		}

		#endregion

		#region Query with template

		public static IEnumerable<T> Query<T>(this DataConnection connection, T template, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).Query(template);
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, T template, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).Query(template);
		}

		#endregion

		#region Execute

		public static int Execute(this DataConnection connection, string sql)
		{
			return new CommandInfo(connection, sql).Execute();
		}

		public static int Execute(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).Execute();
		}

		public static int ExecuteProc(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteProc();
		}

		public static int Execute(this DataConnection connection, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).Execute();
		}

		#endregion

		#region Execute scalar

		public static T Execute<T>(this DataConnection connection, string sql)
		{
			return new CommandInfo(connection, sql).Execute<T>();
		}

		public static T Execute<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).Execute<T>();
		}

		public static T ExecuteProc<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteProc<T>();
		}

		public static T Execute<T>(this DataConnection connection, string sql, DataParameter parameter)
		{
			return new CommandInfo(connection, sql, parameter).Execute<T>();
		}

		public static T Execute<T>(this DataConnection connection, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).Execute<T>();
		}

		#endregion

		#region ExecuteReader

		public static DataReader ExecuteReader(this DataConnection connection, string sql)
		{
			return new CommandInfo(connection, sql).ExecuteReader();
		}

		public static DataReader ExecuteReader(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteReader();
		}

		public static DataReader ExecuteReader(this DataConnection connection, string sql, DataParameter parameter)
		{
			return new CommandInfo(connection, sql, parameter).ExecuteReader();
		}

		public static DataReader ExecuteReader(this DataConnection connection, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteReader();
		}

		public static DataReader ExecuteReader(
			this DataConnection    connection,
			string                 sql,
			CommandType            commandType,
			CommandBehavior        commandBehavior,
			params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters)
			{
				CommandType     = commandType,
				CommandBehavior = commandBehavior,
			}.ExecuteReader();
		}

		#endregion

		#region BulkCopy

		public static BulkCopyRowsCopied BulkCopy<T>([NotNull] this DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");
			return dataConnection.DataProvider.BulkCopy(dataConnection, options, source);
		}

		public static BulkCopyRowsCopied BulkCopy<T>([NotNull] this DataConnection dataConnection, int maxBatchSize, IEnumerable<T> source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			return dataConnection.DataProvider.BulkCopy(
				dataConnection,
				new BulkCopyOptions { MaxBatchSize = maxBatchSize },
				source);
		}

		public static BulkCopyRowsCopied BulkCopy<T>([NotNull] this DataConnection dataConnection, IEnumerable<T> source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			return dataConnection.DataProvider.BulkCopy(
				dataConnection,
				new BulkCopyOptions(),
				source);
		}

		public static BulkCopyRowsCopied BulkCopy<T>([NotNull] this ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContextInfo.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			if (options.TableName    == null) options.TableName    = tbl.TableName;
			if (options.DatabaseName == null) options.DatabaseName = tbl.DatabaseName;
			if (options.SchemaName   == null) options.SchemaName   = tbl.SchemaName;

			return dataConnection.DataProvider.BulkCopy(dataConnection, options, source);
		}

		public static BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, int maxBatchSize, IEnumerable<T> source)
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContextInfo.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.BulkCopy(
				dataConnection,
				new BulkCopyOptions
				{
					MaxBatchSize = maxBatchSize,
					TableName    = tbl.TableName,
					DatabaseName = tbl.DatabaseName,
					SchemaName   = tbl.SchemaName,
				},
				source);
		}

		public static BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, IEnumerable<T> source)
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContextInfo.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.BulkCopy(
				dataConnection,
				new BulkCopyOptions
				{
					TableName    = tbl.TableName,
					DatabaseName = tbl.DatabaseName,
					SchemaName   = tbl.SchemaName,
				},
				source);
		}

		#endregion

		#region Merge

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
		/// <returns>Returns number of affected target records.</returns>
		[Obsolete("Use new Merge API. TODO: link to migration wiki-page")]
		public static int Merge<T>(this DataConnection dataConnection, IQueryable<T> source, Expression<Func<T,bool>> predicate,
			string tableName = null, string databaseName = null, string schemaName = null)
			where T : class 
		{
			return dataConnection.DataProvider.Merge(dataConnection, predicate, true, source.Where(predicate), tableName, databaseName, schemaName);
		}

		/// <summary>
		/// Executes following merge operations in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source.
		/// Method could be used only for with Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataConnection">Data connection instance.</param>
		/// <param name="predicate">Filter, applied to delete operation. Optional.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <returns>Returns number of affected target records.</returns>
		[Obsolete("Use new Merge API. TODO: link to migration wiki-page")]
		public static int Merge<T>(this DataConnection dataConnection, Expression<Func<T,bool>> predicate, IEnumerable<T> source,
			string tableName = null, string databaseName = null, string schemaName = null)
			where T : class 
		{
			return dataConnection.DataProvider.Merge(dataConnection, predicate, true, source, tableName, databaseName, schemaName);
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
		/// <returns>Returns number of affected target records.</returns>
		[Obsolete("Use new Merge API. TODO: link to migration wiki-page")]
		public static int Merge<T>(this DataConnection dataConnection, bool delete, IEnumerable<T> source,
			string tableName = null, string databaseName = null, string schemaName = null)
			where T : class 
		{
			return dataConnection.DataProvider.Merge(dataConnection, null, delete, source, tableName, databaseName, schemaName);
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
		/// <returns>Returns number of affected target records.</returns>
		[Obsolete("Use new Merge API. TODO: link to migration wiki-page")]
		public static int Merge<T>(this DataConnection dataConnection, IEnumerable<T> source,
			string tableName = null, string databaseName = null, string schemaName = null)
			where T : class 
		{
			return dataConnection.DataProvider.Merge(dataConnection, null, false, source, tableName, databaseName, schemaName);
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
		/// <returns>Returns number of affected target records.</returns>
		[Obsolete("Use new Merge API. TODO: link to migration wiki-page")]
		public static int Merge<T>(this ITable<T> table, IQueryable<T> source, Expression<Func<T,bool>> predicate,
			string tableName = null, string databaseName = null, string schemaName = null)
			where T : class 
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl = (Table<T>)table;
			var dataConnection = tbl.DataContextInfo.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.Merge(dataConnection, predicate, true, source.Where(predicate),
				tableName ?? tbl.TableName,
				databaseName ?? tbl.DatabaseName,
				schemaName ?? tbl.SchemaName);
		}

		/// <summary>
		/// Executes following merge operations in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source.
		/// Method could be used only for with Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="predicate">Filter, applied to delete operation. Optional.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <returns>Returns number of affected target records.</returns>
		[Obsolete("Use new Merge API. TODO: link to migration wiki-page")]
		public static int Merge<T>(this ITable<T> table, Expression<Func<T,bool>> predicate, IEnumerable<T> source,
			string tableName = null, string databaseName = null, string schemaName = null)
			where T : class 
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl = (Table<T>)table;
			var dataConnection = tbl.DataContextInfo.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.Merge(dataConnection, predicate, true, source,
				tableName ?? tbl.TableName,
				databaseName ?? tbl.DatabaseName,
				schemaName ?? tbl.SchemaName);
		}

		/// <summary>
		/// Executes following merge operations in specified order:
		/// - Update
		/// - Insert
		/// - Delete By Source (optional).
		/// If delete operation enabled by <paramref name="delete"/> parameter - method could be used only for with Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="delete">If true, merge command will include delete by source operation without condition.</param>
		/// <param name="source">Source data to merge into target table.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <returns>Returns number of affected target records.</returns>
		[Obsolete("Use new Merge API. TODO: link to migration wiki-page")]
		public static int Merge<T>(this ITable<T> table, bool delete, IEnumerable<T> source,
			string tableName = null, string databaseName = null, string schemaName = null)
			where T : class 
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl = (Table<T>)table;
			var dataConnection = tbl.DataContextInfo.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.Merge(dataConnection, null, delete, source,
				tableName ?? tbl.TableName,
				databaseName ?? tbl.DatabaseName,
				schemaName ?? tbl.SchemaName);
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
		/// <returns>Returns number of affected target records.</returns>
		[Obsolete("Use new Merge API. TODO: link to migration wiki-page")]
		public static int Merge<T>(this ITable<T> table, IEnumerable<T> source,
			string tableName = null, string databaseName = null, string schemaName = null)
			where T : class 
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl = (Table<T>)table;
			var dataConnection = tbl.DataContextInfo.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.Merge(dataConnection, null, false, source,
				tableName ?? tbl.TableName,
				databaseName ?? tbl.DatabaseName,
				schemaName ?? tbl.SchemaName);
		}

		#endregion
	}
}
