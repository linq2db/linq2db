using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.MySql.Translation
{
	public class MariaDBMemberTranslator : MySql80MemberTranslator
	{
		protected class MariaDBWindowFunctionsMemberTranslator : MySqlWindowFunctionsMemberTranslator
		{
			// MariaDB 10.3.3+ supports PERCENTILE_CONT / PERCENTILE_DISC (windowed form — OVER is required) and MEDIAN
			// as window functions, unlike MySQL. The group-aggregate (no-OVER) percentile form stays unsupported.
			protected override bool IsOrderedSetWindowedSupported => true;
			protected override bool IsMedianSupported             => true;
			// MariaDB LEAD/LAG accept value + offset only — the default-value (3rd) argument is rejected.
			protected override bool IsLeadLagDefaultSupported     => false;
		}

		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new MariaDBWindowFunctionsMemberTranslator();
		}
	}
}
