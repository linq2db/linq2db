using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.PostgreSQL.Translation
{
	// PostgreSQL 11/12 dialect: adds the window-frame GROUPS mode and the frame EXCLUDE clause on top of the 9.5 tier.
	public class PostgreSQL11MemberTranslator : PostgreSQL95MemberTranslator
	{
		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new PostgreSQL11WindowFunctionsMemberTranslator();
		}
	}
}
