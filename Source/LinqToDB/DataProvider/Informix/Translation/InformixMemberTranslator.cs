using System;
using System.Globalization;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.Informix.Translation
{
	using Common;
	using Linq.Translation;
	using SqlQuery;

	public class InformixMemberTranslator : ProviderMemberTranslatorDefault
	{
		class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertBit(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Boolean));

			protected override Expression? ConvertTinyInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Int16));

			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));
		}

		public class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateMakeDateTime(ITranslationContext translationContext, DbDataType      resulType, ISqlExpression  year, ISqlExpression month, ISqlExpression day, ISqlExpression? hour,
				ISqlExpression?                                                          minute,             ISqlExpression? second,    ISqlExpression? millisecond)
			{
				var factory        = translationContext.ExpressionFactory;
				var intDataType    = factory.GetDbDataType(typeof(int));
				var stringDataType = factory.GetDbDataType(typeof(string));

				ISqlExpression CastToLength(ISqlExpression expression, int stringLength)
				{
					return expression;
					//return factory.Cast(expression, stringDataType.WithLength(stringLength));
				}

				ISqlExpression PartExpression(ISqlExpression expression, int padSize)
				{
					if (translationContext.TryEvaluate(expression, out var expressionValue) && expressionValue is int intValue)
					{
						var padLeft = intValue.ToString(CultureInfo.InvariantCulture).PadLeft(padSize, '0');
						return factory.Value(stringDataType.WithLength(padLeft.Length), padLeft);
					}

					return factory.Function(stringDataType, "LPad",
						CastToLength(expression, padSize),
						factory.Value(intDataType, padSize),
						factory.Value(stringDataType, "0"));
				}

				if (hour == null && minute == null && second == null && millisecond == null)
				{
					var result = factory.Function(resulType, "Mdy", month, day, year);
					return result;
				}

				if (millisecond != null)
				{
					if (translationContext.TryEvaluate(millisecond, out var msecValue))
					{
						if (msecValue is not int intMsec || intMsec != 0)
							return null;
					}
				}

				hour   = hour   ?? factory.Value(intDataType, 0);
				minute = minute ?? factory.Value(intDataType, 0);
				second = second ?? factory.Value(intDataType, 0);

				var yearString  = PartExpression(year, 4);
				var monthString = PartExpression(month, 2);
				var dayString   = PartExpression(day, 2);

				var resultExpression = factory.Concat(
					yearString, factory.Value(stringDataType, "-"),
					monthString, factory.Value(stringDataType, "-"), dayString);

				{
					resultExpression = factory.Concat(
						resultExpression,
						factory.Value(stringDataType, " "),
						PartExpression(hour, 2), factory.Value(stringDataType, ":"),
						PartExpression(minute, 2), factory.Value(stringDataType, ":"),
						PartExpression(second, 2) 
					);
				}

				resultExpression = factory.Function(resulType, "To_Date", resultExpression, factory.Value("%Y-%m-%d %H:%M:%S"));

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory = translationContext.ExpressionFactory;

				var dateType     = factory.GetDbDataType(dateTimeExpression);
				var intDataType  = factory.GetDbDataType(typeof(int));
				var intervalType = factory.GetDbDataType(typeof(TimeSpan));

				switch (datepart)
				{
					case Sql.DateParts.Year:
						return factory.Function(intDataType, "Year", dateTimeExpression);
					case Sql.DateParts.Quarter:
					{
						var month   = factory.Decrement(factory.Function(intDataType, "Month", dateTimeExpression));
						var quarter = factory.Increment(factory.Div(intDataType, month, 3));
						return quarter;
					}
					case Sql.DateParts.Month:
						return factory.Function(intDataType, "Month", dateTimeExpression);
					case Sql.DateParts.DayOfYear:
					{
						var month    = factory.Function(intDataType, "Month", dateTimeExpression);
						var day      = factory.Function(intDataType, "Day", dateTimeExpression);
						var year     = factory.Function(intDataType, "Year", dateTimeExpression);
						var mdy      = factory.Function(dateType, "Mdy", month, day, year);
						var firstDay = factory.Function(dateType, "Mdy", factory.Value(intDataType, 1), factory.Value(intDataType, 1), year);
						var diff     = factory.Sub(intDataType, mdy, firstDay);
						var result   = factory.Increment(diff);
						return result;
					}
					case Sql.DateParts.Day:
						return factory.Function(intDataType, "Day", dateTimeExpression);

					case Sql.DateParts.Week:
					{
						//((Extend({date}, year to day) - (Mdy(12, 31 - WeekDay(Mdy(1, 1, year({date}))), Year({date}) - 1) + Interval(1) day to day)) / 7 + Interval(1) day to day)::char(10)::int

						var dateWithoutTime = factory.Function(dateType, "Extend", dateTimeExpression, factory.Fragment(dateType, "Year to Day"));
						var year            = factory.Function(intDataType, "Year", dateTimeExpression);
						var firstDay        = factory.Function(dateType, "Mdy", factory.Value(intDataType, 1), factory.Value(intDataType, 1), year);

						var lastDay = factory.Function(dateType, "Mdy",
							factory.Value(intDataType, 12),
							factory.Sub(intDataType, factory.Value(intDataType, 31),
								factory.Function(intDataType, "WeekDay", firstDay)
							),
							factory.Decrement(year)
						);

						var interval = factory.Fragment(intervalType, "Interval (1) Day to Day");

						//var result = factory.Sub(dateType, dateWithoutTime, factory.Add(dateType, lastDay, interval));
						var result = factory.Sub(dateType, dateWithoutTime, lastDay);

						result = factory.Div(intDataType,
							result,
							factory.Value(intDataType, 7)
						);

						result = factory.Add(intDataType, result, interval);

						/*
						result = factory.Cast(
							factory.Cast(
								result,
								factory.GetDbDataType(typeof(char)).WithLength(10)),
							intDataType
						);
						*/

						return result;
					}
					case Sql.DateParts.WeekDay:
						return factory.Increment(factory.Function(intDataType, "WeekDay", dateTimeExpression));

					case Sql.DateParts.Hour:
					{
						var result =
							factory.Cast(
								factory.Cast(
									factory.Fragment(intervalType, Precedence.Primary, "{0}::datetime Hour to Hour", dateTimeExpression),
									factory.GetDbDataType(typeof(string)).WithDataType(DataType.Char).WithLength(3)),
								intDataType
							);

						return result;
					}
					case Sql.DateParts.Minute:
					{
						var result =
							factory.Cast(
								factory.Cast(
									factory.Fragment(intervalType, Precedence.Primary, "{0}::datetime Minute to Minute", dateTimeExpression),
									factory.GetDbDataType(typeof(string)).WithDataType(DataType.Char).WithLength(3)),
								intDataType
							);

						return result;
					}
					case Sql.DateParts.Second:
					{
						var result =
							factory.Cast(
								factory.Cast(
									factory.Fragment(intervalType, Precedence.Primary, "{0}::datetime Second to Second", dateTimeExpression),
									factory.GetDbDataType(typeof(string)).WithDataType(DataType.Char).WithLength(3)),
								intDataType
							);

						return result;
					}

					case Sql.DateParts.Millisecond:
						return null;

					default:
						return null;
				}
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment, Sql.DateParts datepart)
			{
				var factory = translationContext.ExpressionFactory;

				var             intervalType  = factory.GetDbDataType(typeof(TimeSpan));
				var             dateType      = factory.GetDbDataType(dateTimeExpression);
				var             incrementType = factory.GetDbDataType(increment);
				//var             intDataType   = factory.GetDbDataType(typeof(int));
				ISqlExpression? multiplier    = null;
				string          fragmentStr;

				switch (datepart)
				{
					case Sql.DateParts.Year: fragmentStr = "Year to Year"; break;
					case Sql.DateParts.Quarter:
					{
						fragmentStr = "Month to Month";
						multiplier  = factory.Value(incrementType, 3);
						break;
					}
					case Sql.DateParts.Month: fragmentStr = "Month to Month"; break;
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
					case Sql.DateParts.Day: fragmentStr = "Day to Day"; break;
					case Sql.DateParts.Week:
					{
						fragmentStr = "Day to Day";
						multiplier  = factory.Value(incrementType, 7);
						break;
					}
					case Sql.DateParts.Hour: fragmentStr = "Hour to Hour"; break;
					case Sql.DateParts.Minute: fragmentStr = "Minute to Minute"; break;
					case Sql.DateParts.Second: fragmentStr = "Second to Second"; break;
					case Sql.DateParts.Millisecond:
					{
						/*fragmentStr = "Second to Fraction (3)";
						multiplier  = factory.Value(intDataType, 1000);
						break;*/

						// Non working code

						return null;
					}
					default:
						return null;
				}

				var intervalExpr     = factory.Fragment(intervalType, "Interval ({0}) " + fragmentStr, increment);
				if (multiplier != null)
				{
					intervalExpr = factory.Multiply(incrementType, intervalExpr, multiplier);
				}

				var resultExpression = factory.Add(dateType, dateTimeExpression, intervalExpr);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				// EXTEND(your_datetime_column, YEAR TO DAY)

				var factory = translationContext.ExpressionFactory;
				var extend  = new SqlFunction(factory.GetDbDataType(dateExpression).WithDataType(DataType.Date), "Extend", dateExpression, new SqlExpression("Year to Day"));

				return extend;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory  = translationContext.ExpressionFactory;
				var timeType = factory.GetDbDataType(typeof(TimeSpan));

				var result =
					factory.Cast(
						factory.Fragment(timeType, Precedence.Additive, "{0}::datetime Hour to Second", dateExpression),
						factory.GetDbDataType(typeof(string)).WithDataType(DataType.Char).WithLength(8));

				return result;
			}

			protected override ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory          = translationContext.ExpressionFactory;
				var currentTimeStamp = factory.NotNullFragment(factory.GetDbDataType(typeof(DateTime)), "CURRENT");
				return currentTimeStamp;
			}
		}

		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new DateFunctionsTranslator();
		}

	}
}
