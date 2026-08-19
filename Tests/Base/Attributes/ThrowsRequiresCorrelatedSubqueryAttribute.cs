using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework.Internal;

namespace Tests
{
	public sealed class ThrowsRequiresCorrelatedSubqueryAttribute : ThrowsForProviderAttribute
	{
		// simple: false (default) — no level-0 provider supports the query, so both YDB and ClickHouse throw.
		// simple: true            — a simple correlated subquery: ClickHouse (IsSupportedSimpleCorrelatedSubqueries)
		//                           runs it, so only YDB is expected to throw.
		public ThrowsRequiresCorrelatedSubqueryAttribute(bool simple = false)
			: base(typeof(LinqToDBException),
			simple ? new[] { TestProvName.AllYdb } : new[] { TestProvName.AllYdb, TestProvName.AllClickHouse })
		{
			ErrorMessage = ErrorHelper.Error_Correlated_Subqueries;
		}

		public override void ApplyToTest(Test test)
		{
			base.ApplyToTest(test);
			// Tag so the correlated-subquery tests can be filtered as a group (forward + reverse verification).
			test.Properties.Add(PropertyNames.Category, "CorrelatedSubquery");
		}
	}
}
