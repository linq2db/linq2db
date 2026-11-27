using System;
using System.Security.Principal;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Runs operations under a Windows access token.
	/// </summary>
	public sealed class WindowsImpersonationSession : IImpersonationSession
	{
		readonly SafeAccessTokenHandle _token;

		/// <summary>
		/// Creates a session that owns <paramref name="token"/> and disposes it with the session.
		/// </summary>
		public WindowsImpersonationSession(SafeAccessTokenHandle token)
		{
			_token = token;
		}

		/// <inheritdoc />
		public Task<T> RunAsync<T>(Func<Task<T>> action)
		{
			if (!OperatingSystem.IsWindows())
				throw new PlatformNotSupportedException("Windows impersonation is supported only on Windows.");

			return WindowsIdentity.RunImpersonatedAsync(_token, action);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_token.Dispose();
		}
	}
}
