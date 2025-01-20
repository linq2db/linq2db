using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Interceptors;

using Microsoft.EntityFrameworkCore.Diagnostics;

#if NETFRAMEWORK
using VTask = System.Threading.Tasks.Task;
#else
using VTask = System.Threading.Tasks.ValueTask;
#endif

namespace LinqToDB.EntityFrameworkCore.Tests.Interceptors
{
	public class TestEfCoreAndLinqToDBComboInterceptor : TestInterceptor, ICommandInterceptor, IDbCommandInterceptor
	{
		#region LinqToDBInterceptor
		public void AfterExecuteReader(LinqToDB.Interceptors.CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, DbDataReader dataReader)
		{
			HasInterceptorBeenInvoked = true;
		}

		public void BeforeReaderDispose(LinqToDB.Interceptors.CommandEventData eventData, DbCommand? command, DbDataReader dataReader)
		{
			HasInterceptorBeenInvoked = true;
		}

		public Task BeforeReaderDisposeAsync(LinqToDB.Interceptors.CommandEventData eventData, DbCommand? command, DbDataReader dataReader)
		{
			HasInterceptorBeenInvoked = true;
			return Task.CompletedTask;
		}

		public DbCommand CommandInitialized(LinqToDB.Interceptors.CommandEventData eventData, DbCommand command)
		{
			HasInterceptorBeenInvoked = true;
			return command;
		}

		public Option<int> ExecuteNonQuery(LinqToDB.Interceptors.CommandEventData eventData, DbCommand command, Option<int> result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

		public Task<Option<int>> ExecuteNonQueryAsync(LinqToDB.Interceptors.CommandEventData eventData, DbCommand command, Option<int> result, CancellationToken cancellationToken)
		{
			HasInterceptorBeenInvoked = true;
			return Task.FromResult(result);
		}

		public Option<DbDataReader> ExecuteReader(LinqToDB.Interceptors.CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

		public Task<Option<DbDataReader>> ExecuteReaderAsync(LinqToDB.Interceptors.CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken)
		{
			HasInterceptorBeenInvoked = true;
			return Task.FromResult(result);
		}

		public Option<object?> ExecuteScalar(LinqToDB.Interceptors.CommandEventData eventData, DbCommand command, Option<object?> result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

		public Task<Option<object?>> ExecuteScalarAsync(LinqToDB.Interceptors.CommandEventData eventData, DbCommand command, Option<object?> result, CancellationToken cancellationToken)
		{
			HasInterceptorBeenInvoked = true;
			return Task.FromResult(result);
		}
		#endregion

		#region EF core interceptor

		public DbCommand CommandCreated(CommandEndEventData eventData, DbCommand result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

		public InterceptionResult<DbCommand> CommandCreating(CommandCorrelatedEventData eventData, InterceptionResult<DbCommand> result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

		public void CommandFailed(DbCommand command, CommandErrorEventData eventData)
		{
			HasInterceptorBeenInvoked = true;
		}

		public Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
		{
			HasInterceptorBeenInvoked = true;
			return Task.CompletedTask;
		}
		public InterceptionResult DataReaderDisposing(DbCommand command, DataReaderDisposingEventData eventData, InterceptionResult result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}
		public int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

#if NETFRAMEWORK
		public Task<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
#else
		public ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
#endif
		{
			HasInterceptorBeenInvoked = true;
			return VTask.FromResult(result);
		}

		public InterceptionResult<int> NonQueryExecuting(DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData, InterceptionResult<int> result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

#if NETFRAMEWORK
		public Task<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
#else
		public ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
#endif
		{
			HasInterceptorBeenInvoked = true;
			return VTask.FromResult(result);
		}

		public DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

#if NETFRAMEWORK
		public Task<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
#else
		public ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
#endif
		{
			HasInterceptorBeenInvoked = true;
			return VTask.FromResult(result);
		}

		public InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData, InterceptionResult<DbDataReader> result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

#if NETFRAMEWORK
		public Task<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
#else
		public ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
#endif
		{
			HasInterceptorBeenInvoked = true;
			return VTask.FromResult(result);
		}

		public object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

#if NETFRAMEWORK
		public Task<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
#else
		public ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
#endif
		{
			HasInterceptorBeenInvoked = true;
			return VTask.FromResult(result);
		}

		public InterceptionResult<object> ScalarExecuting(DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData, InterceptionResult<object> result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

#if NETFRAMEWORK
		public Task<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
#else
		public ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
#endif
		{
			HasInterceptorBeenInvoked = true;
			return VTask.FromResult(result);
		}

#endregion
	}
}
