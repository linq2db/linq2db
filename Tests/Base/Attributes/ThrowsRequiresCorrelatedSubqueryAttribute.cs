using LinqToDB;
using LinqToDB.Internal.Common;

namespace Tests
{
	public sealed class ThrowsRequiresCorrelatedSubqueryAttribute: ThrowsForProviderAttribute
	{
		public ThrowsRequiresCorrelatedSubqueryAttribute()
			: base(typeof(LinqToDBException),
			ProviderName.Ydb, TestProvName.AllClickHouse)
		{
			ErrorMessage = ErrorHelper.Error_Correlated_Subqueries;
		}
	}
}
