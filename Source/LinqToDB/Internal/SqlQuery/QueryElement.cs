using System.Collections.Generic;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Base class for SQL AST nodes. Use only if you need to add debug functionality to AST node.
	/// </summary>
	public abstract class QueryElement : IQueryElement
	{
		/// <summary>
		/// By-reference node comparer instance.
		/// </summary>
		public static readonly IEqualityComparer<IQueryElement> ReferenceComparer = Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default;

#if DEBUG
		static long IdCounter;

		public virtual string DebugText => this.ToDebugString();

		// For debugging purpose. It helps to understand when specific item is created. Consider using DebugAppendUniqueId extension for printing this Id
		public long UniqueId { get; }

		protected QueryElement()
		{
			UniqueId = System.Threading.Interlocked.Increment(ref IdCounter);

			// useful for putting breakpoint when finding when QueryElement was created
			if (UniqueId is 0)
			{

			}
		}
#endif

		public abstract QueryElementType       ElementType { get; }
		public abstract QueryElementTextWriter ToString(QueryElementTextWriter writer);
		public abstract int                    GetElementHashCode();

#if OVERRIDETOSTRING
		public override string ToString() => DebugText;
#endif
	}
}
