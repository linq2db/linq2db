using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.PostgreSQL.Translation
{
	public class PostgreSQL13MemberTranslator : PostgreSQL11MemberTranslator
	{
		protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			return factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "gen_random_uuid");
		}
	}
}
