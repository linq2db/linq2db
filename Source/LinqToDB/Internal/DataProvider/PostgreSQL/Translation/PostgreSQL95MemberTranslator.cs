using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.PostgreSQL.Translation
{
	// PostgreSQL 9.5/9.6/10 dialect: FILTER, ordered-set and hypothetical-set aggregates (9.4+) are supported,
	// but the frame GROUPS mode / EXCLUDE clause (11+) are not.
	public class PostgreSQL95MemberTranslator : PostgreSQLMemberTranslator
	{
		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new PostgreSQL95WindowFunctionsMemberTranslator();
		}
	}
}
