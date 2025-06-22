using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public interface ISimilarityMerger
	{
		IEnumerable<int> GetSimilarityCodes(ISqlPredicate predicate);

		bool TryMerge(NullabilityContext nullabilityContext, ISqlPredicate predicate1, ISqlPredicate predicate2, bool isLogicalOr, out ISqlPredicate? mergedPredicate);
	}
}
