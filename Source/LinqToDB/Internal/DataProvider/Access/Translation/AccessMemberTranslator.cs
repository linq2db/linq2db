using System;
using System.Globalization;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Linq.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Access.Translation
{
	public class AccessMemberTranslator : ProviderMemberTranslatorDefault
	{
		class SqlTypesTranslation : SqlTypesTranslationDefault
		{
		}

		public class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory = translationContext.ExpressionFactory;

				var partStr = datepart switch
				{
					Sql.DateParts.Year      => "yyyy",
					Sql.DateParts.Quarter   => "q",
					Sql.DateParts.Month     => "m",
					Sql.DateParts.DayOfYear => "y",
					Sql.DateParts.Day       => "d",
					Sql.DateParts.Week      => "ww",
					Sql.DateParts.WeekDay   => "w",
					Sql.DateParts.Hour      => "h",
					Sql.DateParts.Minute    => "n",
					Sql.DateParts.Second    => "s",
					_ => null,
				};

				if (partStr == null)
					return null;

				var resultExpression = factory.Function(factory.GetDbDataType(typeof(int)), "DatePart", new SqlValue(typeof(string), partStr), dateTimeExpression);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory = translationContext.ExpressionFactory;

				var partStr = datepart switch
				{
					Sql.DateParts.Year      => "yyyy",
					Sql.DateParts.Quarter   => "q",
					Sql.DateParts.Month     => "m",
					Sql.DateParts.DayOfYear => "y",
					Sql.DateParts.Day       => "d",
					Sql.DateParts.Week      => "ww",
					Sql.DateParts.WeekDay   => "w",
					Sql.DateParts.Hour      => "h",
					Sql.DateParts.Minute    => "n",
					Sql.DateParts.Second    => "s",
					_                       => null,
				};

				if (partStr == null)
					return null;

				var resultExpression = factory.Function(factory.GetDbDataType(dateTimeExpression), "DateAdd", factory.Value(partStr), increment, dateTimeExpression);
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
				var factory = translationContext.ExpressionFactory;

				ISqlExpression resultExpression;

				if (hour == null && minute == null && second == null && millisecond == null)
				{
					resultExpression = factory.Function(resulType, "DateSerial", year, month, day);
				}
				else
				{
					if (millisecond != null)
					{
						if (translationContext.TryEvaluate(millisecond, out var msecValue))
						{
							if (msecValue is not int intMsec || intMsec != 0)
								return null;
						}
					}

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
							var padLeft = intValue.ToString(CultureInfo.InvariantCulture).PadLeft(padSize, '0');
							return factory.Value(stringDataType.WithLength(padLeft.Length), padLeft);
						}

						return factory.Function(stringDataType, "Format",
							ParametersNullabilityType.SameAsFirstParameter,
							expression,
							factory.Function(stringDataType, "String", ParametersNullabilityType.NotNullable, factory.Value(stringDataType, "0"), factory.Value(intDataType, padSize))
						);
					}

					var yearString  = CastToLength(year, 4);
					var monthString = PartExpression(month, 2);
					var dayString   = PartExpression(day, 2);

					hour          ??= factory.Value(intDataType, 0);
					minute        ??= factory.Value(intDataType, 0);
					second        ??= factory.Value(intDataType, 0);

					resultExpression = factory.Concat(
						yearString, factory.Value(stringDataType, "-"),
						monthString, factory.Value(stringDataType, "-"), dayString, factory.Value(stringDataType, " "),
						PartExpression(hour, 2), factory.Value(stringDataType, ":"),
						PartExpression(minute, 2), factory.Value(stringDataType, ":"),
						PartExpression(second, 2)
					);

					resultExpression = factory.Cast(resultExpression, resulType);
				}

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var cast    = factory.Cast(dateExpression, new DbDataType(typeof(DateTime), DataType.Date));

				return cast;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory  = translationContext.ExpressionFactory;
				var timePart = factory.Function(factory.GetDbDataType(typeof(TimeSpan)), "TimeValue", dateExpression);

				return timePart;
			}

			protected override ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory       = translationContext.ExpressionFactory;
				var nowExpression = factory.NotNullFragment(factory.GetDbDataType(typeof(DateTime)), "Now");
				return nowExpression;
			}
		}

		class MathMemberTranslator : MathMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateRoundToEven(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);
				var intType   = factory.GetDbDataType(typeof(int));

				ISqlExpression? result = null;

				if (precision == null || precision is SqlValue { Value: 0 })
				{
					/*
					 IIf(Abs([Value] * 10 Mod 10) = 5 And Int([Value]) Mod 2 = 0, 
						Int([Value]), 
						Round([Value]))
					*/

					var value10 = factory.Multiply(valueType, value, factory.Value(10));
					var mod10   = factory.Mod(value10, factory.Value(10));

					var absMod10 = factory.Function(factory.GetDbDataType(typeof(int)), "ABS", mod10);
					var intCast = factory.Cast(value, intType);

					var is5    = factory.Equal(absMod10, factory.Value(5));
					var isEven = factory.Equal(factory.Mod(intCast, factory.Value(2)), factory.Value(2));

					var condition = factory.SearchCondition()
						.Add(is5)
						.Add(isEven);

					var trueValue  = intCast;
					var falseValue = factory.Function(valueType, "ROUND", value);

					result = factory.Condition(condition, trueValue, falseValue);
				}
				else
				{
					result = base.TranslateRoundToEven(translationContext, methodCall, value, precision);
				}

				return result;
			}

			protected override ISqlExpression? TranslatePow(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var xType      = factory.GetDbDataType(xValue);
				var resultType = xType;

				if (xType.SystemType == typeof(decimal))
				{
					xType  = factory.GetDbDataType(typeof(double));
					xValue = factory.Cast(xValue, xType);
				}

				var yType = factory.GetDbDataType(yValue);

				if (!xType.EqualsDbOnly(yType))
				{
					yValue = factory.Cast(yValue, xType);
				}

				var result = factory.Binary(yType, xValue, "^", yValue);

				if (!resultType.EqualsDbOnly(xType))
				{
					result = factory.Cast(result, resultType);
				}

				return result;
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
			return new MathMemberTranslator();
		}
	}
}
