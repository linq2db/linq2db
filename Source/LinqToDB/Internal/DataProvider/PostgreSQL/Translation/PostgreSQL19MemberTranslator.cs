using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.PostgreSQL.Translation
{
	public class PostgreSQL19MemberTranslator : PostgreSQL18MemberTranslator
	{
		// PostgreSQL 19 adds SQL-standard RESPECT/IGNORE NULLS for value/offset window functions
		// (FIRST_VALUE/LAST_VALUE/NTH_VALUE and LEAD/LAG), emitted after the argument list as
		// FUNC(args) IGNORE NULLS — BasicSqlBuilder's default WindowNullsPlacement.AfterClose.
		protected class PostgreSQL19WindowFunctionsMemberTranslator : PostgreSQLWindowFunctionsMemberTranslator
		{
			protected override bool IsLeadLagNullTreatmentSupported => true;
			protected override bool IsValueNullTreatmentSupported   => true;
		}

		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new PostgreSQL19WindowFunctionsMemberTranslator();
		}
	}
}
