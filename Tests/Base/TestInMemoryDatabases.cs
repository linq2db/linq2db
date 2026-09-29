using System;
using System.Collections.Generic;

namespace Tests
{
	/// <summary>
	/// Holds keep-alive resources (typically a master <c>DbConnection</c>) open for the whole test run
	/// and disposes them at assembly teardown. Provider-agnostic, and despite the name not limited to
	/// in-memory databases — three different reasons to outlive a single test register here:
	/// <list type="bullet">
	/// <item>a shared in-memory database that is destroyed once its last connection closes — SQLite
	/// (<c>...cache=shared</c>) or DuckDB (<c>:memory:?cache=shared</c>) — anchored by one connection so
	/// it survives linq2db's open/close-per-query lifecycle;</item>
	/// <item>a file-based connection whose first open is disproportionately expensive, held so the run
	/// pays that cost once instead of per test — Access over ODBC, where the ACE driver has no pooling
	/// and leaks OS handles on every connect;</item>
	/// <item>a file-based connection whose engine is unsafe to tear down and re-create — Access over
	/// OLE DB, where an open racing the teardown of the last connection access-violates the process.</item>
	/// </list>
	/// No-op unless something is registered (i.e. the normal file-based dev setup).
	/// </summary>
	public static class TestInMemoryDatabases
	{
		static readonly List<IDisposable> _keepAlive = new();

		/// <summary>Hold a connection (or any resource) open for the whole run; disposed at teardown.</summary>
		public static void AddKeepAlive(IDisposable resource)
		{
			lock (_keepAlive)
				_keepAlive.Add(resource);
		}

		/// <summary>Dispose all registered keep-alive resources (assembly teardown).</summary>
		public static void DisposeAll()
		{
			lock (_keepAlive)
			{
				foreach (var r in _keepAlive)
					try { r.Dispose(); } catch { /* best effort */ }

				_keepAlive.Clear();
			}
		}
	}
}
