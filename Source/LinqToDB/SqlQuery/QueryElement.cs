using System.Collections.Generic;
using System.Threading;

using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	public abstract class QueryElement : IQueryElement
	{
		public static readonly IEqualityComparer<IQueryElement> ReferenceComparer = Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default;

#if DEBUG
		internal static long IdCounter;

		public virtual string DebugText => this.ToDebugString();

		// For debugging purpose. It helps to understand when specific item is created. Consider using DebugAppendUniqueId extension for printing this Id
		public long UniqueId { get; }

		protected QueryElement()
		{
			UniqueId = Interlocked.Increment(ref IdCounter);
		}
#endif

		public abstract QueryElementType       ElementType { get; }
		public abstract QueryElementTextWriter ToString(QueryElementTextWriter writer);

#if OVERRIDETOSTRING
		public override string ToString() => DebugText;
#endif
	}
}
