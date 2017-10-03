using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

#if !NOASYNC
using System.Threading;
using System.Threading.Tasks;
#endif

using JetBrains.Annotations;

namespace LinqToDB.Data
{
	using Linq;

	/// <summary>
	/// Contains extension methods for <see cref="DataConnection"/> class.
	/// </summary>
	[PublicAPI]
	public static class DataConnectionExtensions
	{
		#region SetCommand

		/// <summary>
		/// Creates command wrapper for current connection with provided command text.
		/// </summary>
		/// <param name="dataConnection">Database connection.</param>
		/// <param name="commandText">Command text.</param>
		/// <returns>Database command wrapper.</returns>
		public static CommandInfo SetCommand(this DataConnection dataConnection, string commandText)
		{
			return new CommandInfo(dataConnection, commandText);
		}

		/// <summary>
		/// Creates command wrapper for current connection with provided command text and parameters.
		/// </summary>
		/// <param name="dataConnection">Database connection.</param>
		/// <param name="commandText">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Database command wrapper.</returns>
		public static CommandInfo SetCommand(this DataConnection dataConnection, string commandText, params DataParameter[] parameters)
		{
			return new CommandInfo(dataConnection, commandText, parameters);
		}

		/// <summary>
		/// Creates command wrapper for current connection with provided command text and single parameter.
		/// </summary>
		/// <param name="dataConnection">Database connection.</param>
		/// <param name="commandText">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Database command wrapper.</returns>
		public static CommandInfo SetCommand(this DataConnection dataConnection, string commandText, DataParameter parameter)
		{
			return new CommandInfo(dataConnection, commandText, parameter);
		}

		/// <summary>
		/// Creates command wrapper for current connection with provided command text and parameters.
		/// </summary>
		/// <param name="dataConnection">Database connection.</param>
		/// <param name="commandText">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Database command wrapper.</returns>
		public static CommandInfo SetCommand(this DataConnection dataConnection, string commandText, object parameters)
		{
			return new CommandInfo(dataConnection, commandText, parameters);
		}

		#endregion

		#region Query with object reader

		/// <summary>
		/// Executes command and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql)
		{
			return new CommandInfo(connection, sql).Query(objectReader);
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> QueryProc<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryProc(objectReader);
		}

		/// <summary>
		/// Executes command and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).Query(objectReader);
		}

		/// <summary>
		/// Executes command and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).Query(objectReader);
		}

		#endregion

		#region Query with object reader async

#if !NOASYNC

		/// <summary>
		/// Executes command asynchronously and returns list of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql)
		{
			return new CommandInfo(connection, sql).QueryToListAsync(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, CancellationToken cancellationToken)
		{
			return new CommandInfo(connection, sql).QueryToListAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql)
		{
			return new CommandInfo(connection, sql).QueryToArrayAsync(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, CancellationToken cancellationToken)
		{
			return new CommandInfo(connection, sql).QueryToArrayAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToListAsync(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToListAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToArrayAsync(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToArrayAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToListAsync(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, object parameters, CancellationToken cancellationToken)
		{
			return new CommandInfo(connection, sql, parameters).QueryToListAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToArrayAsync(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, CancellationToken cancellationToken, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToArrayAsync(objectReader, cancellationToken);
		}

#endif

		#endregion

		#region Query

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql)
		{
			return new CommandInfo(connection, sql).Query<T>();
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).Query<T>();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> QueryProc<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryProc<T>();
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql, DataParameter parameter)
		{
			return new CommandInfo(connection, sql, parameter).Query<T>();
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).Query<T>();
		}

		#endregion

		#region Query async

#if !NOASYNC

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, string sql)
		{
			return new CommandInfo(connection, sql).QueryToListAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, string sql, CancellationToken cancellationToken)
		{
			return new CommandInfo(connection, sql).QueryToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, string sql)
		{
			return new CommandInfo(connection, sql).QueryToArrayAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, string sql, CancellationToken cancellationToken)
		{
			return new CommandInfo(connection, sql).QueryToArrayAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToListAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToArrayAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToArrayAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, string sql, DataParameter parameter)
		{
			return new CommandInfo(connection, sql, parameter).QueryToListAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, string sql, DataParameter parameter, CancellationToken cancellationToken)
		{
			return new CommandInfo(connection, sql, parameter).QueryToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, string sql, DataParameter parameter)
		{
			return new CommandInfo(connection, sql, parameter).QueryToArrayAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, string sql, DataParameter parameter, CancellationToken cancellationToken)
		{
			return new CommandInfo(connection, sql, parameter).QueryToArrayAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToListAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, string sql, CancellationToken cancellationToken, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToArrayAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, string sql, CancellationToken cancellationToken, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToArrayAsync<T>(cancellationToken);
		}

#endif

		#endregion

		#region Query with template
		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this DataConnection connection, T template, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).Query(template);
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this DataConnection connection, T template, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).Query(template);
		}

		#endregion

		#region Query with template async

#if !NOASYNC

		/// <summary>
		/// Executes command asynchronously and returns list of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, T template, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToListAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, T template, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, T template, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToArrayAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, T template, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToArrayAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, T template, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToListAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this DataConnection connection, T template, string sql, CancellationToken cancellationToken, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, T template, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToArrayAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this DataConnection connection, T template, string sql, CancellationToken cancellationToken, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).QueryToArrayAsync<T>(cancellationToken);
		}

#endif

		#endregion

		#region Execute

		/// <summary>
		/// Executes command and returns number of affected records.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Number of records, affected by command execution.</returns>
		public static int Execute(this DataConnection connection, string sql)
		{
			return new CommandInfo(connection, sql).Execute();
		}

		/// <summary>
		/// Executes command and returns number of affected records.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Number of records, affected by command execution.</returns>
		public static int Execute(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).Execute();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns number of affected records.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Number of records, affected by command execution.</returns>
		public static int ExecuteProc(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteProc();
		}

		/// <summary>
		/// Executes command and returns number of affected records.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Number of records, affected by command execution.</returns>
		public static int Execute(this DataConnection connection, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).Execute();
		}

#if !NOASYNC

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteAsync(this DataConnection connection, string sql)
		{
			return new CommandInfo(connection, sql).ExecuteAsync();
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteAsync(this DataConnection connection, string sql, CancellationToken cancellationToken)
		{
			return new CommandInfo(connection, sql).ExecuteAsync(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteAsync(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteAsync();
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteAsync(this DataConnection connection, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteAsync(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteAsync(this DataConnection connection, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteAsync();
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteAsync(this DataConnection connection, string sql, CancellationToken cancellationToken, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteAsync(cancellationToken);
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteProcAsync(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteProcAsync();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteProcAsync(this DataConnection connection, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteProcAsync(cancellationToken);
		}

#endif

		#endregion

		#region Execute scalar

		/// <summary>
		/// Executes command and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Resulting value.</returns>
		public static T Execute<T>(this DataConnection connection, string sql)
		{
			return new CommandInfo(connection, sql).Execute<T>();
		}

		/// <summary>
		/// Executes command and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Resulting value.</returns>
		public static T Execute<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).Execute<T>();
		}

		/// <summary>
		/// Executes command and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Resulting value.</returns>
		public static T Execute<T>(this DataConnection connection, string sql, DataParameter parameter)
		{
			return new CommandInfo(connection, sql, parameter).Execute<T>();
		}

		/// <summary>
		/// Executes command and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Resulting value.</returns>
		public static T Execute<T>(this DataConnection connection, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).Execute<T>();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Resulting value.</returns>
		public static T ExecuteProc<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteProc<T>();
		}

		#endregion

		#region Execute scalar async

#if !NOASYNC

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this DataConnection connection, string sql)
		{
			return new CommandInfo(connection, sql).ExecuteAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this DataConnection connection, string sql, CancellationToken cancellationToken)
		{
			return new CommandInfo(connection, sql).ExecuteAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this DataConnection connection, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this DataConnection connection, string sql, DataParameter parameter)
		{
			return new CommandInfo(connection, sql, parameter).ExecuteAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this DataConnection connection, string sql, DataParameter parameter, CancellationToken cancellationToken)
		{
			return new CommandInfo(connection, sql, parameter).ExecuteAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this DataConnection connection, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this DataConnection connection, string sql, CancellationToken cancellationToken, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteProcAsync<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteProcAsync<T>();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Resulting value.</returns>
		public static Task<T> ExecuteProcAsync<T>(this DataConnection connection, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteProcAsync<T>(cancellationToken);
		}

#endif

		#endregion

		#region ExecuteReader

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Data reader object.</returns>
		public static DataReader ExecuteReader(this DataConnection connection, string sql)
		{
			return new CommandInfo(connection, sql).ExecuteReader();
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Data reader object.</returns>
		public static DataReader ExecuteReader(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteReader();
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Data reader object.</returns>
		public static DataReader ExecuteReader(this DataConnection connection, string sql, DataParameter parameter)
		{
			return new CommandInfo(connection, sql, parameter).ExecuteReader();
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Data reader object.</returns>
		public static DataReader ExecuteReader(this DataConnection connection, string sql, object parameters)
		{
			return new CommandInfo(connection, sql, parameters).ExecuteReader();
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="commandType">Type of command. See <see cref="CommandType"/> for all supported types.</param>
		/// <param name="commandBehavior">Command behavior flags. See <see cref="CommandBehavior"/> for more details.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Data reader object.</returns>
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

		/// <summary>
		/// Performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="dataConnection">Database connection.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>([NotNull] this DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");
			return dataConnection.DataProvider.BulkCopy(dataConnection, options, source);
		}

		/// <summary>
		/// Performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="dataConnection">Database connection.</param>
		/// <param name="maxBatchSize">TODO</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>([NotNull] this DataConnection dataConnection, int maxBatchSize, IEnumerable<T> source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			return dataConnection.DataProvider.BulkCopy(
				dataConnection,
				new BulkCopyOptions { MaxBatchSize = maxBatchSize },
				source);
		}

		/// <summary>
		/// Performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="dataConnection">Database connection.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>([NotNull] this DataConnection dataConnection, IEnumerable<T> source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			return dataConnection.DataProvider.BulkCopy(
				dataConnection,
				new BulkCopyOptions(),
				source);
		}

		/// <summary>
		/// Performs bulk intert operation into table specified in <paramref name="options"/> parameter or into table, identified by <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>([NotNull] this ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			if (options.TableName    == null) options.TableName    = tbl.TableName;
			if (options.DatabaseName == null) options.DatabaseName = tbl.DatabaseName;
			if (options.SchemaName   == null) options.SchemaName   = tbl.SchemaName;

			return dataConnection.DataProvider.BulkCopy(dataConnection, options, source);
		}

		/// <summary>
		/// Performs bulk intert operation into table, identified by <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="maxBatchSize">TODO</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, int maxBatchSize, IEnumerable<T> source)
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContext as DataConnection;

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

		/// <summary>
		/// Performs bulk intert operation into table, identified by <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, IEnumerable<T> source)
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContext as DataConnection;

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
		public static int Merge<T>(
			this DataConnection      dataConnection, 
			IQueryable<T>            source, 
			Expression<Func<T,bool>> predicate,
			string                   tableName    = null, 
			string                   databaseName = null, 
			string                   schemaName   = null
		)
			where T : class 
		{
			return dataConnection.DataProvider.Merge(dataConnection, predicate, true, source.Where(predicate), tableName, databaseName, schemaName);
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
		/// <returns>Returns number of affected target records.</returns>
		public static int Merge<T>(
			this DataConnection      dataConnection, 
			Expression<Func<T,bool>> predicate, 
			IEnumerable<T>           source,
			string                   tableName    = null, 
			string                   databaseName = null, 
			string                   schemaName   = null
		)
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
		public static int Merge<T>(
			this DataConnection dataConnection, 
			bool                delete, 
			IEnumerable<T>      source,
			string              tableName    = null, 
			string              databaseName = null, 
			string              schemaName   = null
		)
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
		public static int Merge<T>(
			this DataConnection dataConnection, 
			IEnumerable<T>      source,
			string              tableName    = null, 
			string              databaseName = null, 
			string              schemaName   = null
		)
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
		public static int Merge<T>(
			this ITable<T>           table, 
			IQueryable<T>            source, 
			Expression<Func<T,bool>> predicate,
			string                   tableName    = null, 
			string                   databaseName = null, 
			string                   schemaName   = null
		)
			where T : class 
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.Merge(dataConnection, predicate, true, source.Where(predicate),
				tableName    ?? tbl.TableName,
				databaseName ?? tbl.DatabaseName,
				schemaName   ?? tbl.SchemaName);
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
		/// <returns>Returns number of affected target records.</returns>
		public static int Merge<T>(
			this ITable<T>           table, 
			Expression<Func<T,bool>> predicate, 
			IEnumerable<T>           source,
			string                   tableName    = null, 
			string                   databaseName = null, 
			string                   schemaName   = null
		)
			where T : class 
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.Merge(dataConnection, predicate, true, source,
				tableName    ?? tbl.TableName,
				databaseName ?? tbl.DatabaseName,
				schemaName   ?? tbl.SchemaName);
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
		/// <returns>Returns number of affected target records.</returns>
		public static int Merge<T>(
			this ITable<T> table, 
			bool           delete, 
			IEnumerable<T> source,
			string         tableName    = null, 
			string         databaseName = null, 
			string         schemaName   = null
		)
			where T : class 
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.Merge(dataConnection, null, delete, source,
				tableName    ?? tbl.TableName,
				databaseName ?? tbl.DatabaseName,
				schemaName   ?? tbl.SchemaName);
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
		public static int Merge<T>(
			this ITable<T> table, 
			IEnumerable<T> source,
			string         tableName    = null, 
			string         databaseName = null, 
			string         schemaName   = null
		)
			where T : class 
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.Merge(dataConnection, null, false, source,
				tableName    ?? tbl.TableName,
				databaseName ?? tbl.DatabaseName,
				schemaName   ?? tbl.SchemaName);
		}

#if !NOASYNC

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
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		public static Task<int> MergeAsync<T>(
			this DataConnection      dataConnection, 
			IQueryable<T>            source, 
			Expression<Func<T,bool>> predicate,
			string                   tableName         = null, 
			string                   databaseName      = null, 
			string                   schemaName        = null,
			CancellationToken        cancellationToken = default(CancellationToken))
			where T : class
		{
			return dataConnection.DataProvider.MergeAsync(
				dataConnection, predicate, true, source.Where(predicate), tableName, databaseName, schemaName, cancellationToken);
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
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		public static Task<int> MergeAsync<T>(
			this DataConnection      dataConnection, 
			Expression<Func<T,bool>> predicate, 
			IEnumerable<T>           source,
			string                   tableName         = null, 
			string                   databaseName      = null, 
			string                   schemaName        = null,
			CancellationToken        cancellationToken = default(CancellationToken))
			where T : class
		{
			return dataConnection.DataProvider.MergeAsync(dataConnection, predicate, true, source, tableName, databaseName, schemaName, cancellationToken);
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
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		public static Task<int> MergeAsync<T>(
			this DataConnection dataConnection, 
			bool                delete, 
			IEnumerable<T>      source,
			string              tableName         = null, 
			string              databaseName      = null, 
			string              schemaName        = null,
			CancellationToken   cancellationToken = default(CancellationToken)
		)
			where T : class
		{
			return dataConnection.DataProvider.MergeAsync(dataConnection, null, delete, source, tableName, databaseName, schemaName, cancellationToken);
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
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		public static Task<int> MergeAsync<T>(			
			this DataConnection dataConnection, 
			IEnumerable<T>      source,
			string              tableName         = null, 
			string              databaseName      = null, 
			string              schemaName        = null,
			CancellationToken   cancellationToken = default(CancellationToken)
		)
			where T : class
		{
			return dataConnection.DataProvider.MergeAsync(dataConnection, null, false, source, tableName, databaseName, schemaName, cancellationToken);
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
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		public static Task<int> MergeAsync<T>(
			this ITable<T>           table, 
			IQueryable<T>            source, 
			Expression<Func<T,bool>> predicate,
			string                   tableName         = null, 
			string                   databaseName      = null, 
			string                   schemaName        = null,
		    CancellationToken        cancellationToken = default(CancellationToken)
		)
			where T : class 
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.MergeAsync(dataConnection, predicate, true, source.Where(predicate),
				tableName    ?? tbl.TableName,
				databaseName ?? tbl.DatabaseName,
				schemaName   ?? tbl.SchemaName,
				cancellationToken);
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
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		public static Task<int> MergAsync<T>(
			this ITable<T>           table, 
			Expression<Func<T,bool>> predicate, 
			IEnumerable<T>           source,
			string                   tableName         = null, 
			string                   databaseName      = null, 
			string                   schemaName        = null,
			CancellationToken        cancellationToken = default(CancellationToken)
		)
			where T : class 
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.MergeAsync(dataConnection, predicate, true, source,
				tableName    ?? tbl.TableName,
				databaseName ?? tbl.DatabaseName,
				schemaName   ?? tbl.SchemaName,
				cancellationToken);
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
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		public static Task<int> MergeAsync<T>(
			this ITable<T>    table, 
			bool              delete, 
			IEnumerable<T>    source,
			string            tableName         = null, 
			string            databaseName      = null, 
			string            schemaName        = null,
			CancellationToken cancellationToken = default(CancellationToken)
		)
			where T : class 
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.MergeAsync(dataConnection, null, delete, source,
				tableName    ?? tbl.TableName,
				databaseName ?? tbl.DatabaseName,
				schemaName   ?? tbl.SchemaName,
				cancellationToken);
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
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task with number of affected target records.</returns>
		public static Task<int> MergeAsync<T>(
			this ITable<T>    table,
			IEnumerable<T>    source,
			string            tableName         = null,
			string            databaseName      = null,
			string            schemaName        = null,
			CancellationToken cancellationToken = default(CancellationToken))
			where T : class 
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl            = (Table<T>)table;
			var dataConnection = tbl.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.MergeAsync(dataConnection, null, false, source,
				tableName    ?? tbl.TableName,
				databaseName ?? tbl.DatabaseName,
				schemaName   ?? tbl.SchemaName,
				cancellationToken);
		}

#endif

		#endregion
	}
}
