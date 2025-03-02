using System;
using System.Globalization;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Linq.Translation;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Firebird.Translation
{
	public class FirebirdMemberTranslator : ProviderMemberTranslatorDefault
	{
		class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(18, 10));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));

			protected override Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertSmallDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertNVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
					return null;

				return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataType.VarChar));
			}
		}

		protected class FirebirdDateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory          = translationContext.ExpressionFactory;
				var intDataType      = factory.GetDbDataType(typeof(int));
				var shortIntDataType = factory.GetDbDataType(typeof(short));

				string partStr;

				switch (datepart)
				{
					case Sql.DateParts.Year: partStr = "year"; break;
					case Sql.DateParts.Quarter:
					{
						var result = factory.Function(shortIntDataType, "Extract", factory.Fragment(shortIntDataType, "Month from {0}", dateTimeExpression));

						result = factory.Increment(factory.Div(shortIntDataType, factory.Decrement(result), 3));
						return result;
					}
					case Sql.DateParts.Month:       partStr = "month"; break;
					case Sql.DateParts.DayOfYear:   partStr = "yearday"; break;
					case Sql.DateParts.Day:         partStr = "day"; break;
					case Sql.DateParts.Week:        partStr = "week"; break;
					case Sql.DateParts.WeekDay:     partStr = "weekday"; break;
					case Sql.DateParts.Hour:        partStr = "hour"; break;
					case Sql.DateParts.Minute:      partStr = "minute"; break;
					case Sql.DateParts.Second:      partStr = "second"; break;
					case Sql.DateParts.Millisecond: partStr = "millisecond"; break;
					default:
						return null;
				}

				// Cast(Floor(Extract({part} from {date})) as int)

				var extractDbType = shortIntDataType;

				switch (datepart)
				{
					case Sql.DateParts.Second:
						extractDbType = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(9, 4);
						break;
					case Sql.DateParts.Millisecond:
						extractDbType = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(9, 1);
						break;
				}

				var resultExpression =
					factory.Function(extractDbType, "Extract", factory.Fragment(shortIntDataType, partStr + " from {0}", dateTimeExpression));

				switch (datepart)
				{
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
					{
						resultExpression = factory.Increment(resultExpression);
						break;
					}
					case Sql.DateParts.Second:
					case Sql.DateParts.Millisecond:
					{
						resultExpression = factory.Cast(factory.Function(factory.GetDbDataType(typeof(long)), "Floor", resultExpression), intDataType);
						break;
					}
				}

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory = translationContext.ExpressionFactory;

				var number = increment;
				switch (datepart)
				{
					case Sql.DateParts.Quarter:
					{
						datepart = Sql.DateParts.Month;
						number   = factory.Multiply(number, 3);
						break;
					}				
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
					{
						datepart = Sql.DateParts.Day;
						break;
					}	
					case Sql.DateParts.Week:
					{
						datepart = Sql.DateParts.Day;
						number   = factory.Multiply(number, 7);
						break;
					}
				}

				// Firebird does not support dynamic increment in DateAdd function
				QueryHelper.MarkAsNonQueryParameters(number);

				var partExpression   = factory.NotNullFragment(factory.GetDbDataType(typeof(string)), datepart.ToString());
				var resultExpression = factory.Function(factory.GetDbDataType(dateTimeExpression), "DateAdd", partExpression, number, dateTimeExpression);

				return resultExpression;
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
				var stringDataType = factory.GetDbDataType(typeof(string)).WithDataType(DataType.VarChar);
				var intDataType    = factory.GetDbDataType(typeof(int));

				ISqlExpression CastToLength(ISqlExpression expression, int stringLength)
				{
					return factory.Cast(expression, stringDataType.WithLength(stringLength));
				}

				ISqlExpression PartExpression(ISqlExpression expression, int padSize)
				{
					if (translationContext.TryEvaluate(expression, out var expressionValue) && expressionValue is int intValue)
					{
						return factory.Value(stringDataType, intValue.ToString(CultureInfo.InvariantCulture).PadLeft(padSize, '0'));
					}

					return factory.Function(stringDataType, "LPad",
						ParametersNullabilityType.SameAsFirstParameter,
						CastToLength(expression, padSize),
						factory.Value(intDataType, padSize),
						factory.Value(stringDataType, "0"));
				}

				var yearString  = PartExpression(year, 4);
				var monthString = PartExpression(month, 2);
				var dayString   = PartExpression(day, 2);

				var resultExpression = factory.Concat(
					yearString, factory.Value(stringDataType, "-"),
					monthString, factory.Value(stringDataType, "-"), dayString);

				if (hour != null || minute != null || second != null || millisecond != null)
				{
					hour        ??= factory.Value(intDataType, 0);
					minute      ??= factory.Value(intDataType, 0);
					second      ??= factory.Value(intDataType, 0);
					millisecond ??= factory.Value(intDataType, 0);

					resultExpression = factory.Concat(
						resultExpression,
						factory.Value(stringDataType, " "),
						PartExpression(hour, 2), factory.Value(stringDataType, ":"),
						PartExpression(minute, 2), factory.Value(stringDataType, ":"),
						PartExpression(second, 2), factory.Value(stringDataType, "."),
						PartExpression(millisecond, 3)
					);
				}

				resultExpression = factory.Cast(resultExpression, resulType);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var cast = factory.Cast(dateExpression, factory.GetDbDataType(dateExpression).WithDataType(DataType.Date), true);

				return cast;
			}

			protected override ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.NotNullFragment(factory.GetDbDataType(typeof(DateTime)), "LOCALTIMESTAMP");
			}
		}

		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new FirebirdDateFunctionsTranslator();
		}

		protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "Gen_Uuid");

			return timePart;
		}
	}
}
