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
				var factory      = translationContext.ExpressionFactory;
				var intDataType  = factory.GetDbDataType(typeof(int));

				string? partStr;

				switch (datepart)
				{
					case Sql.DateParts.Year        : partStr = "year";      break;
					case Sql.DateParts.Quarter     : partStr = "quarter";   break;
					case Sql.DateParts.Month       : partStr = "month";     break;
					case Sql.DateParts.DayOfYear   : partStr = "dayofyear"; break;
					case Sql.DateParts.Day         : partStr = "day";       break;
					case Sql.DateParts.Week        : partStr = "week";      break;
					case Sql.DateParts.WeekDay     : partStr = "dow";       break;
					case Sql.DateParts.Hour        : partStr = "hour";      break;
					case Sql.DateParts.Minute      : partStr = "minute";    break;
					case Sql.DateParts.Second      : partStr = "second";    break;
					case Sql.DateParts.Millisecond :
					{
						// EXTRACT(millisecond FROM ...) returns total ms including seconds (e.g. 56789 for 56.789s)
						// Use modulo 1000 to get just the millisecond part
						var extractExpr = new SqlExpression(intDataType, "EXTRACT(millisecond FROM {0})", Precedence.Primary, dateTimeExpression);
						return factory.Mod(extractExpr, 1000);
					}
					default:
						return null;
				}

				ISqlExpression resultExpression = new SqlExpression(intDataType, $"EXTRACT({partStr} FROM {{0}})", Precedence.Primary, dateTimeExpression);

				if (datepart == Sql.DateParts.WeekDay)
				{
					resultExpression = factory.Increment(resultExpression);
				}

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment, Sql.DateParts datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var intervalType = factory.GetDbDataType(typeof(System.TimeSpan)).WithDataType(DataType.Interval);

				ISqlExpression ToInterval(ISqlExpression numberExpression, string intervalKind)
				{
					var intervalExpr = factory.NotNullExpression(intervalType, "Interval {0}", factory.Value(intervalKind));
					return factory.Multiply(intervalType, numberExpression, intervalExpr);
				}

				ISqlExpression intervalExpr;
				switch (datepart)
				{
					case Sql.DateParts.Year:        intervalExpr = ToInterval(increment, "1 Year");        break;
					case Sql.DateParts.Quarter:      intervalExpr = factory.Multiply(intervalType, ToInterval(increment, "1 Month"), 3); break;
					case Sql.DateParts.Month:        intervalExpr = ToInterval(increment, "1 Month");       break;
					case Sql.DateParts.Week:         intervalExpr = factory.Multiply(intervalType, ToInterval(increment, "1 Day"), 7); break;
					case Sql.DateParts.Day:          intervalExpr = ToInterval(increment, "1 Day");         break;
					case Sql.DateParts.Hour:         intervalExpr = ToInterval(increment, "1 Hour");        break;
					case Sql.DateParts.Minute:       intervalExpr = ToInterval(increment, "1 Minute");      break;
					case Sql.DateParts.Second:       intervalExpr = ToInterval(increment, "1 Second");      break;
					case Sql.DateParts.Millisecond:  intervalExpr = ToInterval(increment, "1 Millisecond"); break;
					default:
						return null;
				}

				return factory.Add(factory.GetDbDataType(dateTimeExpression), dateTimeExpression, intervalExpr);
			}

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
				var factory        = translationContext.ExpressionFactory;
				var dateType       = resulType;
				var intDataType    = factory.GetDbDataType(typeof(int));
				var doubleDataType = factory.GetDbDataType(typeof(double));

				hour   = hour   == null ? factory.Value(intDataType, 0) : factory.Cast(hour, intDataType);
				minute = minute == null ? factory.Value(intDataType, 0) : factory.Cast(minute, intDataType);
				second = second == null ? factory.Value(doubleDataType, 0.0) : factory.Cast(second, doubleDataType);

				if (millisecond != null)
				{
					millisecond = factory.Cast(millisecond, doubleDataType);
					second      = factory.Add(doubleDataType, second, factory.Div(doubleDataType, millisecond, 1000));
				}

				return factory.Function(dateType, "make_timestamp", year, month, day, hour, minute, second);
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
