using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
	internal sealed class RecordingImpersonationSession(string user, string password, WindowsImpersonationMode mode, Func<string> errorOutput, Action? runStarting = null) : IImpersonationSession
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

			runStarting?.Invoke();

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
				// Dynamic assemblies (e.g. the one hosting dynamic methods) are created in memory, not loaded from disk.
				//
				if (Current != this || args.LoadedAssembly.IsDynamic)
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
			// On macOS Process.Modules lists only the main executable; the dynamic loader lists every loaded image.
			//
			if (OperatingSystem.IsMacOS())
			{
				var images = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var count  = DyldImageCount();

				for (var i = 0u; i < count; i++)
					if (Marshal.PtrToStringUTF8(DyldGetImageName(i)) is { } name)
						images.Add(name);

				return images;
			}

			using var process = Process.GetCurrentProcess();

			return process.Modules.Cast<ProcessModule>().Select(static m => m.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
		}

		[DllImport("/usr/lib/libSystem.dylib", EntryPoint = "_dyld_image_count")]
		[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
		static extern uint DyldImageCount();

		[DllImport("/usr/lib/libSystem.dylib", EntryPoint = "_dyld_get_image_name")]
		[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
		static extern IntPtr DyldGetImageName(uint imageIndex);

		internal sealed record ImpersonatedRun(string ErrorOutputAtEntry, HashSet<string> LoadedAtEntry, HashSet<string> NativeModulesAtEntry)
		{
			public HashSet<string> LoadedInside              { get; } = new(StringComparer.Ordinal);
			public HashSet<string> NativeModulesLoadedInside { get; } = new(StringComparer.OrdinalIgnoreCase);
		}
	}
}
