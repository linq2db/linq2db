using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.PostgreSQL.Translation
{
	public class PostgreSQL18MemberTranslator : PostgreSQL13MemberTranslator
	{
		// uuidv7() is a built-in server function since PostgreSQL 18.
		protected override ISqlExpression? TranslateNewGuid7Method(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "uuidv7");

			return timePart;
		}
	}
}
