using System;
using System.Data.Common;

namespace LinqToDB.Interceptors
{
	class AggregatedUnwrapDataObjectInterceptor : AggregatedInterceptor<IUnwrapDataObjectInterceptor>, IUnwrapDataObjectInterceptor
	{
		protected override AggregatedInterceptor<IUnwrapDataObjectInterceptor> Create()
		{
			return new AggregatedUnwrapDataObjectInterceptor();
		}

		public DbConnection  UnwrapConnection(IDataContext dataContext, DbConnection  connection)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					connection = interceptor.UnwrapConnection(dataContext, connection);
				return connection;
			});
		}

		public DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					transaction = interceptor.UnwrapTransaction(dataContext, transaction);
				return transaction;
			});
		}

		public DbCommand UnwrapCommand(IDataContext dataContext, DbCommand command)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					command = interceptor.UnwrapCommand(dataContext, command);
				return command;
			});
		}

		public DbDataReader UnwrapDataReader(IDataContext dataContext, DbDataReader dataReader)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					dataReader = interceptor.UnwrapDataReader(dataContext, dataReader);
				return dataReader;
			});
		}
	}
}
