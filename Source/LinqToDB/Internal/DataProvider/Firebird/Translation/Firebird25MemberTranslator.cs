using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Firebird.Translation
{
	public class Firebird25MemberTranslator : FirebirdMemberTranslator
	{
		// Firebird 2.5 does not support window functions (added in 3.0)
		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new Firebird25WindowFunctionsMemberTranslator();
		}
	}
}
