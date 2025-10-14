using System;

using JetBrains.Annotations;

using LinqToDB.Data;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Various general-purpose helpers.
	/// </summary>
	[PublicAPI]
	public static class Tools
	{
		/// <summary>
		/// Clears all linq2db caches.
		/// </summary>
		public static void ClearAllCaches()
		{
			Query.ClearCaches();
			MappingSchema.ClearCache();
			CommandInfo.ClearObjectReaderCache();
		}
	}
}
