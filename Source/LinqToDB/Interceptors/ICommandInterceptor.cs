using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Common;
namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Intercepts command creation and execution around <see cref="DbCommand"/> operations.
	/// </summary>
	/// <remarks>
	/// Use this interface for SQL logging, command rewriting, metrics, result suppression,
	/// and other behaviors that need access to fully prepared commands and their execution results.
	/// Methods are called after SQL text and parameters are assigned and around <see cref="DbCommand"/> execution.
	/// Only execution methods that return <see cref="Option{T}"/> can suppress provider command execution.
	/// Register implementations through <see cref="DataOptionsExtensions.UseInterceptor(DataOptions, IInterceptor)"/>
	/// or <see cref="DataOptionsExtensions.UseInterceptors(DataOptions, System.Collections.Generic.IEnumerable{IInterceptor})"/>.
	/// </remarks>
	public interface ICommandInterceptor : IInterceptor
	{
		/// <summary>
		/// Called after command text and parameters are assigned, before command execution.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Initialized command instance.</param>
		/// <returns>Command instance to execute.</returns>
		DbCommand                  CommandInitialized  (CommandEventData eventData, DbCommand command);
		/// <summary>
		/// Called before command execution using <see cref="DbCommand.ExecuteScalar"/> method.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Command, prepared for execution.</param>
		/// <param name="result">Value, returned by previous interceptor when multiple <see cref="ICommandInterceptor"/> instances registered or <see cref="Option{T}.None"/>.</param>
		/// <returns>
		/// <see cref="Option{T}.None"/> to execute the command; explicit value to suppress execution and use that value as the result.
		/// </returns>
		Option<object?>            ExecuteScalar       (CommandEventData eventData, DbCommand command, Option<object?> result);
		/// <summary>
		/// Called before command execution using <see cref="DbCommand.ExecuteScalarAsync(CancellationToken)"/> method.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Command, prepared for execution.</param>
		/// <param name="result">Value, returned by previous interceptor when multiple <see cref="ICommandInterceptor"/> instances registered or <see cref="Option{T}.None"/>.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>
		/// <see cref="Option{T}.None"/> to execute the command; explicit value to suppress execution and use that value as the result.
		/// </returns>
		Task<Option<object?>>      ExecuteScalarAsync  (CommandEventData eventData, DbCommand command, Option<object?> result, CancellationToken cancellationToken);
		/// <summary>
		/// Called before command execution using <see cref="DbCommand.ExecuteNonQuery"/> method.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Command, prepared for execution.</param>
		/// <param name="result">Value, returned by previous interceptor when multiple <see cref="ICommandInterceptor"/> instances registered or <see cref="Option{T}.None"/>.</param>
		/// <returns>
		/// <see cref="Option{T}.None"/> to execute the command; explicit value to suppress execution and use that value as the result.
		/// </returns>
		Option<int>                ExecuteNonQuery     (CommandEventData eventData, DbCommand command, Option<int> result);
		/// <summary>
		/// Called before command execution using <see cref="DbCommand.ExecuteNonQueryAsync(CancellationToken)"/> method.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Command, prepared for execution.</param>
		/// <param name="result">Value, returned by previous interceptor when multiple <see cref="ICommandInterceptor"/> instances registered or <see cref="Option{T}.None"/>.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>
		/// <see cref="Option{T}.None"/> to execute the command; explicit value to suppress execution and use that value as the result.
		/// </returns>
		Task<Option<int>>          ExecuteNonQueryAsync(CommandEventData eventData, DbCommand command, Option<int> result, CancellationToken cancellationToken);
		/// <summary>
		/// Called before command execution using <see cref="DbCommand.ExecuteReader(CommandBehavior)"/> method.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Command, prepared for execution.</param>
		/// <param name="commandBehavior">Behavior, used for command execution.</param>
		/// <param name="result">Value, returned by previous interceptor when multiple <see cref="ICommandInterceptor"/> instances registered or <see cref="Option{T}.None"/>.</param>
		/// <returns>
		/// <see cref="Option{T}.None"/> to execute the command; explicit reader to suppress execution and use that reader as the result.
		/// </returns>
		Option<DbDataReader>       ExecuteReader       (CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result);
		/// <summary>
		/// Called before command execution using <see cref="DbCommand.ExecuteReaderAsync(CommandBehavior, CancellationToken)"/> method.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Command, prepared for execution.</param>
		/// <param name="commandBehavior">Behavior, used for command execution.</param>
		/// <param name="result">Value, returned by previous interceptor when multiple <see cref="ICommandInterceptor"/> instances registered or <see cref="Option{T}.None"/>.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>
		/// <see cref="Option{T}.None"/> to execute the command; explicit reader to suppress execution and use that reader as the result.
		/// </returns>
		Task<Option<DbDataReader>> ExecuteReaderAsync  (CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken);
		// no async version for now
		/// <summary>
		/// Called after <see cref="DbCommand.ExecuteReader(CommandBehavior)"/> or <see cref="DbCommand.ExecuteReaderAsync(CommandBehavior, CancellationToken)"/> returns a data reader.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Executed command.</param>
		/// <param name="commandBehavior">Behavior, used for command execution.</param>
		/// <param name="dataReader"><see cref="DbDataReader"/> instance, returned by <see cref="DbCommand.ExecuteReader(CommandBehavior)"/> or <see cref="DbCommand.ExecuteReaderAsync(CommandBehavior, CancellationToken)"/> methods.</param>
		void AfterExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, DbDataReader dataReader);
		/// <summary>
		/// Called after all data is consumed from <see cref="DbDataReader"/> and before <see cref="IDisposable.Dispose"/> call.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Executed command. Could be <see langword="null"/>.</param>
		/// <param name="dataReader"><see cref="DbDataReader"/> instance.</param>
		void BeforeReaderDispose(CommandEventData eventData, DbCommand? command, DbDataReader dataReader);
		/// <summary>
		/// Called after all data is consumed from <see cref="DbDataReader"/> and before <c>DisposeAsync</c> call.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Executed command. Could be <see langword="null"/>.</param>
		/// <param name="dataReader"><see cref="DbDataReader"/> instance.</param>
		Task BeforeReaderDisposeAsync(CommandEventData eventData, DbCommand? command, DbDataReader dataReader);
	}
}
