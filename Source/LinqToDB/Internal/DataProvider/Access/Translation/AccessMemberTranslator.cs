using System;
using System.Globalization;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Access.Translation
{
	public class AccessMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
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

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new AccessStringMemberTranslator();
		}

		protected override IMemberTranslator CreateGuidMemberTranslator()
		{
			return new GuidMemberTranslator();
		}

		protected override IMemberTranslator CreateAggregateFunctionsMemberTranslator()
		{
			return new AccessAggregateFunctionsMemberTranslator();
		}

		protected class DateFunctionsTranslator : DateFunctionsTranslatorBase
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
					_                       => null,
				};

				if (partStr == null)
					return null;

				var resultExpression = factory.Function(factory.GetDbDataType(typeof(int)), "DatePart", factory.Value(partStr), dateTimeExpression);

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
					var dayString   = PartExpression(day,   2);

					hour   ??= factory.Value(intDataType, 0);
					minute ??= factory.Value(intDataType, 0);
					second ??= factory.Value(intDataType, 0);

					resultExpression = factory.Concat(
						yearString, factory.Value(stringDataType,                "-"),
						monthString, factory.Value(stringDataType,               "-"), dayString, factory.Value(stringDataType, " "),
						PartExpression(hour,   2), factory.Value(stringDataType, ":"),
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
				var nowExpression = factory.NotNullExpression(factory.GetDbDataType(typeof(DateTime)), "Now");
				return nowExpression;
			}
		}

		protected class MathMemberTranslator : MathMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateRoundToEven(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);
				var intType   = factory.GetDbDataType(typeof(int));

				if (precision is null or SqlValue { Value: 0 })
				{
					/*
					 IIf(Abs([Value] * 10 Mod 10) = 5 And Int([Value]) Mod 2 = 0,
						Int([Value]),
						Round([Value]))
					*/

					var value10 = factory.Multiply(valueType, value, factory.Value(10));
					var mod10   = factory.Mod(value10, factory.Value(10));

					var absMod10 = factory.Function(factory.GetDbDataType(typeof(int)), "ABS", mod10);
					var intCast  = factory.Cast(value, intType);

					var is5    = factory.Equal(absMod10,                               factory.Value(5));
					var isEven = factory.Equal(factory.Mod(intCast, factory.Value(2)), factory.Value(2));

					var condition = factory.SearchCondition()
						.Add(is5)
						.Add(isEven);

					var trueValue  = intCast;
					var falseValue = factory.Function(valueType, "ROUND", value);

					return factory.Condition(condition, trueValue, falseValue);
				}
				else
				{
					return base.TranslateRoundToEven(translationContext, methodCall, value, precision);
				}
			}

			protected override ISqlExpression? TranslateRoundAwayFromZero(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);
				var intType   = factory.GetDbDataType(typeof(int));

				ISqlExpression result;

				if (precision is null or SqlValue { Value: 0 })
				{
					/*
					IIf([Value] >= 0, Int([Value] + 0.5), Int([Value] - 0.5))
					 */

					// Create condition: [Value] >= 0
					var isPositive = factory.GreaterOrEqual(value, factory.Value(valueType, 0));

					// True branch: Int([Value] + 0.5)
					var addHalf        = factory.Add(valueType, value, factory.Value(valueType, 0.5));
					var positiveResult = factory.Function(intType, "Int", addHalf);

					// False branch: Int([Value] - 0.5)
					var subtractHalf   = factory.Sub(valueType, value, factory.Value(valueType, 0.5));
					var negativeResult = factory.Function(intType, "Int", subtractHalf);

					// IIf condition
					var condition = factory.SearchCondition().Add(isPositive);
					result = factory.Condition(condition, positiveResult, negativeResult);
				}
				else
				{
					/*
					Int([Value] * (10 ^ [Precision]) + IIf([Value] >= 0, 0.5, -0.5)) / (10 ^ [Precision])
					 */

					// Calculate 10 ^ [Precision]
					var ten   = factory.Value(valueType, 10);
					var power = factory.Binary(valueType, ten, "^", precision);

					// [Value] * (10 ^ [Precision])
					var scaled = factory.Multiply(valueType, value, power);

					// IIf([Value] >= 0, 0.5, -0.5)
					var isPositive = factory.GreaterOrEqual(value, factory.Value(valueType, 0));
					var condition  = factory.SearchCondition().Add(isPositive);
					var adjustment = factory.Condition(condition,
						factory.Value(valueType, 0.5),
						factory.Value(valueType, -0.5));

					// [Value] * (10 ^ [Precision]) + IIf([Value] >= 0, 0.5, -0.5)
					var adjusted = factory.Add(valueType, scaled, adjustment);

					// Int(...)
					var truncated = factory.Function(valueType, "Int", adjusted);

					// Int(...) / (10 ^ [Precision])
					result = factory.Div(valueType, truncated, power);
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

		protected class AccessStringMemberTranslator : StringMemberTranslatorBase
		{
			public override ISqlExpression? TranslateLPad(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression padding, ISqlExpression paddingChar)
			{
				var factory = translationContext.ExpressionFactory;

				var valueTypeString = factory.GetDbDataType(value);
				var valueTypeInt    = factory.GetDbDataType(typeof(int));

				var lengthValue = TranslateLength(translationContext, translationFlags, value);
				if (lengthValue == null)
					return null;

				var valueSymbolsToAdd = factory.Sub(valueTypeInt, padding, lengthValue);
				var fillingString     = factory.Function(valueTypeString, "STRING", valueSymbolsToAdd, paddingChar);

				return factory.Concat(fillingString, value);
			}

			protected override Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool nullValuesAsEmptyString, bool isNullableResult)
			{
				var builder = new AggregateFunctionBuilder();

				ConfigureConcatWsEmulation(builder, nullValuesAsEmptyString, isNullableResult, (factory, valueType, separator, valuesExpr) =>
				{
					var intDbType = factory.GetDbDataType(typeof(int));
					var substring = factory.Function(valueType, "Mid",
						valuesExpr,
						factory.Add(intDbType, factory.Length(separator), factory.Value(intDbType, 1)));

					return substring;
				});

				return builder.Build(translationContext, methodCall);
			}
		}

		protected class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr,
				TranslationFlags                                                          translationFlags)
			{
				// LCase(Mid(CStr({0}), 2, 36))

				var factory      = translationContext.ExpressionFactory;
				var stringDbType = factory.GetDbDataType(typeof(string));

				var cStrExpression = factory.Function(stringDbType, "CStr", guidExpr);
				var midExpression  = factory.Function(stringDbType, "Mid",  cStrExpression, factory.Value(2), factory.Value(36));
				var toLower        = factory.ToLower(midExpression);

				return toLower;
			}
		}

		protected class AccessAggregateFunctionsMemberTranslator : AggregateFunctionsMemberTranslatorBase
		{
			protected override bool IsCountDistinctSupported       => false;
			protected override bool IsAggregationDistinctSupported => false;
		}
	}
}
