using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.MySql.Translation
{
	// MariaDB shares the MySQL 8 dialect translator but adds the MariaDB-only UUID_v7() generator.
	// linq2db does not version-split MariaDB, so UUID_v7() is emitted for every MariaDB dialect
	// (the function requires MariaDB 11.7+; older MariaDB versions predate practical support).
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

		protected override ISqlExpression? TranslateNewGuid7Method(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			return factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "UUID_v7");
		}
	}
}
