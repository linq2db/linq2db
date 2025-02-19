using System;
using System.Globalization;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Linq.Translation;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Oracle.Translation
{
	public class OracleMemberTranslator : ProviderMemberTranslatorDefault
	{
		class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));

			protected override Expression? ConvertNVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
					return null;

				return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataType.VarChar).WithDbType($"VarChar2({length.ToString(CultureInfo.InvariantCulture)})"));
			}
		}

		public class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var intDataType  = factory.GetDbDataType(typeof(int));
				var dataTimeType = factory.GetDbDataType(dateTimeExpression);

				string? partStr = null;
				string? extractStr = null;

				switch (datepart)
				{
					case Sql.DateParts.Year:      extractStr = "YEAR"; break;
					case Sql.DateParts.Quarter:   partStr    = "Q"; break;
					case Sql.DateParts.Month:     extractStr = "MONTH"; break;
					case Sql.DateParts.DayOfYear: partStr    = "DDD"; break;
					case Sql.DateParts.Day:       extractStr = "DAY"; break;
					case Sql.DateParts.Week:      partStr    = "WW"; break;
					case Sql.DateParts.WeekDay:
					{
						var weekDayFunc = factory.Mod(
							factory.Increment(
								factory.Sub(intDataType,
									factory.Function(dataTimeType, "TRUNC", dateTimeExpression),
									factory.Function(dataTimeType, "TRUNC", ParametersNullabilityType.SameAsFirstParameter, dateTimeExpression, factory.Value("IW"))
								)
							),
							factory.Value(7));

							//var weekDayFunc = factory.Increment(factory.Function(intDataType, "MOD", factory.Function(dataTimeType, "TRUNC", dateTimeExpression), factory.Value(7)));

						return factory.Increment(weekDayFunc);
					}
					case Sql.DateParts.Hour:        extractStr = "HOUR"; break;
					case Sql.DateParts.Minute:      extractStr = "MINUTE"; break;
					case Sql.DateParts.Second:      extractStr = "SECOND"; break;
					case Sql.DateParts.Millisecond: partStr    = "FF"; break;
					default:
						return null;
				}

				var extractDbType = intDataType;

				ISqlExpression resultExpression;

				if (extractStr != null)
				{
					resultExpression = factory.Function(extractDbType, "EXTRACT", factory.Fragment(intDataType, extractStr + " FROM {0}", dateTimeExpression));
				}
				else
				{
					resultExpression = factory.Function(intDataType, "TO_NUMBER", factory.Function(dataTimeType, "TO_CHAR", ParametersNullabilityType.SameAsFirstParameter, dateTimeExpression, factory.Value(partStr)));

					if (datepart == Sql.DateParts.Millisecond)
					{
						resultExpression = factory.Div(intDataType, resultExpression, factory.Value(intDataType, 1000));
					}
				}
					
				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var dateType     = factory.GetDbDataType(dateTimeExpression);
				var intervalType = factory.GetDbDataType(increment).WithDataType(DataType.Interval);

				string expStr;
				switch (datepart)
				{
					case Sql.DateParts.Year:    expStr = "INTERVAL '1' YEAR"; break;
					case Sql.DateParts.Quarter: expStr = "INTERVAL '3' MONTH"; break;
					case Sql.DateParts.Month:   expStr = "INTERVAL '1' MONTH"; break;
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
					case Sql.DateParts.Day:         expStr = "INTERVAL '1' DAY"; break;
					case Sql.DateParts.Week:        expStr = "INTERVAL '7' DAY"; break;
					case Sql.DateParts.Hour:        expStr = "INTERVAL '1' HOUR"; break;
					case Sql.DateParts.Minute:      expStr = "INTERVAL '1' MINUTE"; break;
					case Sql.DateParts.Second:      expStr = "INTERVAL '1' SECOND"; break;
					case Sql.DateParts.Millisecond: expStr = "INTERVAL '0.001' SECOND"; break;
					default:
						return null;
				}

				var intervalExpression = factory.Multiply(intervalType, increment, factory.NotNullFragment(intervalType, expStr));
				var resultExpression   = factory.Add(dateType, dateTimeExpression, intervalExpression);

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
				var stringDataType = factory.GetDbDataType(typeof(string));
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

				hour        ??= factory.Value(intDataType, 0);
				minute      ??= factory.Value(intDataType, 0);
				second      ??= factory.Value(intDataType, 0);
				millisecond ??= factory.Value(intDataType, 0);

				var resultExpression = factory.Concat(
					yearString, factory.Value(stringDataType, "-"),
					monthString, factory.Value(stringDataType, "-"), dayString, factory.Value(stringDataType, " "),
					PartExpression(hour, 2), factory.Value(stringDataType, ":"),
					PartExpression(minute, 2), factory.Value(stringDataType, ":"),
					PartExpression(second, 2), factory.Value(stringDataType, "."),
					PartExpression(millisecond, 3)
				); 
				
				resultExpression = factory.Function(resulType, "TO_TIMESTAMP", ParametersNullabilityType.SameAsFirstParameter, resultExpression, factory.Value(stringDataType, "YYYY-MM-DD HH24:MI:SS.FF3"));

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				// To_Char(t."DateTimeValue", 'HH24:MI:SS')

				var factory = translationContext.ExpressionFactory;
				var dateType = factory.GetDbDataType(dateExpression);

				var resultExpression = factory.Function(dateType.WithDataType(DataType.Time), "TO_CHAR", ParametersNullabilityType.SameAsFirstParameter, dateExpression, factory.Value("HH24:MI:SS"));

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				// Date(your_datetime_column)

				var dateFunc = translationContext.ExpressionFactory.Function(translationContext.GetDbDataType(dateExpression), "TRUNC", dateExpression);

				return dateFunc;
			}
		}

		protected class OracleMathMemberTranslator : MathMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateMaxMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var dbType = factory.GetDbDataType(xValue);

				return factory.Function(dbType, "GREATEST", xValue, yValue);
			}

			protected override ISqlExpression? TranslateMinMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var dbType = factory.GetDbDataType(xValue);

				return factory.Function(dbType, "LEAST", xValue, yValue);
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

		protected override IMemberTranslator CreateMathMemberTranslator()
		{
			return new OracleMathMemberTranslator();
		}

		protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "Sys_Guid");

			return timePart;
		}

	}
}
