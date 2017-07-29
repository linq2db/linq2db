using System;
using System.Threading;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Provides a scope, in which LINQ queries will not be added to a LINQ query cache. This could be used to tell
	/// linq2db to not cache queries that operate with big parametes.
	/// More details could be found here <see cref="https://github.com/linq2db/linq2db/issues/256" />.
	/// Take into account that this class only disables adding of new query, created in its scope, to a cache.
	/// If query already present in cache - linq2db will use cached query.
	/// </summary>
	public class NoLinqCache : IDisposable
	{
		private static readonly AsyncLocal<bool> _value = new AsyncLocal<bool>();

		/// <summary>
		/// Creates disposable no-cache scope.
		/// </summary>
		public static IDisposable Scope()
		{
			return new NoLinqCache();
		}

		private NoLinqCache()
		{
			_value.Value = true;
		}

		void IDisposable.Dispose()
		{
			_value.Value = false;
		}

		internal static bool IsNoCache
		{
			get
			{
				var rv = _value.Value;
				Console.WriteLine(rv);
				return rv;
			}
		}
	}
}