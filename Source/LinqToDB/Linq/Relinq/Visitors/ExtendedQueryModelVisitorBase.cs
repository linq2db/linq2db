using LinqToDB.Linq.Relinq.Clauses;
using Remotion.Linq;

namespace LinqToDB.Linq.Relinq.Visitors
{
	public class ExtendedQueryModelVisitorBase : QueryModelVisitorBase, IExtendedQueryVisitor
	{
		public virtual void VisitJoinClause(ExtendedJoinClause joinClause, QueryModel queryModel, int index)
		{
		}
	}
}
