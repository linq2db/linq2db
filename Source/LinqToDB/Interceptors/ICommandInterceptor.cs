using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Internal.Common;

namespace LinqToDB.Interceptors
{
	public interface ICommandInterceptor : IInterceptor
	{
		/// <summary>
		/// Event, triggered after command prepared for execution with both command text and parameters set.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Initialized command instance.</param>
		/// <returns>Returns command instance for execution.</returns>
		DbCommand                  CommandInitialized  (CommandEventData eventData, DbCommand command);

		/// <summary>
		/// Event, triggered before command execution using <see cref="DbCommand.ExecuteScalar"/> method.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Command, prepared for execution.</param>
		/// <param name="result">Value, returned by previous interceptor when multiple <see cref="ICommandInterceptor"/> instances registered or <see cref="Option{T}.None"/>.</param>
		/// <returns>
		/// When event returns <see cref="Option{T}.None"/>, Linq To DB will execute command, otherwise it will use returned value as execution result.
		/// </returns>
		Option<object?>            ExecuteScalar       (CommandEventData eventData, DbCommand command, Option<object?> result);

		/// <summary>
		/// Event, triggered before command execution using <see cref="DbCommand.ExecuteScalarAsync(CancellationToken)"/> method.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Command, prepared for execution.</param>
		/// <param name="result">Value, returned by previous interceptor when multiple <see cref="ICommandInterceptor"/> instances registered or <see cref="Option{T}.None"/>.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>
		/// When event returns <see cref="Option{T}.None"/>, Linq To DB will execute command, otherwise it will use returned value as execution result.
		/// </returns>
		Task<Option<object?>>      ExecuteScalarAsync  (CommandEventData eventData, DbCommand command, Option<object?> result, CancellationToken cancellationToken);

		/// <summary>
		/// Event, triggered before command execution using <see cref="DbCommand.ExecuteNonQuery"/> method.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Command, prepared for execution.</param>
		/// <param name="result">Value, returned by previous interceptor when multiple <see cref="ICommandInterceptor"/> instances registered or <see cref="Option{T}.None"/>.</param>
		/// <returns>
		/// When event returns <see cref="Option{T}.None"/>, Linq To DB will execute command, otherwise it will use returned value as execution result.
		/// </returns>
		Option<int>                ExecuteNonQuery     (CommandEventData eventData, DbCommand command, Option<int> result);

		/// <summary>
		/// Event, triggered before command execution using <see cref="DbCommand.ExecuteNonQueryAsync(CancellationToken)"/> method.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Command, prepared for execution.</param>
		/// <param name="result">Value, returned by previous interceptor when multiple <see cref="ICommandInterceptor"/> instances registered or <see cref="Option{T}.None"/>.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>
		/// When event returns <see cref="Option{T}.None"/>, Linq To DB will execute command, otherwise it will use returned value as execution result.
		/// </returns>
		Task<Option<int>>          ExecuteNonQueryAsync(CommandEventData eventData, DbCommand command, Option<int> result, CancellationToken cancellationToken);

		/// <summary>
		/// Event, triggered before command execution using <see cref="DbCommand.ExecuteReader(CommandBehavior)"/> method.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Command, prepared for execution.</param>
		/// <param name="commandBehavior">Behavior, used for command execution.</param>
		/// <param name="result">Value, returned by previous interceptor when multiple <see cref="ICommandInterceptor"/> instances registered or <see cref="Option{T}.None"/>.</param>
		/// <returns>
		/// When event returns <see cref="Option{T}.None"/>, Linq To DB will execute command, otherwise it will use returned value as execution result.
		/// </returns>
		Option<DbDataReader>       ExecuteReader       (CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result);

		/// <summary>
		/// Event, triggered before command execution using <see cref="DbCommand.ExecuteReaderAsync(CommandBehavior, CancellationToken)"/> method.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Command, prepared for execution.</param>
		/// <param name="commandBehavior">Behavior, used for command execution.</param>
		/// <param name="result">Value, returned by previous interceptor when multiple <see cref="ICommandInterceptor"/> instances registered or <see cref="Option{T}.None"/>.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>
		/// When event returns <see cref="Option{T}.None"/>, Linq To DB will execute command, otherwise it will use returned value as execution result.
		/// </returns>
		Task<Option<DbDataReader>> ExecuteReaderAsync  (CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken);

		// no async version for now
		/// <summary>
		/// Event, triggered after command execution using <see cref="DbCommand.ExecuteReader(CommandBehavior)"/> or <see cref="DbCommand.ExecuteReaderAsync(CommandBehavior, CancellationToken)"/> methods.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Executed command.</param>
		/// <param name="commandBehavior">Behavior, used for command execution.</param>
		/// <param name="dataReader"><see cref="DbDataReader"/> instance, returned by <see cref="DbCommand.ExecuteReader(CommandBehavior)"/> or <see cref="DbCommand.ExecuteReaderAsync(CommandBehavior, CancellationToken)"/> methods.</param>
		void AfterExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, DbDataReader dataReader);

		/// <summary>
		/// Event, triggered after all data is consumed from <see cref="DbDataReader"/> before <see cref="IDisposable.Dispose"/> call.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Executed command. Could be <c>null</c>.</param>
		/// <param name="dataReader"><see cref="DbDataReader"/> instance.</param>
		void BeforeReaderDispose(CommandEventData eventData, DbCommand? command, DbDataReader dataReader);

		/// <summary>
		/// Event, triggered after all data is consumed from <see cref="DbDataReader"/> before <c>DisposeAsync</c> call.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Executed command. Could be <c>null</c>.</param>
		/// <param name="dataReader"><see cref="DbDataReader"/> instance.</param>
		Task BeforeReaderDisposeAsync(CommandEventData eventData, DbCommand? command, DbDataReader dataReader);
	}
}
