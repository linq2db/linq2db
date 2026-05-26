using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.DB2.Translation
{
	public class DB2zOSMemberTranslator : DB2MemberTranslator
	{
		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new ZOsDateFunctionsTranslator();
		}

		protected class ZOsDateFunctionsTranslator : DateFunctionsTranslator
		{
			protected override ISqlExpression? TranslateServerNow(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var dbType = factory.GetDbDataType(typeof(DateTime));
				return factory.NotNullExpression(dbType, "CURRENT TIMESTAMP WITH TIME ZONE");
			}
		}
	}
}
