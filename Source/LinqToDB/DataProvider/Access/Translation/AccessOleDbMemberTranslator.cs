using System.Linq.Expressions;

using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Access.Translation
{
	public class AccessOleDbMemberTranslator : AccessMemberTranslator
	{
		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new AccessOleDbStringMemberTranslator();
		}

		class AccessOleDbStringMemberTranslator : StringMemberTranslatorBase
		{
			public override ISqlExpression? TranslateReplace(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression oldValue,
				ISqlExpression                                   newValue)
			{
				// OledDb provider does not support REPLACE function
				return null;
			}
		}
	}
}
