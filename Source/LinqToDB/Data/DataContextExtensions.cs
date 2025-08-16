using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Data
{
	/// <summary>
	/// Contains extension methods for <see cref="DataContext"/> class.
	/// </summary>
	[PublicAPI]
	public static class DataContextExtensions
	{
		#region Execute

		/// <summary>
		/// Executes command and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Number of records, affected by command execution.</returns>
		public static int Execute(this DataContext context, string sql)
		{
			var connection = context.GetDataConnection();
			var ret = connection.Execute(sql);
			context.ReleaseQuery();
			return ret;
		}

		/// <summary>
		/// Executes command and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Number of records, affected by command execution.</returns>
		public static int Execute(this DataContext context, string sql, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = connection.Execute(sql, parameters);
			context.ReleaseQuery();
			return ret;
		}

		/// <summary>
		/// Executes command and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
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
		public static int Execute(this DataContext context, string sql, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = connection.Execute(sql, parameters);
			context.ReleaseQuery();
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static async Task<int> ExecuteAsync(this DataContext context, string sql)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync(sql).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static async Task<int> ExecuteAsync(this DataContext context, string sql, CancellationToken cancellationToken)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync(sql, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static async Task<int> ExecuteAsync(this DataContext context, string sql, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync(sql, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static async Task<int> ExecuteAsync(this DataContext context, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync(sql, cancellationToken, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
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
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static async Task<int> ExecuteAsync(this DataContext context, string sql, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync(sql, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
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
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static async Task<int> ExecuteAsync(this DataContext context, string sql, CancellationToken cancellationToken, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync(sql, cancellationToken, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		#endregion

		#region ExecuteProc

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns number of affected records.
		/// Sets result values for output and reference parameters to corresponding parameters in <paramref name="parameters"/>.
		/// </summary>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Number of records, affected by command execution.</returns>
		public static int ExecuteProc(this DataContext context, string sql, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = connection.ExecuteProc(sql, parameters);
			context.ReleaseQuery();
			return ret;
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
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
		public static int ExecuteProc(this DataContext context, string sql, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = connection.ExecuteProc(sql, parameters);
			context.ReleaseQuery();
			return ret;
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns number of affected records.
		/// Sets result values for output and reference parameters to corresponding parameters in <paramref name="parameters"/>.
		/// </summary>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static async Task<int> ExecuteProcAsync(this DataContext context, string sql, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteProcAsync(sql, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
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
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static async Task<int> ExecuteProcAsync(this DataContext context, string sql, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteProcAsync(sql, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static async Task<int> ExecuteProcAsync(this DataContext context, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteProcAsync(sql, cancellationToken, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
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
		/// <returns>Task with number of records, affected by command execution.</returns>
		public static async Task<int> ExecuteProcAsync(this DataContext context, string sql, CancellationToken cancellationToken, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteProcAsync(sql, cancellationToken, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		#endregion

		#region Execute scalar

		/// <summary>
		/// Executes command and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Resulting value.</returns>
		public static T Execute<T>(this DataContext context, string sql)
		{
			var connection = context.GetDataConnection();
			var ret = connection.Execute<T>(sql);
			context.ReleaseQuery();
			return ret;
		}

		/// <summary>
		/// Executes command and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Resulting value.</returns>
		public static T Execute<T>(this DataContext context, string sql, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = connection.Execute<T>(sql, parameters);
			context.ReleaseQuery();
			return ret;
		}

		/// <summary>
		/// Executes command and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Resulting value.</returns>
		public static T Execute<T>(this DataContext context, string sql, DataParameter parameter)
		{
			var connection = context.GetDataConnection();
			var ret = connection.Execute<T>(sql, parameter);
			context.ReleaseQuery();
			return ret;
		}

		/// <summary>
		/// Executes command and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
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
		public static T Execute<T>(this DataContext context, string sql, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = connection.Execute<T>(sql, parameters);
			context.ReleaseQuery();
			return ret;
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns single value.
		/// Sets result values for output and reference parameters to corresponding parameters in <paramref name="parameters"/>.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Resulting value.</returns>
		public static T ExecuteProc<T>(this DataContext context, string sql, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = connection.ExecuteProc<T>(sql, parameters);
			context.ReleaseQuery();
			return ret;
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
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
		public static T ExecuteProc<T>(this DataContext context, string sql, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = connection.ExecuteProc<T>(sql, parameters);
			context.ReleaseQuery();
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <returns>Task with resulting value.</returns>
		public static async Task<T> ExecuteAsync<T>(this DataContext context, string sql)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync<T>(sql).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with resulting value.</returns>
		public static async Task<T> ExecuteAsync<T>(this DataContext context, string sql, CancellationToken cancellationToken)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync<T>(sql, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with resulting value.</returns>
		public static async Task<T> ExecuteAsync<T>(this DataContext context, string sql, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync<T>(sql, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with resulting value.</returns>
		public static async Task<T> ExecuteAsync<T>(this DataContext context, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync<T>(sql, cancellationToken, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Task with resulting value.</returns>
		public static async Task<T> ExecuteAsync<T>(this DataContext context, string sql, DataParameter parameter)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync<T>(sql, parameter).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with resulting value.</returns>
		public static async Task<T> ExecuteAsync<T>(this DataContext context, string sql, DataParameter parameter, CancellationToken cancellationToken)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync<T>(sql, parameter, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
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
		/// <returns>Task with resulting value.</returns>
		public static async Task<T> ExecuteAsync<T>(this DataContext context, string sql, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync<T>(sql, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
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
		/// <returns>Task with resulting value.</returns>
		public static async Task<T> ExecuteAsync<T>(this DataContext context, string sql, CancellationToken cancellationToken, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteAsync<T>(sql, cancellationToken, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns single value.
		/// Sets result values for output and reference parameters to corresponding parameters in <paramref name="parameters"/>.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Task with resulting value.</returns>
		public static async Task<T> ExecuteProcAsync<T>(this DataContext context, string sql, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteProcAsync<T>(sql, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
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
		/// <returns>Task with resulting value.</returns>
		public static async Task<T> ExecuteProcAsync<T>(this DataContext context, string sql, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteProcAsync<T>(sql, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Resulting value.</returns>
		public static async Task<T> ExecuteProcAsync<T>(this DataContext context, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteProcAsync<T>(sql, cancellationToken, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text. This is caller's responsibility to properly escape procedure name.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
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
		public static async Task<T> ExecuteProcAsync<T>(this DataContext context, string sql, CancellationToken cancellationToken, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.ExecuteProcAsync<T>(sql, cancellationToken, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		#endregion

		#region QueryTo async

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static async Task<List<T>> QueryToListAsync<T>(this DataContext context, string sql)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToListAsync<T>(sql).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns async sequence of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Async sequence of records returned by the query.</returns>
		public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(
			this DataContext context,
			string sql)
		{
			return QueryToAsyncEnumerableImpl(context, sql);

			static async IAsyncEnumerable<T> QueryToAsyncEnumerableImpl(
				DataContext context,
				string sql,
				[EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				var connection = context.GetDataConnection();
				await foreach (var item in connection.QueryToAsyncEnumerable<T>(sql).WithCancellation(cancellationToken).ConfigureAwait(false))
				{
					yield return item;
				}

				await context.ReleaseQueryAsync().ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static async Task<T[]> QueryToArrayAsync<T>(this DataContext context, string sql)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToArrayAsync<T>(sql).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static async Task<List<T>> QueryToListAsync<T>(this DataContext context, string sql, CancellationToken cancellationToken)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToListAsync<T>(sql, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static async Task<T[]> QueryToArrayAsync<T>(this DataContext context, string sql, CancellationToken cancellationToken)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToArrayAsync<T>(sql, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static async Task<List<T>> QueryToListAsync<T>(this DataContext context, string sql, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToListAsync<T>(sql, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static async Task<List<T>> QueryToListAsync<T>(this DataContext context, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToListAsync<T>(sql, cancellationToken, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static async Task<List<T>> QueryToListAsync<T>(this DataContext context, string sql, DataParameter parameter)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToListAsync<T>(sql, parameter).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public static async Task<List<T>> QueryToListAsync<T>(this DataContext context, string sql, DataParameter parameter, CancellationToken cancellationToken)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToListAsync<T>(sql, parameter, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
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
		/// <returns>Returns task with list of query result records.</returns>
		public static async Task<List<T>> QueryToListAsync<T>(this DataContext context, string sql, CancellationToken cancellationToken, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToListAsync<T>(sql, cancellationToken, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static async Task<T[]> QueryToArrayAsync<T>(this DataContext context, string sql, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToArrayAsync<T>(sql, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static async Task<T[]> QueryToArrayAsync<T>(this DataContext context, string sql, CancellationToken cancellationToken, params DataParameter[] parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToArrayAsync<T>(sql, cancellationToken, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static async Task<T[]> QueryToArrayAsync<T>(this DataContext context, string sql, DataParameter parameter)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToArrayAsync<T>(sql, parameter).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public static async Task<T[]> QueryToArrayAsync<T>(this DataContext context, string sql, DataParameter parameter, CancellationToken cancellationToken)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToArrayAsync<T>(sql, parameter, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
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
		/// <returns>Returns task with array of query result records.</returns>
		public static async Task<T[]> QueryToArrayAsync<T>(this DataContext context, string sql, CancellationToken cancellationToken, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToArrayAsync<T>(sql, cancellationToken, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns async sequence of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="sql">Command text.</param>
		/// <param name="parameters">Command parameters.</param>
		/// <returns>Async sequence of records returned by the query.</returns>
		public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(
			this DataContext context,
			string sql,
			params DataParameter[] parameters)
		{
			return QueryToAsyncEnumerableImpl(context, sql, parameters);

			static async IAsyncEnumerable<T> QueryToAsyncEnumerableImpl(
				DataContext context,
				string sql,
				DataParameter[] parameters,
				[EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				var connection = context.GetDataConnection();
				await foreach (var item in connection.QueryToAsyncEnumerable<T>(sql, parameters).WithCancellation(cancellationToken).ConfigureAwait(false))
				{
					yield return item;
				}

				await context.ReleaseQueryAsync().ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
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
		/// <returns>Returns task with list of query result records.</returns>
		public static async Task<List<T>> QueryToListAsync<T>(this DataContext context, string sql, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToListAsync<T>(sql, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
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
		/// <returns>Returns task with array of query result records.</returns>
		public static async Task<T[]> QueryToArrayAsync<T>(this DataContext context, string sql, object? parameters)
		{
			var connection = context.GetDataConnection();
			var ret = await connection.QueryToArrayAsync<T>(sql, parameters).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}

		/// <summary>
		/// Executes command asynchronously and returns async sequence of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="context">Database context.</param>
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
		public static IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(
			this DataContext context,
			string sql,
			object? parameters)
		{
			return QueryToAsyncEnumerableImpl(context, sql, parameters);

			static async IAsyncEnumerable<T> QueryToAsyncEnumerableImpl(
				DataContext context,
				string sql,
				object? parameters,
				[EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				var connection = context.GetDataConnection();
				await foreach (var item in connection.QueryToAsyncEnumerable<T>(sql, parameters).WithCancellation(cancellationToken).ConfigureAwait(false))
				{
					yield return item;
				}

				await context.ReleaseQueryAsync().ConfigureAwait(false);
			}
		}

		#endregion

		#region BulkCopy
	
		/// <summary>
		/// Performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this DataContext context, BulkCopyOptions options, IEnumerable<T> source)
			where T : class
		{
			var connection = context.GetDataConnection();
			var ret = connection.BulkCopy(options, source);
			context.ReleaseQuery();
			return ret;
		}
	
		/// <summary>
		/// Performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="maxBatchSize">Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this DataContext context, int maxBatchSize, IEnumerable<T> source)
			where T : class
		{
			var connection = context.GetDataConnection();
			var ret = connection.BulkCopy(maxBatchSize, source);
			context.ReleaseQuery();
			return ret;
		}
	
		/// <summary>
		/// Performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this DataContext context, IEnumerable<T> source)
			where T : class
		{
			var connection = context.GetDataConnection();
			var ret = connection.BulkCopy(source);
			context.ReleaseQuery();
			return ret;
		}
	
		/// <summary>
		/// Asynchronously performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this DataContext context, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : class
		{
			var connection = context.GetDataConnection();
			var ret = await connection.BulkCopyAsync(options, source, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}
	
		/// <summary>
		/// Asynchronously performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="maxBatchSize">Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this DataContext context, int maxBatchSize, IEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : class
		{
			var connection = context.GetDataConnection();
			var ret = await connection.BulkCopyAsync(maxBatchSize, source, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}
	
		/// <summary>
		/// Asynchronously performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this DataContext context, IEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : class
		{
			var connection = context.GetDataConnection();
			var ret = await connection.BulkCopyAsync(source, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}
	
		/// <summary>
		/// Asynchronously performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this DataContext context, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : class
		{
			var connection = context.GetDataConnection();
			var ret = await connection.BulkCopyAsync(options, source, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}
	
		/// <summary>
		/// Asynchronously performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="maxBatchSize">Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this DataContext context, int maxBatchSize, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : class
		{
			var connection = context.GetDataConnection();
			var ret = await connection.BulkCopyAsync(maxBatchSize, source, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}
	
		/// <summary>
		/// Asynchronously performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="context">Database context.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(this DataContext context, IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
			where T : class
		{
			var connection = context.GetDataConnection();
			var ret = await connection.BulkCopyAsync(source, cancellationToken).ConfigureAwait(false);
			await context.ReleaseQueryAsync().ConfigureAwait(false);
			return ret;
		}
	
		#endregion
	}
}
