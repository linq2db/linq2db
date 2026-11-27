using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.QueryExecution;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Test impersonation session: runs work without changing identity and records, per run, what had already
	/// been written to the error output when the run started. <see cref="Current"/> flows with the run like a
	/// Windows impersonation token does, so observers can tell whether an event happened inside.
	/// </summary>
	internal sealed class RecordingImpersonationSession(string user, string password, WindowsImpersonationMode mode, Func<string> errorOutput) : IImpersonationSession
	{
		static readonly AsyncLocal<RecordingImpersonationSession?> _current = new();

		/// <summary>Session whose run the calling code is in, or <see langword="null"/> outside of any run.</summary>
		public static RecordingImpersonationSession? Current => _current.Value;

		public string                   User     { get; } = user;
		public string                   Password { get; } = password;
		public WindowsImpersonationMode Mode     { get; } = mode;
		public List<ImpersonatedRun>    Runs     { get; } = new();
		public bool                     Disposed { get; private set; }

		public async Task<T> RunAsync<T>(Func<Task<T>> action)
		{
			ObjectDisposedException.ThrowIf(Disposed, this);

			lock (Runs)
				Runs.Add(new ImpersonatedRun(errorOutput()));

			// Set in this async method, so the value flows into the work and is restored for the caller.
			//
			_current.Value = this;

			return await action();
		}

		public void Dispose()
		{
			Disposed = true;
		}

		internal sealed record ImpersonatedRun(string ErrorOutputAtEntry);
	}
}
