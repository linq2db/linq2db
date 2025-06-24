using System.Linq.Expressions;

using LinqToDB.Internal.Linq.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Access.Translation
{
	public class AccessJetMemberTranslator : AccessMemberTranslator
	{
		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new AccessJetStringMemberTranslator();
		}

		class AccessJetStringMemberTranslator : StringMemberTranslator
		{
			public override ISqlExpression? TranslateReplace(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression oldValue,
				ISqlExpression                                   newValue)
			{
				// JET provider does not support REPLACE function
				return null;
			}
		}
	}
}
