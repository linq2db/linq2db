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
	}
}
