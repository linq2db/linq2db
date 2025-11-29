using System;
using System.Globalization;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Informix.Translation
{
	public class InformixMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new DateFunctionsTranslator();
		}

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new StringMemberTranslator();
		}

		protected override IMemberTranslator CreateGuidMemberTranslator()
		{
			return new GuidMemberTranslator();
		}

		protected class SqlTypesTranslation : SqlTypesTranslationDefault
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

		protected class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateMakeDateTime(ITranslationContext translationContext, DbDataType      resulType, ISqlExpression  year, ISqlExpression month, ISqlExpression day, ISqlExpression? hour,
				ISqlExpression?                                                          minute,             ISqlExpression? second,    ISqlExpression? millisecond)
			{
				var factory        = translationContext.ExpressionFactory;
				var intDataType    = factory.GetDbDataType(typeof(int));
				var stringDataType = factory.GetDbDataType(typeof(string));

				ISqlExpression PartExpression(ISqlExpression expression, int padSize)
				{
					if (translationContext.TryEvaluate(expression, out var expressionValue) && expressionValue is int intValue)
					{
						var padLeft = intValue.ToString(CultureInfo.InvariantCulture).PadLeft(padSize, '0');
						return factory.Value(stringDataType.WithLength(padLeft.Length), padLeft);
					}

					return factory.Function(stringDataType, "LPad",
						ParametersNullabilityType.SameAsFirstParameter,
						expression,
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

				resultExpression = factory.Function(resulType, "To_Date", ParametersNullabilityType.SameAsFirstParameter, resultExpression, factory.Value("%Y-%m-%d %H:%M:%S"));

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
						var firstDay = factory.Function(dateType, "Mdy", ParametersNullabilityType.SameAsLastParameter, factory.Value(intDataType, 1), factory.Value(intDataType, 1), year);
						var diff     = factory.Sub(intDataType, mdy, firstDay);
						var result   = factory.Increment(diff);
						return result;
					}
					case Sql.DateParts.Day:
						return factory.Function(intDataType, "Day", dateTimeExpression);

					case Sql.DateParts.Week:
					{
						//((Extend({date}, year to day) - (Mdy(12, 31 - WeekDay(Mdy(1, 1, year({date}))), Year({date}) - 1) + Interval(1) day to day)) / 7 + Interval(1) day to day)::char(10)::int

						var dateWithoutTime = factory.Function(dateType, "Extend", ParametersNullabilityType.SameAsFirstParameter, dateTimeExpression, factory.NotNullExpression(dateType, "Year to Day"));
						var year            = factory.Function(intDataType, "Year", dateTimeExpression);
						var firstDay        = factory.Function(dateType, "Mdy", ParametersNullabilityType.SameAsLastParameter, factory.Value(intDataType, 1), factory.Value(intDataType, 1), year);

						var lastDay = factory.Function(dateType, "Mdy",
							factory.Value(intDataType, 12),
							factory.Sub(intDataType, factory.Value(intDataType, 31),
								factory.Function(intDataType, "WeekDay", firstDay)
							),
							factory.Decrement(year)
						);

						var interval = factory.NotNullExpression(intervalType, "Interval (1) Day to Day");

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
									factory.Cast(dateTimeExpression, intervalType.WithDbType("datetime Hour to Hour"), isMandatory: true),
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
									factory.Cast(dateTimeExpression, intervalType.WithDbType("datetime Minute to Minute"), isMandatory: true),
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
									factory.Cast(dateTimeExpression, intervalType.WithDbType("datetime Second to Second"), isMandatory: true),
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

				// interval literal cannot be dynamic so we should try to disable at least parameters
				QueryHelper.MarkAsNonQueryParameters(increment);

				var intervalExpr     = factory.NotNullExpression(intervalType, "Interval ({0}) " + fragmentStr, increment);
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
				var extend  = factory.Function(factory.GetDbDataType(dateExpression).WithDataType(DataType.Date), "Extend", ParametersNullabilityType.SameAsFirstParameter, dateExpression, factory.Fragment("Year to Day"));

				return extend;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory  = translationContext.ExpressionFactory;
				var timeType = factory.GetDbDataType(typeof(TimeSpan)).WithDbType("datetime Hour to Second");

				var result =
					factory.Cast(
						factory.Cast(dateExpression, timeType, isMandatory: true),
						factory.GetDbDataType(typeof(string)).WithDataType(DataType.Char).WithLength(8));

				return result;
			}

			protected override ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory          = translationContext.ExpressionFactory;
				var currentTimeStamp = factory.NotNullExpression(factory.GetDbDataType(typeof(DateTime)), "CURRENT");
				return currentTimeStamp;
			}

			protected override ISqlExpression? TranslateSqlCurrentTimestampUtc(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				// "datetime(1970-01-01 00:00:00) year to second + (dbinfo('utc_current')/86400)::int::char(9)::interval day(9) to day + (mod(dbinfo('utc_current'), 86400))::char(5)::interval second(5) to second

				var factory = translationContext.ExpressionFactory;

				return factory.Expression(dbDataType,
					"datetime(1970-01-01 00:00:00) year to second + (dbinfo('utc_current')/86400)::int::char(9)::interval day(9) to day + (mod(dbinfo('utc_current'), 86400))::char(5)::interval second(5) to second", Precedence.Additive);
			}
		}

		protected class StringMemberTranslator : StringMemberTranslatorBase
		{
			protected override Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool nullValuesAsEmptyString, bool isNullableResult)
			{
				var builder = new AggregateFunctionBuilder();

				ConfigureConcatWsEmulation(builder, nullValuesAsEmptyString, isNullableResult, (factory, valueType, separator, valuesExpr) =>
				{
					var intDbType = factory.GetDbDataType(typeof(int));
					var substring = factory.Function(valueType, "SUBSTRING",
						[new SqlFunctionArgument(valuesExpr, suffix: factory.Fragment("FROM {0}", factory.Add(intDbType, factory.Length(separator), factory.Value(intDbType, 1))))],
						[true],
						canBeNull: true
					);

					return substring;
				});

				return builder.Build(translationContext, methodCall);
			}
		}

		protected class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
		{
				// Lower(To_Char({0}))

				var factory        = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string));
				var toChar         = factory.Function(stringDataType, "To_Char", guidExpr);
				var toLower        = factory.ToLower(toChar);

				return toLower;
		}
		}
	}
}
