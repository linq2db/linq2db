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

			/// <summary>
			/// Re-attaches the offset <paramref name="zoned"/> carries in its zone, read off it with
			/// <c>DATEPART(TZOFFSET, …)</c>.
			/// </summary>
			protected override ISqlExpression? FromNamedZoneFrame(ITranslationContext translationContext, SqlTimeZoneConversionExpression zoned, ISqlExpression framed, DbDataType resultType)
			{
				var minutes = TranslateDateTimeOffsetOffsetMinutes(translationContext, zoned);
				if (minutes == null)
					return null;

				return translationContext.ExpressionFactory.Function(resultType, "ToDateTimeOffset", framed, minutes);
			}
		}
	}
}
