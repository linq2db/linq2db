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
			/// <summary>
			/// The elapsed difference needs <c>DATEDIFF_BIG</c>, which arrived in 2016. The 32-bit <c>DATEDIFF</c>
			/// overflows after about 24 days in milliseconds, so earlier versions leave date subtraction alone.
			/// </summary>
			private protected override bool CanTranslateDateDifference => true;

			protected override ISqlExpression? TranslateZonedUtcNow(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.NotNullExpression(dbDataType, "SYSDATETIMEOFFSET() AT TIME ZONE 'UTC'");
			}
		}
	}
}
