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
			private protected override ISqlExpression? TranslateDateTimeIntervalDifference(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression leftExpression, ISqlExpression rightExpression, bool isDateTimeOffset)
			{
				var factory      = translationContext.ExpressionFactory;
				var intervalType = factory.GetDbDataType(typeof(System.TimeSpan)).WithDataType(DataType.Int64);
				return factory.Expression(intervalType, "DATEDIFF_BIG(nanosecond, {1}, {0}) / 100", leftExpression, rightExpression);
			}

			protected override ISqlExpression? TranslateZonedUtcNow(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.NotNullExpression(dbDataType, "SYSDATETIMEOFFSET() AT TIME ZONE 'UTC'");
			}
		}
	}
}
