using System;

namespace LinqToDB.Internal.Linq
{
	[Flags]
	public enum QueryFlags
	{
		None                        = 0,
		/// <summary>
		/// Bit set, when inline parameters enabled for connection.
		/// </summary>
		InlineParameters            = 0x02,

		/// <summary>
		/// Indicates that query contains expression, which have been expanded
		/// </summary>
		ExpandedQuery               = 0x04,

		/// <summary>
		/// Bit set when the data context provides an
		/// <see cref="LinqToDB.Interceptors.IEntityServiceInterceptor"/>.
		/// Materialization may invoke entity-creation callbacks, so the cached
		/// query plan is sensitive to it.
		/// </summary>
		HasEntityServiceInterceptor = 0x08,

		/// <summary>
		/// Bit set when <see cref="LinqToDB.Common.Configuration.OptimizeForSequentialAccess"/> is
		/// enabled. The compiled data-reader materialization reads columns in strict ordinal order
		/// for sequential access, so a plan built with this optimization must not be reused by a
		/// reader opened without <see cref="System.Data.CommandBehavior.SequentialAccess"/> (or
		/// vice-versa) — otherwise column reads land out of order.
		/// </summary>
		OptimizeForSequentialAccess = 0x10,
	}
}
