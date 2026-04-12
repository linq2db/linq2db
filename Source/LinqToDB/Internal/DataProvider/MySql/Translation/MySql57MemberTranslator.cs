using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.MySql.Translation
{
	public class MySql57MemberTranslator : MySqlMemberTranslator
	{
		protected class MySql57WindowFunctionsMemberTranslator : MySqlWindowFunctionsMemberTranslator
		{
			// MySQL 5.7 does not support window functions
			protected override bool IsWindowFunctionsSupported => false;
		}

		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new MySql57WindowFunctionsMemberTranslator();
		}
	}
}
