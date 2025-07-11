using System.Collections.Generic;

namespace LinqToDB.Internal.SqlQuery
{
	public interface ISimilarityMerger
	{
		IEnumerable<int> GetSimilarityCodes(ISqlPredicate predicate);

		bool TryMerge(NullabilityContext nullabilityContext, bool isNestedPredicate, ISqlPredicate predicate1, ISqlPredicate predicate2, bool isLogicalOr, out ISqlPredicate? mergedPredicate);

		bool TryMerge(NullabilityContext nullabilityContext, bool isNestedPredicate, ISqlPredicate single, ISqlPredicate predicateFromList, bool isLogicalOr, out ISqlPredicate? mergedSinglePredicate,
			out ISqlPredicate?           mergedListPredicate);
	}
}
