using System;
using System.Collections.Generic;

namespace Tests
{
	/// <summary>
	/// Holds keep-alive resources (typically a master <c>DbConnection</c>) open for the whole test run
	/// and disposes them at assembly teardown. Provider-agnostic: any provider whose CI configuration
	/// uses a shared in-memory database that is destroyed once its last connection closes — SQLite
	/// (<c>...cache=shared</c>) or DuckDB (<c>:memory:?cache=shared</c>) — registers one anchor
	/// connection here so the database survives linq2db's open/close-per-query lifecycle. No-op unless
	/// something is registered (i.e. the normal file-based dev setup).
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
