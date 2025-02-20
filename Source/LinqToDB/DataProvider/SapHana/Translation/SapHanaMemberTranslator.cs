﻿using System.Globalization;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SapHana.Translation
{
	public class SapHanaMemberTranslator : ProviderMemberTranslatorDefault
	{
		class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertBit(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Int16));

			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));

			protected override Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Time));

			protected override Expression? ConvertSmallDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.SmallDateTime));

			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

		}

		public class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory   = translationContext.ExpressionFactory;
				var intDbType = factory.GetDbDataType(typeof(int));

				switch (datepart)
				{
					case Sql.DateParts.Year: return factory.Function(intDbType, "Year", dateTimeExpression);
					case Sql.DateParts.Quarter:
					{
						var doubleDbType = factory.GetDbDataType(typeof(double));

						var resultExpression = factory.Increment(
							factory.Function(intDbType, "Floor",
								factory.Div(doubleDbType, factory.Decrement(factory.Function(intDbType, "Month", dateTimeExpression)), factory.Value(3)))
						);

						return resultExpression;
					}
					case Sql.DateParts.Month:     return factory.Function(intDbType, "Month", dateTimeExpression);
					case Sql.DateParts.DayOfYear: return factory.Function(intDbType, "DayOfYear", dateTimeExpression);
					case Sql.DateParts.Day: return factory.Function(intDbType, "DayOfMonth", dateTimeExpression);
					case Sql.DateParts.Week: return factory.Function(intDbType, "Week", dateTimeExpression);
					case Sql.DateParts.WeekDay:
					{
						var resultExpression = factory.Function(intDbType, "Mod",
							ParametersNullabilityType.SameAsFirstParameter,
							factory.Increment(factory.Function(intDbType, "Weekday", dateTimeExpression)),
							factory.Value(7)
						);

						return factory.Increment(resultExpression);
					}
					case Sql.DateParts.Hour:   return factory.Function(intDbType, "Hour", dateTimeExpression);
					case Sql.DateParts.Minute: return factory.Function(intDbType, "Minute", dateTimeExpression);
					case Sql.DateParts.Second: return factory.Function(intDbType, "Second", dateTimeExpression);
					case Sql.DateParts.Millisecond:
					{
						// Not found better solution for this
						var stringDbType = factory.GetDbDataType(typeof(string));
						var result       = factory.Cast(factory.Function(stringDbType, "To_NVarchar", ParametersNullabilityType.SameAsFirstParameter, dateTimeExpression, factory.Value(stringDbType, "FF3")), intDbType);

						return result;
					}
					default:
						return null;
				}
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory       = translationContext.ExpressionFactory;
				var dateType      = factory.GetDbDataType(dateTimeExpression);
				var incrementType = factory.GetDbDataType(increment);

				var number = increment;

				string function;
				switch (datepart)
				{
					case Sql.DateParts.Year:
					{
						function = "Add_Years";
						break;
					}
					case Sql.DateParts.Quarter:
					{
						function = "Add_Months";
						number   = factory.Multiply(number, 3);
						break;
					}
					case Sql.DateParts.Month:
					{
						function = "Add_Months";
						break;
					}
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.Day:
					case Sql.DateParts.WeekDay:
					{
						function = "Add_Days";
						break;
					}
					case Sql.DateParts.Week:
					{
						function = "Add_Days";
						number   = factory.Multiply(number, 7);
						break;
					}
					case Sql.DateParts.Hour:
					{
						function = "Add_Seconds";
						number   = factory.Multiply(number, 3600);
						break;
					}
					case Sql.DateParts.Minute:
						function = "Add_Seconds";
						number   = factory.Multiply(number, 60);
						break;
					case Sql.DateParts.Second:
						function = "Add_Seconds";
						break;
					case Sql.DateParts.Millisecond:
					{
						function = "Add_Seconds";
						number   = factory.Div(incrementType, number, 1000);
						break;
					}
					default:
						return null;
				}

				var resultExpression = factory.Function(dateType, function, dateTimeExpression, number);
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

				ISqlExpression PartExpression(ISqlExpression expression, int padSize)
				{
					if (translationContext.TryEvaluate(expression, out var expressionValue) && expressionValue is int intValue)
					{
						return factory.Value(stringDataType, intValue.ToString(CultureInfo.InvariantCulture).PadLeft(padSize, '0'));
					}

					return factory.Function(stringDataType, "LPad",
						ParametersNullabilityType.SameAsFirstParameter,
						expression,
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

				resultExpression = factory.Function(resulType, "To_Timestamp", resultExpression);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				// TO_DATE(your_datetime_column)

				var factory = translationContext.ExpressionFactory;
				return factory.Function(factory.GetDbDataType(dateExpression), "To_Date", dateExpression);
			}
		}

		protected class SapHanaMathMemberTranslator : MathMemberTranslatorBase
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
			return new SapHanaMathMemberTranslator();
		}

		protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			// Not found working solution for this
			/*var factory    = translationContext.ExpressionFactory;
			var guidType = factory.GetDbDataType(typeof(Guid));
			var sysUUID    = factory.NonPureFragment(guidType, "SYSUUID");

			return sysUUID;*/

			return null;
		}
	}
}
