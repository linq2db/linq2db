using System.Data.Common;

using LinqToDB.Interceptors;
using LinqToDB.Tools;

namespace LinqToDB.Internal.Interceptors
{
	sealed class AggregatedUnwrapDataObjectInterceptor : AggregatedInterceptor<IUnwrapDataObjectInterceptor>, IUnwrapDataObjectInterceptor
	{
		public DbConnection  UnwrapConnection(IDataContext dataContext, DbConnection  connection)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapConnection))
						connection = interceptor.UnwrapConnection(dataContext, connection);
				return connection;
			});
		}

		public DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapTransaction))
						transaction = interceptor.UnwrapTransaction(dataContext, transaction);
				return transaction;
			});
		}

		public DbCommand UnwrapCommand(IDataContext dataContext, DbCommand command)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapCommand))
						command = interceptor.UnwrapCommand(dataContext, command);
				return command;
			});
		}

		public DbDataReader UnwrapDataReader(IDataContext dataContext, DbDataReader dataReader)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
						dataReader = interceptor.UnwrapDataReader(dataContext, dataReader);
				return dataReader;
			});
		}
	}
}
