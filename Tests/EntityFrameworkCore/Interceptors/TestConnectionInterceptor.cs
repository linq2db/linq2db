using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Interceptors;

namespace LinqToDB.EntityFrameworkCore.Tests.Interceptors
{
	public class TestConnectionInterceptor : TestInterceptor, IConnectionInterceptor
	{
		public void ConnectionOpened(ConnectionEventData eventData, DbConnection connection)
		{
			HasInterceptorBeenInvoked = true;
		}

		public Task ConnectionOpenedAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			HasInterceptorBeenInvoked = true;
			return Task.CompletedTask;
		}

		public void ConnectionOpening(ConnectionEventData eventData, DbConnection connection)
		{
			HasInterceptorBeenInvoked = true;
		}

		public Task ConnectionOpeningAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			HasInterceptorBeenInvoked = true;
			return Task.CompletedTask;
		}
	}
}
