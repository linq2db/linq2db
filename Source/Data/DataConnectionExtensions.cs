using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Data
{
	public static class DataConnectionExtensions
	{
		#region SetCommand

		public static CommandInfo SetCommand(this DataConnection dataConnection, string commandText)
		{
			return new CommandInfo(dataConnection, commandText);
		}

		public static CommandInfo SetCommand(this DataConnection dataConnection, string commandText, params DataParameter[] parameters)
		{
			return new CommandInfo(dataConnection, commandText, parameters);
		}

		public static CommandInfo SetCommand(this DataConnection dataConnection, string commandText, DataParameter parameter)
		{
			return new CommandInfo(dataConnection, commandText, parameter);
		}

		public static CommandInfo SetCommand(this DataConnection dataConnection, string commandText, object parameters)
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

		public static BulkCopyRowsCopied BulkCopy<T>(this DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return dataConnection.DataProvider.BulkCopy(dataConnection, options, source);
		}

		public static BulkCopyRowsCopied BulkCopy<T>(this DataConnection dataConnection, int maxBatchSize, IEnumerable<T> source)
		{
			return dataConnection.DataProvider.BulkCopy(
				dataConnection,
				new BulkCopyOptions { MaxBatchSize = maxBatchSize },
				source);
		}

		public static BulkCopyRowsCopied BulkCopy<T>(this DataConnection dataConnection, IEnumerable<T> source)
		{
			return dataConnection.DataProvider.BulkCopy(
				dataConnection,
				new BulkCopyOptions(),
				source);
		}

		#endregion

		#region Merge

		public static int Merge<T>(this DataConnection dataConnection, IQueryable<T> source, Expression<Func<T,bool>> predicate)
			where T : class 
		{
			return dataConnection.DataProvider.Merge(dataConnection, predicate, true, source.Where(predicate));
		}

		public static int Merge<T>(this DataConnection dataConnection, Expression<Func<T,bool>> predicate, IEnumerable<T> source)
			where T : class 
		{
			return dataConnection.DataProvider.Merge(dataConnection, predicate, true, source);
		}

		public static int Merge<T>(this DataConnection dataConnection, bool delete, IEnumerable<T> source)
			where T : class 
		{
			return dataConnection.DataProvider.Merge(dataConnection, null, delete, source);
		}

		public static int Merge<T>(this DataConnection dataConnection, IEnumerable<T> source)
			where T : class 
		{
			return dataConnection.DataProvider.Merge(dataConnection, null, false, source);
		}

		#endregion
	}
}
