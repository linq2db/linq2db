using System.Linq.Expressions;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Access.Translation
{
	public class AccessLibRedMemberTranslator : AccessMemberTranslator
	{
		protected override IMemberTranslator CreateGuidMemberTranslator()
		{
			return new AccessLibRedGuidMemberTranslator();
		}

		protected class AccessLibRedGuidMemberTranslator : GuidMemberTranslator
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr,
				TranslationFlags                                                          translationFlags)
			{
				// LibRed's CStr renders a GUID unbraced (measured: 36 chars), so the base translator's
				// Mid({0}, 2, 36) - which exists to strip Access's leading '{' - drops the first hex digit.
				var factory      = translationContext.ExpressionFactory;
				var stringDbType = factory.GetDbDataType(typeof(string));

				var cStrExpression   = factory.Function(stringDbType, "CStr", guidExpr);
				var toLower          = factory.ToLower(cStrExpression);
				var resultExpression = factory.Condition(factory.IsNullPredicate(guidExpr), factory.Value<string?>(stringDbType, null), factory.NotNull(toLower));

				return resultExpression;
			}
		}
	}
}
