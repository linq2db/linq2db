using System;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.DataProvider;

namespace LinqToDB.CommandLine.Commands.Connection
{
	/// <summary>
	/// Resolved provider and options for a database connection, with the optional impersonation session that
	/// database work runs in.
	/// </summary>
	internal sealed class ConnectionScope(DataOptions dataOptions, IDataProvider dataProvider, IImpersonationSession? session) : IDisposable
	{
		public DataOptions   DataOptions  { get; } = dataOptions;
		public IDataProvider DataProvider { get; } = dataProvider;

		/// <summary>
		/// Runs database work, as the impersonated identity when impersonation is enabled.
		/// Only connection opening and SQL execution belong in <paramref name="action"/>: SQL validation and anything
		/// that reads local files runs before, under the original process account.
		/// </summary>
		public Task<T> RunAsync<T>(Func<Task<T>> action)
		{
			return session == null ? action() : session.RunAsync(action);
		}

		public void Dispose()
		{
			session?.Dispose();
		}
	}
}
