using System.Linq.Expressions;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DuckDB.Translation
{
	public class DuckDBMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected override IMemberTranslator CreateSqlTypesTranslator()    => new SqlTypesTranslation();
		protected override IMemberTranslator CreateDateMemberTranslator()  => new DateFunctionsTranslator();
		protected override IMemberTranslator CreateStringMemberTranslator()=> new StringMemberTranslator();

		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));

			protected override Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime2));

			protected override Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime2));
		}

		protected class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory     = translationContext.ExpressionFactory;
				var intDataType = factory.GetDbDataType(typeof(int));

				var partStr = datepart switch
				{
					Sql.DateParts.Year         => "year",
					Sql.DateParts.Quarter      => "quarter",
					Sql.DateParts.Month        => "month",
					Sql.DateParts.DayOfYear    => "dayofyear",
					Sql.DateParts.Day          => "day",
					Sql.DateParts.Week         => "week",
					Sql.DateParts.WeekDay      => "isodow",
					Sql.DateParts.Hour         => "hour",
					Sql.DateParts.Minute       => "minute",
					Sql.DateParts.Second       => "second",
					Sql.DateParts.Millisecond  => "millisecond",
					_                          => null,
				};

				if (partStr == null)
					return null;

				return new SqlExpression(intDataType, $"EXTRACT({partStr} FROM {{0}})", Precedence.Primary, dateTimeExpression);
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory  = translationContext.ExpressionFactory;
				var dateType = factory.GetDbDataType(typeof(System.DateTime)).WithDataType(DataType.Date);

				return factory.Cast(dateExpression, dateType);
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory  = translationContext.ExpressionFactory;
				var timeType = factory.GetDbDataType(typeof(System.TimeSpan)).WithDataType(DataType.Time);

				return factory.Cast(dateExpression, timeType);
			}
		}

		protected class StringMemberTranslator : StringMemberTranslatorBase
		{
		}
	}
}
