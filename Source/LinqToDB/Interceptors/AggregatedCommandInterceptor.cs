using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	using Common;

	class AggregatedCommandInterceptor : AggregatedInterceptor<ICommandInterceptor>, ICommandInterceptor
	{
		protected override AggregatedInterceptor<ICommandInterceptor> Create()
		{
			return new AggregatedCommandInterceptor();
		}

		public DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					command = interceptor.CommandInitialized(eventData, command);
				return command;
			});
		}

		public Option<object?> ExecuteScalar(CommandEventData eventData, DbCommand command, Option<object?> result)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					result = interceptor.ExecuteScalar(eventData, command, result);
				return result;
			});
		}

		public async Task<Option<object?>> ExecuteScalarAsync(CommandEventData eventData, DbCommand command, Option<object?> result, CancellationToken cancellationToken)
		{
			return await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					result = await interceptor.ExecuteScalarAsync(eventData, command, result, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				return result;
			}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		public Option<int> ExecuteNonQuery(CommandEventData eventData, DbCommand command, Option<int> result)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					result = interceptor.ExecuteNonQuery(eventData, command, result);
				return result;
			});
		}

		public async Task<Option<int>> ExecuteNonQueryAsync(CommandEventData eventData, DbCommand command, Option<int> result, CancellationToken cancellationToken)
		{
			return await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					result = await interceptor.ExecuteNonQueryAsync(eventData, command, result, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				return result;
			}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		public Option<DbDataReader> ExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					result = interceptor.ExecuteReader(eventData, command, commandBehavior, result);
				return result;
			});
		}

		public async Task<Option<DbDataReader>> ExecuteReaderAsync(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken)
		{
			return await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					result = await interceptor.ExecuteReaderAsync(eventData, command, commandBehavior, result, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				return result;
			}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}
	}
}
