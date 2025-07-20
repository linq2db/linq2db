using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.SqlServer.Translation
{
	class SqlServer2012MemberTranslator : SqlServerMemberTranslator
	{
		sealed class SqlServer2012DateFunctionsTranslator : SqlServerDateFunctionsTranslator
		{
			protected override ISqlExpression? TranslateMakeDateTime(
				ITranslationContext translationContext,
				DbDataType          resulType,
				ISqlExpression      year,
				ISqlExpression      month,
				ISqlExpression      day,
				ISqlExpression?     hour,
				ISqlExpression?     minute,
				ISqlExpression?     second,
				ISqlExpression?     millisecond)
			{
				var factory     = translationContext.ExpressionFactory;
				var intDataType = factory.GetDbDataType(typeof(int));

				hour        ??= factory.Value(intDataType, 0);
				minute      ??= factory.Value(intDataType, 0);
				second      ??= factory.Value(intDataType, 0);
				millisecond ??= factory.Value(intDataType, 0);

				var resultExpression = factory.Function(resulType, "DATETIMEFROMPARTS", year, month, day, hour, minute, second, millisecond);

				return resultExpression;
			}
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new SqlServer2012DateFunctionsTranslator();
		}
	}
}
