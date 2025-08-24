using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Extensions;
using LinqToDB.Metrics;

namespace LinqToDB.Data
{
	/// <summary>
	/// Contains extension methods for <see cref="DataConnection"/> and <see cref="DataContext"/> classes.
	/// </summary>
	[PublicAPI]
	public static class DataContextExtensions
	{
		#region SetCommand

		/// <summary>
		/// Creates command wrapper for current connection with provided command text.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="commandText">Command text.</param>
		/// <returns>Database command wrapper.</returns>
		public static CommandInfo SetCommand(this IDataContext dataContext, string commandText)
		{
			return new CommandInfo(dataContext, commandText);
		}

		/// <summary>
		/// Creates command wrapper for current connection with provided command text and parameters.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="commandText">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Database command wrapper.</returns>
		public static CommandInfo SetCommand(this IDataContext dataContext, string commandText, params DataParameter[] parameters)
		{
			return new CommandInfo(dataContext, commandText, parameters);
		}

		/// <summary>
		/// Creates command wrapper for current connection with provided command text and single parameter.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="commandText">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Database command wrapper.</returns>
		public static CommandInfo SetCommand(this IDataContext dataContext, string commandText, DataParameter parameter)
		{
			return new CommandInfo(dataContext, commandText, parameter);
		}

		/// <summary>
		/// Creates command wrapper for current connection with provided command text and parameters.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="commandText">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Database command wrapper.</returns>
		public static CommandInfo SetCommand(this IDataContext dataContext, string commandText, object? parameters)
		{
			return new CommandInfo(dataContext, commandText, parameters);
		}

		#endregion

		#region Query[Async] with object reader

		/// <summary>
		/// Executes command and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql)
		{
			return SetCommand(dataContext, sql).Query(objectReader);
		}

		/// <summary>
		/// Executes command and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql).QueryAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).Query(objectReader);
		}

		/// <summary>
		/// Executes command and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryAsync(objectReader);
		}

		/// <summary>
		/// Executes command and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).Query(objectReader);
		}

		/// <summary>
		/// Executes command and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryAsync(objectReader, cancellationToken);
		}

		#endregion

		#region QueryProc[Async] with object reader

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> QueryProc<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryProc(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryProcAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryProcAsync(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryProcAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryProcAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> QueryProc<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryProc(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryProcAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryProcAsync(objectReader, cancellationToken);
		}

		#endregion

		#region QueryTo[List|Array]Async / QueryToAsyncEnumerable with object reader

		/// <summary>
		/// Executes command asynchronously and returns list of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql).QueryToListAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToListAsync(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToListAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryToListAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql).QueryToArrayAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToArrayAsync(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToArrayAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryToArrayAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns async sequence of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Async sequence of records returned by the query.</returns>
		public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql)
		{
			return SetCommand(dataContext, sql).QueryToAsyncEnumerable(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Async sequence of records returned by the query.</returns>
		public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToAsyncEnumerable(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Async sequence of records returned by the query.</returns>
		public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToAsyncEnumerable(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously and apply provided action to each record, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="action">Action, applied to each result record.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task.</returns>
		public static Task QueryForEachAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, Action<T> action, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryForEachAsync(objectReader, action);
		}

		/// <summary>
		/// Executes command asynchronously and apply provided action to each record, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="action">Action, applied to each result record.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task.</returns>
		public static Task QueryForEachAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, Action<T> action, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryForEachAsync(objectReader, action, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and apply provided action to each record, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="action">Action, applied to each result record.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static Task QueryForEachAsync<T>(this IDataContext dataContext, Func<DbDataReader, T> objectReader, Action<T> action, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryForEachAsync(objectReader, action, cancellationToken);
		}

		#endregion

		#region Query[Async] with built-in mapper

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this IDataContext dataContext, string sql)
		{
			return SetCommand(dataContext, sql).Query<T>();
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDataContext dataContext, string sql, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql).QueryAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).Query<T>();
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryAsync<T>();
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDataContext dataContext, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this IDataContext dataContext, string sql, DataParameter parameter)
		{
			return SetCommand(dataContext, sql, parameter).Query<T>();
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDataContext dataContext, string sql, DataParameter parameter, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameter).QueryAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this IDataContext dataContext, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).Query<T>();
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDataContext dataContext, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this IDataContext dataContext, T template, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).Query(template);
		}

		#endregion

		#region Query[Async] with built-in mapper and template

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDataContext dataContext, T template, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryAsync(template);
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDataContext dataContext, T template, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryAsync(template, cancellationToken);
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> Query<T>(this IDataContext dataContext, T template, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).Query(template);
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDataContext dataContext, T template, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryAsync(template, cancellationToken);
		}

		#endregion

		#region QueryMultiple[Async]

		/// <summary>
		/// Executes command and returns a result containing multiple result sets.
		/// </summary>
		/// <typeparam name="T">Result set type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns result.</returns>
		/// <example>
		/// Example of <typeparamref name="T"/> definition with <see cref="Mapping.ResultSetIndexAttribute"/>.
		/// <code>
		/// class MultipleResult
		/// {
		///	   [ResultSetIndex(0)] public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   [ResultSetIndex(1)] public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   [ResultSetIndex(2)] public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   [ResultSetIndex(3)] public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// Example of <typeparamref name="T"/> definition without attributes.
		/// <code>
		/// class MultipleResult
		/// {
		///	   public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// </example>
		/// <remarks>
		///		- type <typeparamref name="T"/> should have default constructor.<para/>
		///		- if at least one property or field has <see cref="Mapping.ResultSetIndexAttribute"/>,
		///		then properties that are not marked with <see cref="Mapping.ResultSetIndexAttribute"/> will be ignored.<para/>
		///		- if there is missing index in properties that are marked with <see cref="Mapping.ResultSetIndexAttribute"/>, then result set under missing index will be ignored.<para/>
		///		- if there is no <see cref="Mapping.ResultSetIndexAttribute"/>, then all non readonly fields or properties with setter will read from multiple result set. Order is based on their appearance in class.
		/// </remarks>
		public static T QueryMultiple<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
			where T : class
		{
			return SetCommand(dataContext, sql, parameters).QueryMultiple<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns a result containing multiple result sets.
		/// </summary>
		/// <typeparam name="T">Result set type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>
		///     A task that represents the asynchronous operation.
		///     The task result contains object with multiply result sets.
		/// </returns>
		/// <example>
		/// Example of <typeparamref name="T"/> definition with <see cref="Mapping.ResultSetIndexAttribute"/>.
		/// <code>
		/// class MultipleResult
		/// {
		///	   [ResultSetIndex(0)] public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   [ResultSetIndex(1)] public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   [ResultSetIndex(2)] public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   [ResultSetIndex(3)] public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// Example of <typeparamref name="T"/> definition without attributes.
		/// <code>
		/// class MultipleResult
		/// {
		///	   public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// </example>
		/// <remarks>
		///		- type <typeparamref name="T"/> should have default constructor.<para/>
		///		- if at least one property or field has <see cref="Mapping.ResultSetIndexAttribute"/>,
		///		then properties that are not marked with <see cref="Mapping.ResultSetIndexAttribute"/> will be ignored.<para/>
		///		- if there is missing index in properties that are marked with <see cref="Mapping.ResultSetIndexAttribute"/>, then result set under missing index will be ignored.<para/>
		///		- if there is no <see cref="Mapping.ResultSetIndexAttribute"/>, then all non readonly fields or properties with setter will read from multiple result set. Order is based on their appearance in class.
		/// </remarks>
		public static Task<T> QueryMultipleAsync<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
			where T : class
		{
			return SetCommand(dataContext, sql, parameters).QueryMultipleAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns a result containing multiple result sets.
		/// </summary>
		/// <typeparam name="T">Result set type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>
		///     A task that represents the asynchronous operation.
		///     The task result contains object with multiply result sets.
		/// </returns>
		/// <example>
		/// Example of <typeparamref name="T"/> definition with <see cref="Mapping.ResultSetIndexAttribute"/>.
		/// <code>
		/// class MultipleResult
		/// {
		///	   [ResultSetIndex(0)] public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   [ResultSetIndex(1)] public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   [ResultSetIndex(2)] public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   [ResultSetIndex(3)] public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// Example of <typeparamref name="T"/> definition without attributes.
		/// <code>
		/// class MultipleResult
		/// {
		///	   public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// </example>
		/// <remarks>
		///		- type <typeparamref name="T"/> should have default constructor.<para/>
		///		- if at least one property or field has <see cref="Mapping.ResultSetIndexAttribute"/>,
		///		then properties that are not marked with <see cref="Mapping.ResultSetIndexAttribute"/> will be ignored.<para/>
		///		- if there is missing index in properties that are marked with <see cref="Mapping.ResultSetIndexAttribute"/>, then result set under missing index will be ignored.<para/>
		///		- if there is no <see cref="Mapping.ResultSetIndexAttribute"/>, then all non readonly fields or properties with setter will read from multiple result set. Order is based on their appearance in class.
		/// </remarks>
		public static Task<T> QueryMultipleAsync<T>(this IDataContext dataContext, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
			where T : class
		{
			return SetCommand(dataContext, sql, parameters).QueryMultipleAsync<T>(cancellationToken);
		}

		#endregion

		#region QueryProc[Async] with built-in mapper and template

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> QueryProc<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryProc<T>();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryProcAsync<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryProcAsync<T>();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryProcAsync<T>(this IDataContext dataContext, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryProcAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> QueryProc<T>(this IDataContext dataContext, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryProc<T>();
		}

		/// <summary>
		/// Executes command asynchronously using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryProcAsync<T>(this IDataContext dataContext, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryProcAsync<T>(cancellationToken);
		}

		#endregion

		#region QueryProc[Async] with built-in mapper and template

		/// <summary>
		/// Executes stored procedure and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> QueryProc<T>(this IDataContext dataContext, T template, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryProc(template);
		}

		/// <summary>
		/// Executes stored procedure asynchronously and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryProcAsync<T>(this IDataContext dataContext, T template, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryProcAsync(template);
		}

		/// <summary>
		/// Executes stored procedure asynchronously and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryProcAsync<T>(this IDataContext dataContext, T template, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryProcAsync(template, cancellationToken);
		}

		/// <summary>
		/// Executes stored procedure and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns collection of query result records.</returns>
		public static IEnumerable<T> QueryProc<T>(this IDataContext dataContext, T template, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryProc(template);
		}

		/// <summary>
		/// Executes stored procedure asynchronously and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public static Task<IEnumerable<T>> QueryProcAsync<T>(this IDataContext dataContext, T template, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryProcAsync(template, cancellationToken);
		}

		#endregion

		#region QueryProcMultiple[Async]

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns a result containing multiple result sets.
		/// Sets result values for output and reference parameters to corresponding parameters in <paramref name="parameters"/>.
		/// </summary>
		/// <typeparam name="T">Result set type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns result.</returns>
		/// <example>
		/// Example of <typeparamref name="T"/> definition with <see cref="Mapping.ResultSetIndexAttribute"/>.
		/// <code>
		/// class MultipleResult
		/// {
		///	   [ResultSetIndex(0)] public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   [ResultSetIndex(1)] public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   [ResultSetIndex(2)] public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   [ResultSetIndex(3)] public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// Example of <typeparamref name="T"/> definition without attributes.
		/// <code>
		/// class MultipleResult
		/// {
		///	   public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// </example>
		/// <remarks>
		///		- type <typeparamref name="T"/> should have default constructor.<para/>
		///		- if at least one property or field has <see cref="Mapping.ResultSetIndexAttribute"/>,
		///		then properties that are not marked with <see cref="Mapping.ResultSetIndexAttribute"/> will be ignored.<para/>
		///		- if there is missing index in properties that are marked with <see cref="Mapping.ResultSetIndexAttribute"/>, then result set under missing index will be ignored.<para/>
		///		- if there is no <see cref="Mapping.ResultSetIndexAttribute"/>, then all non readonly fields or properties with setter will read from multiple result set. Order is based on their appearance in class.
		/// </remarks>
		public static T QueryProcMultiple<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
			where T : class
		{
			return SetCommand(dataContext, sql, parameters).QueryProcMultiple<T>();
		}

		/// <summary>
		/// Executes command asynchronously using <see cref="CommandType.StoredProcedure"/> command type and returns a result containing multiple result sets.
		/// </summary>
		/// <typeparam name="T">Result set type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>
		///     A task that represents the asynchronous operation.
		///     The task result contains object with multiply result sets.
		/// </returns>
		/// <example>
		/// Example of <typeparamref name="T"/> definition with <see cref="Mapping.ResultSetIndexAttribute"/>.
		/// <code>
		/// class MultipleResult
		/// {
		///	   [ResultSetIndex(0)] public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   [ResultSetIndex(1)] public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   [ResultSetIndex(2)] public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   [ResultSetIndex(3)] public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// Example of <typeparamref name="T"/> definition without attributes.
		/// <code>
		/// class MultipleResult
		/// {
		///	   public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// </example>
		/// <remarks>
		///		- type <typeparamref name="T"/> should have default constructor.<para/>
		///		- if at least one property or field has <see cref="Mapping.ResultSetIndexAttribute"/>,
		///		then properties that are not marked with <see cref="Mapping.ResultSetIndexAttribute"/> will be ignored.<para/>
		///		- if there is missing index in properties that are marked with <see cref="Mapping.ResultSetIndexAttribute"/>, then result set under missing index will be ignored.<para/>
		///		- if there is no <see cref="Mapping.ResultSetIndexAttribute"/>, then all non readonly fields or properties with setter will read from multiple result set. Order is based on their appearance in class.
		/// </remarks>
		public static Task<T> QueryProcMultipleAsync<T>(this IDataContext dataContext, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
			where T : class
		{
			return SetCommand(dataContext, sql, parameters).QueryProcMultipleAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously using <see cref="CommandType.StoredProcedure"/> command type and returns a result containing multiple result sets.
		/// Sets result values for output and reference parameters to corresponding parameters in <paramref name="parameters"/>.
		/// </summary>
		/// <typeparam name="T">Result set type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>
		///     A task that represents the asynchronous operation.
		///     The task result contains object with multiply result sets.
		/// </returns>
		/// <example>
		/// Example of <typeparamref name="T"/> definition with <see cref="Mapping.ResultSetIndexAttribute"/>.
		/// <code>
		/// class MultipleResult
		/// {
		///	   [ResultSetIndex(0)] public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   [ResultSetIndex(1)] public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   [ResultSetIndex(2)] public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   [ResultSetIndex(3)] public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// Example of <typeparamref name="T"/> definition without attributes.
		/// <code>
		/// class MultipleResult
		/// {
		///	   public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// </example>
		/// <remarks>
		///		- type <typeparamref name="T"/> should have default constructor.<para/>
		///		- if at least one property or field has <see cref="Mapping.ResultSetIndexAttribute"/>,
		///		then properties that are not marked with <see cref="Mapping.ResultSetIndexAttribute"/> will be ignored.<para/>
		///		- if there is missing index in properties that are marked with <see cref="Mapping.ResultSetIndexAttribute"/>, then result set under missing index will be ignored.<para/>
		///		- if there is no <see cref="Mapping.ResultSetIndexAttribute"/>, then all non readonly fields or properties with setter will read from multiple result set. Order is based on their appearance in class.
		/// </remarks>
		public static Task<T> QueryProcMultipleAsync<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
			where T : class
		{
			return SetCommand(dataContext, sql, parameters).QueryProcMultipleAsync<T>();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns a result containing multiple result sets.
		/// </summary>
		/// <typeparam name="T">Result set type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Returns result.</returns>
		/// <example>
		/// Example of <typeparamref name="T"/> definition with <see cref="Mapping.ResultSetIndexAttribute"/>.
		/// <code>
		/// class MultipleResult
		/// {
		///	   [ResultSetIndex(0)] public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   [ResultSetIndex(1)] public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   [ResultSetIndex(2)] public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   [ResultSetIndex(3)] public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// Example of <typeparamref name="T"/> definition without attributes.
		/// <code>
		/// class MultipleResult
		/// {
		///	   public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// </example>
		/// <remarks>
		///		- type <typeparamref name="T"/> should have default constructor.<para/>
		///		- if at least one property or field has <see cref="Mapping.ResultSetIndexAttribute"/>,
		///		then properties that are not marked with <see cref="Mapping.ResultSetIndexAttribute"/> will be ignored.<para/>
		///		- if there is missing index in properties that are marked with <see cref="Mapping.ResultSetIndexAttribute"/>, then result set under missing index will be ignored.<para/>
		///		- if there is no <see cref="Mapping.ResultSetIndexAttribute"/>, then all non readonly fields or properties with setter will read from multiple result set. Order is based on their appearance in class.
		/// </remarks>
		public static T QueryProcMultiple<T>(this IDataContext dataContext, string sql, object? parameters)
			where T : class
		{
			return SetCommand(dataContext, sql, parameters).QueryProcMultiple<T>();
		}

		/// <summary>
		/// Executes command asynchronously using <see cref="CommandType.StoredProcedure"/> command type and returns a result containing multiple result sets.
		/// </summary>
		/// <typeparam name="T">Result set type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>
		///     A task that represents the asynchronous operation.
		///     The task result contains object with multiply result sets.
		/// </returns>
		/// <example>
		/// Example of <typeparamref name="T"/> definition with <see cref="Mapping.ResultSetIndexAttribute"/>.
		/// <code>
		/// class MultipleResult
		/// {
		///	   [ResultSetIndex(0)] public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   [ResultSetIndex(1)] public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   [ResultSetIndex(2)] public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   [ResultSetIndex(3)] public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// Example of <typeparamref name="T"/> definition without attributes.
		/// <code>
		/// class MultipleResult
		/// {
		///	   public IEnumerable&lt;Person&gt;  AllPersons   { get; set; }
		///	   public IList&lt;Doctor&gt;        AllDoctors   { get; set; }
		///	   public IEnumerable&lt;Patient&gt; AllPatients  { get; set; }
		///	   public Patient              FirstPatient { get; set; }
		/// }
		/// </code>
		/// </example>
		/// <remarks>
		///		- type <typeparamref name="T"/> should have default constructor.<para/>
		///		- if at least one property or field has <see cref="Mapping.ResultSetIndexAttribute"/>,
		///		then properties that are not marked with <see cref="Mapping.ResultSetIndexAttribute"/> will be ignored.<para/>
		///		- if there is missing index in properties that are marked with <see cref="Mapping.ResultSetIndexAttribute"/>, then result set under missing index will be ignored.<para/>
		///		- if there is no <see cref="Mapping.ResultSetIndexAttribute"/>, then all non readonly fields or properties with setter will read from multiple result set. Order is based on their appearance in class.
		/// </remarks>
		public static Task<T> QueryProcMultipleAsync<T>(this IDataContext dataContext, string sql, object? parameters, CancellationToken cancellationToken = default)
			where T : class
		{
			return SetCommand(dataContext, sql, parameters).QueryProcMultipleAsync<T>(cancellationToken);
		}

		#endregion

		#region QueryTo[List|Array]Async and QueryToAsyncEnumerable with built-in mapper

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this IDataContext dataContext, string sql, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql).QueryToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToListAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this IDataContext dataContext, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this IDataContext dataContext, string sql, DataParameter parameter, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameter).QueryToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static Task<List<T>> QueryToListAsync<T>(this IDataContext dataContext, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this IDataContext dataContext, string sql, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql).QueryToArrayAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToArrayAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this IDataContext dataContext, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToArrayAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this IDataContext dataContext, string sql, DataParameter parameter, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameter).QueryToArrayAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static Task<T[]> QueryToArrayAsync<T>(this IDataContext dataContext, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryToArrayAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Async sequence of records returned by the query.</returns>
		public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(this IDataContext dataContext, string sql)
		{
			return SetCommand(dataContext, sql).QueryToAsyncEnumerable<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Async sequence of records returned by the query.</returns>
		public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToAsyncEnumerable<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Async sequence of records returned by the query.</returns>
		public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(this IDataContext dataContext, string sql, DataParameter parameter)
		{
			return SetCommand(dataContext, sql, parameter).QueryToAsyncEnumerable<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Async sequence of records returned by the query.</returns>
		public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(this IDataContext dataContext, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToAsyncEnumerable<T>();
		}

		/// <summary>
		/// Executes command asynchronously and apply provided action to each record.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="action">Action, applied to each result record.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task.</returns>
		public static Task QueryForEachAsync<T>(this IDataContext dataContext, Action<T> action, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryForEachAsync(action);
		}

		/// <summary>
		/// Executes command asynchronously and apply provided action to each record.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="action">Action, applied to each result record.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task.</returns>
		public static Task QueryForEachAsync<T>(this IDataContext dataContext, Action<T> action, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryForEachAsync(action, cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and apply provided action to each record.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="action">Action, applied to each result record.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static Task QueryForEachAsync<T>(this IDataContext dataContext, Action<T> action, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryForEachAsync(action, cancellationToken);
		}

		#endregion

		#region QueryTo[List|Array]Async and QueryToAsyncEnumerable with built-in mapper and template

		/// <summary>
		/// Executes command asynchronously and returns list of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "template param used to provide T generic argument")]
		public static Task<List<T>> QueryToListAsync<T>(this IDataContext dataContext, T template, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToListAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "template param used to provide T generic argument")]
		public static Task<List<T>> QueryToListAsync<T>(this IDataContext dataContext, T template, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "template param used to provide T generic argument")]
		public static Task<List<T>> QueryToListAsync<T>(this IDataContext dataContext, T template, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryToListAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "template param used to provide T generic argument")]
		public static Task<T[]> QueryToArrayAsync<T>(this IDataContext dataContext, T template, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToArrayAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "template param used to provide T generic argument")]
		public static Task<T[]> QueryToArrayAsync<T>(this IDataContext dataContext, T template, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToArrayAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "template param used to provide T generic argument")]
		public static Task<T[]> QueryToArrayAsync<T>(this IDataContext dataContext, T template, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).QueryToArrayAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Async sequence of records returned by the query.</returns>
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "template param used to provide T generic argument")]
		public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(this IDataContext dataContext, T template, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToAsyncEnumerable<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Async sequence of records returned by the query.</returns>
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "template param used to provide T generic argument")]
		public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(this IDataContext dataContext, T template, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).QueryToAsyncEnumerable<T>();
		}

		#endregion

		#region Execute[Async] (ExecuteNonQuery)

		/// <summary>
		/// Executes command and returns number of affected records.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Number of records, affected by command execution.</returns>
		public static int Execute(this IDataContext dataContext, string sql)
		{
			return SetCommand(dataContext, sql).Execute();
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteAsync(this IDataContext dataContext, string sql, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql).ExecuteAsync(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns number of affected records.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Number of records, affected by command execution.</returns>
		public static int Execute(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).Execute();
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteAsync(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteAsync();
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteAsync(this IDataContext dataContext, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteAsync(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns number of affected records.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Number of records, affected by command execution.</returns>
		public static int Execute(this IDataContext dataContext, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).Execute();
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteAsync(this IDataContext dataContext, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteAsync(cancellationToken);
		}

		#endregion

		#region ExecuteProc[Async] (ExecuteNonQuery)

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns number of affected records.
		/// Sets result values for output and reference parameters to corresponding parameters in <paramref name="parameters"/>.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Number of records, affected by command execution.</returns>
		public static int ExecuteProc(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteProc();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns number of affected records.
		/// Sets result values for output and reference parameters to corresponding parameters in <paramref name="parameters"/>.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteProcAsync(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteProcAsync();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteProcAsync(this IDataContext dataContext, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteProcAsync(cancellationToken);
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns number of affected records.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Number of records, affected by command execution.</returns>
		public static int ExecuteProc(this IDataContext dataContext, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteProc();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static Task<int> ExecuteProcAsync(this IDataContext dataContext, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteProcAsync(cancellationToken);
		}

		#endregion

		#region Execute[Async] (ExecuteScalar)

		/// <summary>
		/// Executes command and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Resulting value.</returns>
		public static T Execute<T>(this IDataContext dataContext, string sql)
		{
			return SetCommand(dataContext, sql).Execute<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this IDataContext dataContext, string sql, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql).ExecuteAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Resulting value.</returns>
		public static T Execute<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).Execute<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteAsync<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this IDataContext dataContext, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Resulting value.</returns>
		public static T Execute<T>(this IDataContext dataContext, string sql, DataParameter parameter)
		{
			return SetCommand(dataContext, sql, parameter).Execute<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this IDataContext dataContext, string sql, DataParameter parameter, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameter).ExecuteAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Resulting value.</returns>
		public static T Execute<T>(this IDataContext dataContext, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).Execute<T>();
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteAsync<T>(this IDataContext dataContext, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteAsync<T>(cancellationToken);
		}

		#endregion

		#region ExecuteProc[Async] (ExecuteScalar)

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns single value.
		/// Sets result values for output and reference parameters to corresponding parameters in <paramref name="parameters"/>.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Resulting value.</returns>
		public static T ExecuteProc<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteProc<T>();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns single value.
		/// Sets result values for output and reference parameters to corresponding parameters in <paramref name="parameters"/>.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with resulting value.</returns>
		public static Task<T> ExecuteProcAsync<T>(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteProcAsync<T>();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Resulting value.</returns>
		public static Task<T> ExecuteProcAsync<T>(this IDataContext dataContext, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteProcAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Resulting value.</returns>
		public static T ExecuteProc<T>(this IDataContext dataContext, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteProc<T>();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Resulting value.</returns>
		public static Task<T> ExecuteProcAsync<T>(this IDataContext dataContext, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteProcAsync<T>(cancellationToken);
		}

		#endregion

		#region ExecuteReader[Async]

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Data reader object.</returns>
		public static DataReaderAsync ExecuteReader(this IDataContext dataContext, string sql)
		{
			return SetCommand(dataContext, sql).ExecuteReader();
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Data reader object.</returns>
		public static Task<DataReaderAsync> ExecuteReaderAsync(this IDataContext dataContext, string sql, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql).ExecuteReaderAsync(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Data reader object.</returns>
		public static DataReaderAsync ExecuteReader(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteReader();
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Data reader object.</returns>
		public static Task<DataReaderAsync> ExecuteReaderAsync(this IDataContext dataContext, string sql, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteReaderAsync();
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Data reader object.</returns>
		public static Task<DataReaderAsync> ExecuteReaderAsync(this IDataContext dataContext, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteReaderAsync(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Data reader object.</returns>
		public static DataReaderAsync ExecuteReader(this IDataContext dataContext, string sql, DataParameter parameter)
		{
			return SetCommand(dataContext, sql, parameter).ExecuteReader();
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Data reader object.</returns>
		public static Task<DataReaderAsync> ExecuteReaderAsync(this IDataContext dataContext, string sql, DataParameter parameter, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameter).ExecuteReaderAsync(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <returns>Data reader object.</returns>
		public static DataReaderAsync ExecuteReader(this IDataContext dataContext, string sql, object? parameters)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteReader();
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with column name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Data reader object.</returns>
		public static Task<DataReaderAsync> ExecuteReaderAsync(this IDataContext dataContext, string sql, object? parameters, CancellationToken cancellationToken = default)
		{
			return SetCommand(dataContext, sql, parameters).ExecuteReaderAsync(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="commandType">Type of command. See <see cref="CommandType"/> for all supported types.</param>
		/// <param name="commandBehavior">Command behavior flags. See <see cref="CommandBehavior"/> for more details.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Data reader object.</returns>
		public static DataReaderAsync ExecuteReader(
			this IDataContext      dataContext,
			string                 sql,
			CommandType            commandType,
			CommandBehavior        commandBehavior,
			params DataParameter[] parameters)
		{
			return new CommandInfo(dataContext, sql, parameters)
			{
				CommandType     = commandType,
				CommandBehavior = commandBehavior,
			}.ExecuteReader();
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="commandType">Type of command. See <see cref="CommandType"/> for all supported types.</param>
		/// <param name="commandBehavior">Command behavior flags. See <see cref="CommandBehavior"/> for more details.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Data reader object.</returns>
		public static Task<DataReaderAsync> ExecuteReaderAsync(
			this IDataContext      dataContext,
			string                 sql,
			CommandType            commandType,
			CommandBehavior        commandBehavior,
			params DataParameter[] parameters)
		{
			return new CommandInfo(dataContext, sql, parameters)
			{
				CommandType     = commandType,
				CommandBehavior = commandBehavior,
			}.ExecuteReaderAsync();
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="commandType">Type of command. See <see cref="CommandType"/> for all supported types.</param>
		/// <param name="commandBehavior">Command behavior flags. See <see cref="CommandBehavior"/> for more details.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Data reader object.</returns>
		public static Task<DataReaderAsync> ExecuteReaderAsync(
			this IDataContext      dataContext,
			string                 sql,
			CommandType            commandType,
			CommandBehavior        commandBehavior,
			CancellationToken      cancellationToken,
			params DataParameter[] parameters)
		{
			return new CommandInfo(dataContext, sql, parameters)
			{
				CommandType     = commandType,
				CommandBehavior = commandBehavior,
			}.ExecuteReaderAsync(cancellationToken);
		}
		#endregion

		#region BulkCopy[Async] IEnumerable<T>

		/// <summary>
		/// Performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this IDataContext dataContext, BulkCopyOptions options, IEnumerable<T> source)
			where T : class
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			using var _ = ActivityService.Start(ActivityID.BulkCopy);

			return dataContext.GetDataProvider().BulkCopy(
				dataContext.Options.WithOptions(options),
				dataContext.GetTable<T>(),
				source);
		}

		/// <summary>
		/// Performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="maxBatchSize">Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server. </param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this IDataContext dataContext, int maxBatchSize, IEnumerable<T> source)
			where T : class
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			using var _ = ActivityService.Start(ActivityID.BulkCopy);

			return dataContext.GetDataProvider().BulkCopy(
				dataContext.Options.WithOptions<BulkCopyOptions>(o => o with { MaxBatchSize = maxBatchSize }),
				dataContext.GetTable<T>(),
				source);
		}

		/// <summary>
		/// Performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this IDataContext dataContext, IEnumerable<T> source)
			where T : class
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			using var _ = ActivityService.Start(ActivityID.BulkCopy);

			return dataContext.GetDataProvider().BulkCopy(
				dataContext.Options,
				dataContext.GetTable<T>(),
				source);
		}

		/// <summary>
		/// Performs bulk insert operation into table specified in <paramref name="options"/> parameter or into table, identified by <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
			where T : notnull
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			using var _ = ActivityService.Start(ActivityID.BulkCopy);

			DataConnection? dataConnection = null;

			if (options.BulkCopyType == BulkCopyType.RowByRow && !table.TryGetDataConnection(out dataConnection))
			{
				return new RowByRowBulkCopy().BulkCopy(
					BulkCopyType.RowByRow,
					table,
					table.DataContext.Options.WithOptions(options),
					source);
			}

			dataConnection ??= table.GetDataConnection();

			return table.GetDataProvider().BulkCopy(
				dataConnection.Options.WithOptions(options),
				table,
				source);
		}

		/// <summary>
		/// Performs bulk insert operation into table, identified by <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="maxBatchSize">Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server. </param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, int maxBatchSize, IEnumerable<T> source)
			where T : notnull
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			using var _ = ActivityService.Start(ActivityID.BulkCopy);

			var dataConnection = table.GetDataConnection();

			return table.GetDataProvider().BulkCopy(
				dataConnection.Options.WithOptions<BulkCopyOptions>(o => o with { MaxBatchSize = maxBatchSize, }),
				table,
				source);
		}

		/// <summary>
		/// Performs bulk insert operation into table, identified by <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this ITable<T> table, IEnumerable<T> source)
			where T : notnull
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			using var _ = ActivityService.Start(ActivityID.BulkCopy);

			var dataConnection = table.GetDataConnection();

			return table.GetDataProvider().BulkCopy(
				dataConnection.Options,
				table,
				source);
		}

		static Task<BulkCopyRowsCopied> CallMetrics(Func<Task<BulkCopyRowsCopied>> call)
		{
			var a = ActivityService.StartAndConfigureAwait(ActivityID.BulkCopyAsync);

			return a is null ? call() : CallAwait();

			async Task<BulkCopyRowsCopied> CallAwait()
			{
				await using (a)
					return await call().ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Asynchronously performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this IDataContext dataContext, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : class
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (source      == null) throw new ArgumentNullException(nameof(source));

			return CallMetrics(() =>
				dataContext.GetDataProvider().BulkCopyAsync(
					dataContext.Options.WithOptions(options),
					dataContext.GetTable<T>(),
					source,
					cancellationToken));
		}

		/// <summary>
		/// Asynchronously performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="maxBatchSize">Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server. </param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this IDataContext dataContext, int maxBatchSize, IEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : class
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (source      == null) throw new ArgumentNullException(nameof(source));

			return CallMetrics(() =>
				dataContext.GetDataProvider().BulkCopyAsync(
					dataContext.Options.WithOptions<BulkCopyOptions>(o => o with { MaxBatchSize = maxBatchSize }),
					dataContext.GetTable<T>(),
					source,
					cancellationToken));
		}

		/// <summary>
		/// Asynchronously performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this IDataContext dataContext, IEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : class
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (source      == null) throw new ArgumentNullException(nameof(source));

			return CallMetrics(() =>
				dataContext.GetDataProvider().BulkCopyAsync(
					dataContext.Options,
					dataContext.GetTable<T>(),
					source,
					cancellationToken));
		}

		/// <summary>
		/// Asynchronously performs bulk insert operation into table specified in <paramref name="options"/> parameter or into table, identified by <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : notnull
		{
			if (table  == null) throw new ArgumentNullException(nameof(table));
			if (source == null) throw new ArgumentNullException(nameof(source));

			DataConnection? dataConnection = null;

			if (options.BulkCopyType == BulkCopyType.RowByRow && !table.TryGetDataConnection(out dataConnection))
			{
			return CallMetrics(() =>
					new RowByRowBulkCopy().BulkCopyAsync(
						BulkCopyType.RowByRow,
						table,
						table.DataContext.Options.WithOptions(options),
						source,
						cancellationToken));
			}

			dataConnection ??= table.GetDataConnection();

			return CallMetrics(() =>
				table.GetDataProvider().BulkCopyAsync(
					dataConnection.Options.WithOptions(options),
					table,
					source,
					cancellationToken));
		}

		/// <summary>
		/// Asynchronously performs bulk insert operation into table, identified by <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="maxBatchSize">Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server. </param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this ITable<T> table, int maxBatchSize, IEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : notnull
		{
			if (table  == null) throw new ArgumentNullException(nameof(table));
			if (source == null) throw new ArgumentNullException(nameof(source));

			var dataConnection = table.GetDataConnection();

			return CallMetrics(() =>
				table.GetDataProvider().BulkCopyAsync(
					dataConnection.Options.WithOptions<BulkCopyOptions>(o => o with { MaxBatchSize = maxBatchSize, }),
					table,
					source,
					cancellationToken));
		}

		/// <summary>
		/// Asynchronously performs bulk insert operation into table, identified by <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this ITable<T> table, IEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : notnull
		{
			if (table  == null) throw new ArgumentNullException(nameof(table));
			if (source == null) throw new ArgumentNullException(nameof(source));

			var dataConnection = table.GetDataConnection();

			return CallMetrics(() =>
				table.GetDataProvider().BulkCopyAsync(
					dataConnection.Options,
					table,
					source,
					cancellationToken));
		}

		#endregion

		#region BulkCopyAsync IAsyncEnumerable

		/// <summary>
		/// Asynchronously performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this IDataContext dataContext, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : class
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (source      == null) throw new ArgumentNullException(nameof(source));

			return CallMetrics(() =>
				dataContext.GetDataProvider().BulkCopyAsync(
					dataContext.Options.WithOptions(options),
					dataContext.GetTable<T>(),
					source,
					cancellationToken));
		}

		/// <summary>
		/// Asynchronously performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="maxBatchSize">Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server. </param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this IDataContext dataContext, int maxBatchSize, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : class
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (source      == null) throw new ArgumentNullException(nameof(source));

			return CallMetrics(() =>
				dataContext.GetDataProvider().BulkCopyAsync(
					dataContext.Options.WithOptions<BulkCopyOptions>(o => o with { MaxBatchSize = maxBatchSize }),
					dataContext.GetTable<T>(),
					source,
					cancellationToken));
		}

		/// <summary>
		/// Asynchronously performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="dataContext">Database connection.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this IDataContext dataContext, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : class
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (source      == null) throw new ArgumentNullException(nameof(source));

			return CallMetrics(() =>
				dataContext.GetDataProvider().BulkCopyAsync(
					dataContext.Options,
					dataContext.GetTable<T>(),
					source,
					cancellationToken));
		}

		/// <summary>
		/// Asynchronously performs bulk insert operation into table specified in <paramref name="options"/> parameter or into table, identified by <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
		where T: notnull
		{
			if (table  == null) throw new ArgumentNullException(nameof(table));
			if (source == null) throw new ArgumentNullException(nameof(source));

			var dataConnection = table.GetDataConnection();

			return CallMetrics(() =>
				table.GetDataProvider().BulkCopyAsync(
					dataConnection.Options.WithOptions(options),
					table,
					source,
					cancellationToken));
		}

		/// <summary>
		/// Asynchronously performs bulk insert operation into table, identified by <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="maxBatchSize">Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server. </param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this ITable<T> table, int maxBatchSize, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
		where T: notnull
		{
			if (table  == null) throw new ArgumentNullException(nameof(table));
			if (source == null) throw new ArgumentNullException(nameof(source));

			var dataConnection = table.GetDataConnection();

			return CallMetrics(() =>
				table.GetDataProvider().BulkCopyAsync(
					dataConnection.Options.WithOptions<BulkCopyOptions>(o => o with { MaxBatchSize = maxBatchSize }),
					table,
					source,
					cancellationToken));
		}

		/// <summary>
		/// Asynchronously performs bulk insert operation into table, identified by <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="table">Target table.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this ITable<T> table, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
		where T: notnull
		{
			if (table  == null) throw new ArgumentNullException(nameof(table));
			if (source == null) throw new ArgumentNullException(nameof(source));

			var dataConnection = table.GetDataConnection();

			return CallMetrics(() =>
				table.GetDataProvider().BulkCopyAsync(
					dataConnection.Options,
					table,
					source,
					cancellationToken));
		}

		#endregion

		sealed class RowByRowBulkCopy : BasicBulkCopy;
	}
}
