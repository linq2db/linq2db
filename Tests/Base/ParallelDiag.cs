using System;
using System.IO;

using NUnit.Framework;

namespace Tests
{
	// TEMPORARY diagnostics for the parallel CreateDatabase-seeding investigation (PR #5614).
	// Writes to a file (survives non-test-thread output, which the console logger drops) AND, best
	// effort, to the captured test output. Grep CI logs / trx for the "PDIAG" marker. Remove once
	// the CreateDatabase-first issue is resolved.
	public static class ParallelDiag
	{
		static readonly object _sync = new object();
		static readonly string _path = Path.Combine(AppContext.BaseDirectory, "parallel-diag.log");

		public static void Log(string msg)
		{
			var line = $"{DateTime.UtcNow:HH:mm:ss.fff} t{Environment.CurrentManagedThreadId:000} {msg}";

			lock (_sync)
			{
				try { File.AppendAllText(_path, line + "\n"); } catch { /* best effort */ }
			}

			// Captured when called from a test thread (SetUp/TearDown/body); no-op/throws off-thread.
			try { TestContext.Out.WriteLine("PDIAG " + line); } catch { /* off test thread */ }
		}

		// Dump the file at assembly teardown so the non-test-thread (dispatcher) lines have a chance
		// to reach the captured output too.
		public static void Dump()
		{
			try
			{
				foreach (var l in File.ReadLines(_path))
					TestContext.Progress.WriteLine("PDIAGFILE " + l);
			}
			catch { /* best effort */ }
		}
	}
}
