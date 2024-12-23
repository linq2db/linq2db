using System.Linq.Expressions;

using LinqToDB.Internals.Linq.Translation;
using LinqToDB.Internals.SqlQuery;

namespace LinqToDB.DataProvider.Access.Translation
{
	public class AccessJetMemberTranslator : AccessMemberTranslator
	{
		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new AccessJetStringMemberTranslator();
		}

		class AccessJetStringMemberTranslator : StringMemberTranslatorBase
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
