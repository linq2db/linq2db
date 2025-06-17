using System;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.ClickHouse.Translation
{
	public class ClickHouseMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new DateFunctionsTranslator();
		}

		protected override IMemberTranslator CreateMathMemberTranslator()
		{
			return new MathMemberTranslator();
		}

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new StringMemberTranslator();
		}

		protected override IMemberTranslator CreateGuidMemberTranslator()
		{
			return new GuidMemberTranslator();
		}

		class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal128).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal128).WithPrecisionScale(10, 4));

			protected override Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime64));

			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime64));
		}

		public class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var intDataType  = factory.GetDbDataType(typeof(int));
				var longDataType = factory.GetDbDataType(typeof(long));

				switch (datepart)
				{
					case Sql.DateParts.Year:        return factory.Function(intDataType, "toYear", dateTimeExpression);
					case Sql.DateParts.Quarter:     return factory.Function(intDataType, "toQuarter", dateTimeExpression);
					case Sql.DateParts.Month:       return factory.Function(intDataType, "toMonth", dateTimeExpression);
					case Sql.DateParts.DayOfYear:   return factory.Function(intDataType, "toDayOfYear", dateTimeExpression);
					case Sql.DateParts.Day:         return factory.Function(intDataType, "toDayOfMonth", dateTimeExpression);
					case Sql.DateParts.Week:        return factory.Function(intDataType, "toISOWeek", factory.Function(longDataType, "toDateTime64", ParametersNullabilityType.SameAsFirstParameter, dateTimeExpression, factory.Value(intDataType, 1)));
					case Sql.DateParts.Hour:        return factory.Function(intDataType, "toHour", dateTimeExpression);
					case Sql.DateParts.Minute:      return factory.Function(intDataType, "toMinute", dateTimeExpression);
					case Sql.DateParts.Second:      return factory.Function(intDataType, "toSecond", dateTimeExpression);
					case Sql.DateParts.WeekDay:     return factory.Function(intDataType, "toDayOfWeek", factory.Function(intDataType, "addDays", ParametersNullabilityType.SameAsFirstParameter, dateTimeExpression, factory.Value(intDataType, 1)));
					case Sql.DateParts.Millisecond: return factory.Mod(factory.Function(intDataType, "toUnixTimestamp64Milli", dateTimeExpression), 1000);
					default:                        return null;
				}
			}

			protected override ISqlExpression? TranslateDateTimeOffsetDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				return TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var intDataType  = factory.GetDbDataType(typeof(int));
				var longDataType = factory.GetDbDataType(typeof(long));
				var dateType     = factory.GetDbDataType(dateTimeExpression);

				string? function;
				switch (datepart)
				{
					case Sql.DateParts.Year:    function = "addYears"; break;
					case Sql.DateParts.Quarter: function = "addQuarters"; break;
					case Sql.DateParts.Month:   function = "addMonths"; break;
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.Day:
					case Sql.DateParts.WeekDay: function = "addDays"; break;
					case Sql.DateParts.Week:    function = "addWeeks"; break;
					case Sql.DateParts.Hour:    function = "addHours"; break;
					case Sql.DateParts.Minute:  function = "addMinutes"; break;
					case Sql.DateParts.Second:  function = "addSeconds"; break;
					case Sql.DateParts.Millisecond:
					{
						var resultExpression = factory.Function(dateType, "fromUnixTimestamp64Nano",
							factory.Add(
								longDataType,
								factory.Function(longDataType, "toUnixTimestamp64Nano", dateTimeExpression),
								factory.Cast(factory.Multiply(factory.GetDbDataType(increment), increment, 1000000), longDataType)
							)
						);

						return resultExpression;
					}
					default:
						return null;
				}

				var result = factory.Function(dateType, function, dateTimeExpression, increment);
				return result;
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
				var factory     = translationContext.ExpressionFactory;
				var dateType    = resulType;
				var intDataType = factory.GetDbDataType(typeof(int));

				ISqlExpression resultExpression;

				if (millisecond == null)
				{
					resultExpression = factory.Function(dateType, "makeDateTime", year, month, day,
						hour        ?? factory.Value(intDataType, 0),
						minute      ?? factory.Value(intDataType, 0),
						second      ?? factory.Value(intDataType, 0)
					);
				}
				else
				{
					resultExpression = factory.Function(dateType, "makeDateTime64",
						year, month, day,
						hour        ?? factory.Value(intDataType, 0),
						minute      ?? factory.Value(intDataType, 0),
						second      ?? factory.Value(intDataType, 0),
						millisecond
					);

					resultExpression = factory.Cast(resultExpression, dateType.WithDataType(DataType.DateTime64));
				}

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var cast = new SqlCastExpression(dateExpression, new DbDataType(typeof(DateTime), DataType.Date32), null, true);
				return cast;
			}

			protected override ISqlExpression? TranslateDateTimeOffsetTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var cast = new SqlCastExpression(dateExpression, new DbDataType(typeof(DateTime), DataType.Date32), null, true);
				return cast;
			}

			static ISqlExpression? CommonTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression)
			{
				//toInt64((toUnixTimestamp64Nano(toDateTime64(t.DateTimeValue, 7)) - toUnixTimestamp64Nano(toDateTime64(toDate32(t.DateTimeValue), 7))) / 100)
				var factory        = translationContext.ExpressionFactory;
				var longDataType   = factory.GetDbDataType(typeof(long));
				var intDataType    = factory.GetDbDataType(typeof(int));
				var resultDataType = longDataType.WithSystemType(typeof(TimeSpan));
				var doubleDataType = factory.GetDbDataType(typeof(double));
				var dateTime64     = factory.GetDbDataType(dateExpression).WithDataType(DataType.DateTime64);
				var dateTime32     = factory.GetDbDataType(dateExpression).WithDataType(DataType.DateTime);

				var precision = factory.Value(intDataType, 7);

				var resultExpression = factory.Cast(
					factory.Div(
						doubleDataType,
						factory.Sub(
							longDataType,
							factory.Function(longDataType, "toUnixTimestamp64Nano", factory.Function(dateTime64, "toDateTime64", dateExpression, precision)),
							factory.Function(longDataType, "toUnixTimestamp64Nano", factory.Function(dateTime64, "toDateTime64", factory.Function(dateTime32, "toDate32", dateExpression), precision))
						),
						factory.Value(intDataType, 100)),
					resultDataType);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				return CommonTruncationToTime(translationContext, dateExpression);
			}

			protected override ISqlExpression? TranslateDateTimeOffsetTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				return CommonTruncationToTime(translationContext, dateExpression);
			}

			protected override ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory     = translationContext.ExpressionFactory;
				var nowFunction = factory.Function(factory.GetDbDataType(typeof(DateTime)), "now", ParametersNullabilityType.NotNullable);
				return nowFunction;
			}
		}

		class MathMemberTranslator : MathMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateRoundToEven(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				var factory = translationContext.ExpressionFactory;

				var valueType = factory.GetDbDataType(value);

				ISqlExpression result;

				if (precision != null)
					result = factory.Function(valueType, "roundBankers", value, precision);
				else
					result = factory.Function(valueType, "roundBankers", value);
				
				return result;
			}
		}

		public class StringMemberTranslator : StringMemberTranslatorBase
		{
		}

		protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "generateUUIDv4");

			return timePart;
		}

		class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
			{
				// lower(toString({0}))

				var factory        = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string));
				var toChar         = factory.Function(stringDataType, "toString", guidExpr);
				var toLower        = factory.ToLower(toChar);

				return toLower;
			}
		}
	}
}
