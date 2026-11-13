using System;
using System.Threading.Tasks;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// A logged-on Windows identity that database work runs under. Disposing it releases the logon token.
	/// </summary>
	public interface IImpersonationSession : IDisposable
	{
		/// <summary>
		/// Runs an asynchronous operation under the session identity. The identity flows across awaits inside the
		/// operation and is restored when the operation completes, fails or is cancelled.
		/// </summary>
		Task<T> RunAsync<T>(Func<Task<T>> action);
	}
}
