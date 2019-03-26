using LinqToDB.Linq.Relinq.Clauses;
using Remotion.Linq;

namespace LinqToDB.Linq.Relinq
{
	public interface IExtendedQueryVisitor : IQueryModelVisitor
	{
        void VisitJoinClause (ExtendedJoinClause joinClause, QueryModel queryModel, int index);
	}
}
