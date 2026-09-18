using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Interceptors;
using LinqToDB.Metrics;

namespace LinqToDB.Internal.Interceptors
{
	sealed class AggregatedCommandInterceptor : AggregatedInterceptor<ICommandInterceptor>, ICommandInterceptor
	{
		public DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.CommandInterceptorCommandInitialized))
						command = interceptor.CommandInitialized(eventData, command);
				return command;
			});
		}

		public Option<object?> ExecuteScalar(CommandEventData eventData, DbCommand command, Option<object?> result)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.CommandInterceptorExecuteScalar))
						result = interceptor.ExecuteScalar(eventData, command, result);
				return result;
			});
		}

		public Task<Option<object?>> ExecuteScalarAsync(CommandEventData eventData, DbCommand command, Option<object?> result, CancellationToken cancellationToken)
		{
			return Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandInterceptorExecuteScalarAsync))
						result = await interceptor.ExecuteScalarAsync(eventData, command, result, cancellationToken)
							.ConfigureAwait(false);
				return result;
			});
		}

		public Option<int> ExecuteNonQuery(CommandEventData eventData, DbCommand command, Option<int> result)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.CommandInterceptorExecuteNonQuery))
						result = interceptor.ExecuteNonQuery(eventData, command, result);
				return result;
			});
		}

		public Task<Option<int>> ExecuteNonQueryAsync(CommandEventData eventData, DbCommand command, Option<int> result, CancellationToken cancellationToken)
		{
			return Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandInterceptorExecuteNonQueryAsync))
						result = await interceptor.ExecuteNonQueryAsync(eventData, command, result, cancellationToken)
							.ConfigureAwait(false);
				return result;
			});
		}

		public Option<DbDataReader> ExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.CommandInterceptorExecuteReader))
						result = interceptor.ExecuteReader(eventData, command, commandBehavior, result);
				return result;
			});
		}

		public Task<Option<DbDataReader>> ExecuteReaderAsync(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken)
		{
			return Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandInterceptorExecuteReaderAsync))
						result = await interceptor.ExecuteReaderAsync(eventData, command, commandBehavior, result, cancellationToken)
							.ConfigureAwait(false);
				return result;
			});
		}

		public void AfterExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, DbDataReader dataReader)
		{
			Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.CommandInterceptorAfterExecuteReader))
						interceptor.AfterExecuteReader(eventData, command, commandBehavior, dataReader);
			});
		}

		public void BeforeReaderDispose(CommandEventData eventData, DbCommand? command, DbDataReader dataReader)
		{
			Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.CommandInterceptorBeforeReaderDispose))
						interceptor.BeforeReaderDispose(eventData, command, dataReader);
			});
		}

		public Task BeforeReaderDisposeAsync(CommandEventData eventData, DbCommand? command, DbDataReader dataReader)
		{
			return Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandInterceptorBeforeReaderDisposeAsync))
						await interceptor.BeforeReaderDisposeAsync(eventData, command, dataReader)
							.ConfigureAwait(false);
			});
		}
	}
}
