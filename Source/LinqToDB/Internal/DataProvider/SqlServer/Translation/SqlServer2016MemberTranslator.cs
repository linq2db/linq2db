using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.SqlServer.Translation
{
	public class SqlServer2016MemberTranslator : SqlServer2012MemberTranslator
	{
		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new SqlServer2016DateFunctionsTranslator();
		}

		protected class SqlServer2016DateFunctionsTranslator : SqlServer2012DateFunctionsTranslator
		{
			protected override ISqlExpression? TranslateZonedUtcNow(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.NotNullExpression(dbDataType, "SYSDATETIMEOFFSET() AT TIME ZONE 'UTC'");
			}
		}
	}
}
