using System.Globalization;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Linq.Translation;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DB2.Translation
{
	public class DB2MemberTranslator : ProviderMemberTranslatorDefault
	{
		class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));

			protected override Expression? ConvertDefaultNChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			{
				var dbDataType = translationContext.MappingSchema.GetDbDataType(typeof(char));

				dbDataType = dbDataType.WithDataType(DataType.Char).WithSystemType(typeof(string)).WithLength(null);

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new SqlValue(dbDataType, ""), memberExpression);
			}

			protected override Expression? ConvertNVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
					return null;

				return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataType.Char));
			}

			protected override Expression? ConvertDefaultNVarChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			{
				var dbDataType = translationContext.MappingSchema.GetDbDataType(typeof(string));

				dbDataType = dbDataType.WithDataType(DataType.Char);

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new SqlValue(dbDataType, ""), memberExpression);
			}

			protected override Expression? ConvertDefaultChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			{
				var dbDataType = translationContext.MappingSchema.GetDbDataType(typeof(char));
				dbDataType = dbDataType.WithSystemType(typeof(string)).WithDataType(DataType.Char).WithLength(null);

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new SqlValue(dbDataType, ""), memberExpression);
			}

			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));
		}

		public class DateFunctionsTranslator : DateFunctionsTranslatorBase
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
				var factory        = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string)).WithDataType(DataType.NVarChar);
				var intDataType    = factory.GetDbDataType(typeof(int));

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

			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var intDataType  = factory.GetDbDataType(typeof(int));
				var dataTimeType = factory.GetDbDataType(dateTimeExpression);

				string? partStr = null;
				string? extractStr = null;

				switch (datepart)
				{
					case Sql.DateParts.Year: extractStr = "year"; break;
					case Sql.DateParts.Quarter: partStr = "Q"; break;
					case Sql.DateParts.Month: extractStr = "month"; break;
					case Sql.DateParts.DayOfYear: partStr = "DDD"; break;
					case Sql.DateParts.Day: extractStr = "day"; break;
					case Sql.DateParts.Week: partStr = "WW"; break;
					case Sql.DateParts.WeekDay:
					{
						var weekDayFunc = factory.Mod(
							factory.Increment(
								factory.Sub(intDataType,
									factory.Function(dataTimeType, "Trunc", dateTimeExpression),
									factory.Function(dataTimeType, "Trunc", ParametersNullabilityType.SameAsFirstParameter, dateTimeExpression, factory.Value("IW"))
								)
							),
							factory.Value(7));

						return factory.Increment(weekDayFunc);
					}
					case Sql.DateParts.Hour: extractStr = "hour"; break;
					case Sql.DateParts.Minute: extractStr = "minute"; break;
					case Sql.DateParts.Second: extractStr = "second"; break;
					case Sql.DateParts.Millisecond: partStr = "FF"; break;
					default:
						return null;
				}

				var extractDbType = intDataType;

				ISqlExpression resultExpression;

				if (extractStr != null)
				{
					resultExpression = factory.Function(extractDbType, "Extract", factory.Fragment(intDataType, extractStr + " from {0}", dateTimeExpression));
				}
				else
				{
					resultExpression = factory.Function(intDataType, "To_Number",
						factory.Function(dataTimeType, "To_Char", ParametersNullabilityType.SameAsFirstParameter, dateTimeExpression, factory.Value(partStr)));

					if (datepart == Sql.DateParts.Millisecond)
					{
						resultExpression = factory.Div(intDataType, resultExpression, factory.Value(intDataType, 1000));
					}
				}

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts datepart)
			{
				var factory            = translationContext.ExpressionFactory;
				var dateType           = factory.GetDbDataType(dateTimeExpression);
				var incrementValueType = factory.GetDbDataType(increment);
				var doubleDataType     = factory.GetDbDataType(typeof(double));
				var intervalType       = factory.GetDbDataType(increment).WithDataType(DataType.Interval);

				var incrementValueExpr = increment;

				string expStr;
				switch (datepart)
				{
					case Sql.DateParts.Year: expStr = "YEAR"; break;
					case Sql.DateParts.Quarter:
					{
						expStr             = "MONTH";
						incrementValueExpr = factory.Multiply(incrementValueType, increment, 3);
						break;
					}
					case Sql.DateParts.Month: expStr = "MONTH"; break;
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
					case Sql.DateParts.Day: expStr = "DAY"; break;
					case Sql.DateParts.Week:
					{
						expStr             = "DAY";
						incrementValueExpr = factory.Multiply(incrementValueType, increment, 7);
						break;
					}
					case Sql.DateParts.Hour: expStr = "HOUR"; break;
					case Sql.DateParts.Minute: expStr = "MINUTE"; break;
					case Sql.DateParts.Second: expStr = "SECOND"; break;
					case Sql.DateParts.Millisecond:
					{
						expStr             = "MICROSECONDS";
						incrementValueExpr = factory.Multiply(doubleDataType, increment, 1000.0);
						break;
					}
					default:
						return null;
				}

				var intervalExpression = factory.Fragment(intervalType, "{0} " + expStr, incrementValueExpr);
				var resultExpression   = factory.Add(dateType, dateTimeExpression, intervalExpression);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var dateFunction = new SqlFunction(dateExpression.SystemType!, "DATE", dateExpression);

				return dateFunction;
			}
		}

		protected class DB2MathMemberTranslator : MathMemberTranslatorBase
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
			return new DB2MathMemberTranslator();
		}
	}
}
