using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.QueryExecution;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Test impersonation session: runs work without changing identity and records, per run, what had already
	/// happened when the run started and which assemblies were loaded inside it. <see cref="Current"/> flows with
	/// the run like a Windows impersonation token does, so observers can tell whether an event happened inside.
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

			var run = new ImpersonatedRun(
				errorOutput(),
				AppDomain.CurrentDomain.GetAssemblies().Select(static a => a.GetName().Name!).ToHashSet(StringComparer.Ordinal),
				GetNativeModules());

			lock (Runs)
				Runs.Add(run);

			AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;

			// Set in this async method, so the value flows into the work and is restored for the caller.
			//
			_current.Value = this;

			try
			{
				return await action();
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;

				// Process-wide, so this can include modules another thread loaded meanwhile; tests that check it
				// run one command at a time.
				//
				run.NativeModulesLoadedInside.UnionWith(GetNativeModules().Except(run.NativeModulesAtEntry, StringComparer.OrdinalIgnoreCase));
			}

			void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
			{
				if (Current != this)
					return;

				lock (run.LoadedInside)
					run.LoadedInside.Add(args.LoadedAssembly.GetName().Name!);
			}
		}

		public void Dispose()
		{
			Disposed = true;
		}

		static HashSet<string> GetNativeModules()
		{
			using var process = Process.GetCurrentProcess();

			return process.Modules.Cast<ProcessModule>().Select(static m => m.ModuleName).ToHashSet(StringComparer.OrdinalIgnoreCase);
		}

		internal sealed record ImpersonatedRun(string ErrorOutputAtEntry, HashSet<string> LoadedAtEntry, HashSet<string> NativeModulesAtEntry)
		{
			public HashSet<string> LoadedInside              { get; } = new(StringComparer.Ordinal);
			public HashSet<string> NativeModulesLoadedInside { get; } = new(StringComparer.OrdinalIgnoreCase);
		}
	}
}
