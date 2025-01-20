using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Interceptors;

namespace LinqToDB.EntityFrameworkCore.Tests.Interceptors
{
	public class TestCommandInterceptor : TestInterceptor, ICommandInterceptor
	{
		public void AfterExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, DbDataReader dataReader)
		{
			HasInterceptorBeenInvoked = true;
		}

		public void BeforeReaderDispose(CommandEventData eventData, DbCommand? command, DbDataReader dataReader)
		{
			HasInterceptorBeenInvoked = true;
		}

		public Task BeforeReaderDisposeAsync(CommandEventData eventData, DbCommand? command, DbDataReader dataReader)
		{
			HasInterceptorBeenInvoked = true;
			return Task.CompletedTask;
		}

		public DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
		{
			HasInterceptorBeenInvoked = true;
			return command;
		}

		public Option<int> ExecuteNonQuery(CommandEventData eventData, DbCommand command, Option<int> result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

		public Task<Option<int>> ExecuteNonQueryAsync(CommandEventData eventData, DbCommand command, Option<int> result, CancellationToken cancellationToken)
		{
			HasInterceptorBeenInvoked = true;
			return Task.FromResult(result);
		}

		public Option<DbDataReader> ExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

		public Task<Option<DbDataReader>> ExecuteReaderAsync(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken)
		{
			HasInterceptorBeenInvoked = true;
			return Task.FromResult(result);
		}

		public Option<object?> ExecuteScalar(CommandEventData eventData, DbCommand command, Option<object?> result)
		{
			HasInterceptorBeenInvoked = true;
			return result;
		}

		public Task<Option<object?>> ExecuteScalarAsync(CommandEventData eventData, DbCommand command, Option<object?> result, CancellationToken cancellationToken)
		{
			HasInterceptorBeenInvoked = true;
			return Task.FromResult(result);
		}
	}
}
