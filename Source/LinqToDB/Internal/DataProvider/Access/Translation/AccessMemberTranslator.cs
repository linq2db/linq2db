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
			private protected override ISqlExpression? TranslateNativeTimeSpanPart(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression timeSpanExpression, TimeSpanPart part, Type resultType)
			{
				var factory        = translationContext.ExpressionFactory;
				var expressionType = factory.GetDbDataType(timeSpanExpression);

				if (expressionType.DataType != DataType.Decimal)
					return null;

				var decimalType = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(18, 0);
				var doubleType  = factory.GetDbDataType(typeof(double));
				var ticks       = factory.Expression(decimalType, "{0}", timeSpanExpression);

				ISqlExpression Truncate(ISqlExpression expression)
				{
					return factory.Function(decimalType, "Fix", expression);
				}

				ISqlExpression Divide(long divisor)
				{
					return Truncate(factory.Div(decimalType, ticks, factory.Value(decimalType, (decimal)divisor)));
				}

				ISqlExpression Component(long divisor, int modulo)
				{
					var value    = Divide(divisor);
					var quotient = Truncate(factory.Div(decimalType, value, factory.Value(decimalType, (decimal)modulo)));
					return factory.Sub(decimalType, value, factory.Multiply(decimalType, quotient, (decimal)modulo));
				}

				ISqlExpression Total(double divisor)
				{
					var value = factory.Function(doubleType, "CDbl", ticks);
					return factory.Div(doubleType, value, factory.Value(doubleType, divisor));
				}

				ISqlExpression Retype(ISqlExpression expression)
				{
					return factory.Expression(factory.GetDbDataType(expression).WithSystemType(resultType), "{0}", expression);
				}

				return part switch
				{
					TimeSpanPart.Days              => Retype(Divide(TimeSpan.TicksPerDay)),
					TimeSpanPart.TotalDays         => Total(TimeSpan.TicksPerDay),
					TimeSpanPart.Hours             => Retype(Component(TimeSpan.TicksPerHour, 24)),
					TimeSpanPart.TotalHours        => Total(TimeSpan.TicksPerHour),
					TimeSpanPart.Minutes           => Retype(Component(TimeSpan.TicksPerMinute, 60)),
					TimeSpanPart.TotalMinutes      => Total(TimeSpan.TicksPerMinute),
					TimeSpanPart.Seconds           => Retype(Component(TimeSpan.TicksPerSecond, 60)),
					TimeSpanPart.TotalSeconds      => Total(TimeSpan.TicksPerSecond),
					TimeSpanPart.Milliseconds      => Retype(Component(TimeSpan.TicksPerMillisecond, 1000)),
					TimeSpanPart.TotalMilliseconds => Total(TimeSpan.TicksPerMillisecond),
#if NET7_0_OR_GREATER
					TimeSpanPart.Microseconds      => Retype(Component(TimeSpan.TicksPerMicrosecond, 1000)),
					TimeSpanPart.TotalMicroseconds => Total(TimeSpan.TicksPerMicrosecond),
					TimeSpanPart.Nanoseconds       => Retype(factory.Sub(decimalType, factory.Multiply(decimalType, ticks, 100m), factory.Multiply(decimalType, Truncate(factory.Div(decimalType, factory.Multiply(decimalType, ticks, 100m), 1000m)), 1000m))),
					TimeSpanPart.TotalNanoseconds  => factory.Multiply(doubleType, factory.Function(doubleType, "CDbl", ticks), 100D),
#endif
					TimeSpanPart.Ticks             => Retype(ticks),
					_                              => null,
				};
			}

			private protected override ISqlExpression? TranslateNativeTimeSpanNegate(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression timeSpanExpression)
			{
				var factory        = translationContext.ExpressionFactory;
				var expressionType = factory.GetDbDataType(timeSpanExpression);
				return expressionType.DataType == DataType.Decimal
					? factory.Negate(expressionType, timeSpanExpression)
					: null;
			}

			private protected override ISqlExpression? TranslateNativeDateTimeIntervalAdd(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression dateTimeExpression, ISqlExpression intervalExpression, bool isSubtract, bool isDateTimeOffset)
			{
				var factory        = translationContext.ExpressionFactory;
				var expressionType = factory.GetDbDataType(intervalExpression);

				if (expressionType.DataType != DataType.Decimal)
					return null;

				var decimalType = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(18, 0);
				var ticks       = factory.Expression(decimalType, "{0}", intervalExpression);
				var days        = factory.Function(decimalType, "Fix", factory.Div(decimalType, ticks, factory.Value(decimalType, (decimal)TimeSpan.TicksPerDay)));
				var remainder   = factory.Sub(decimalType, ticks, factory.Multiply(decimalType, days, (decimal)TimeSpan.TicksPerDay));

				if (isSubtract)
				{
					days      = factory.Negate(decimalType, days);
					remainder = factory.Negate(decimalType, remainder);
				}

				var result = isDateTimeOffset
					? TranslateDateTimeOffsetDateAdd(translationContext, translationFlags, dateTimeExpression, days, Sql.DateParts.Day)
					: TranslateDateTimeDateAdd(translationContext, translationFlags, dateTimeExpression, days, Sql.DateParts.Day);

				if (result == null)
					return null;

				var seconds = factory.Function(decimalType, "Fix", factory.Div(decimalType, remainder, factory.Value(decimalType, (decimal)TimeSpan.TicksPerSecond)));
				return isDateTimeOffset
					? TranslateDateTimeOffsetDateAdd(translationContext, translationFlags, result, seconds, Sql.DateParts.Second)
					: TranslateDateTimeDateAdd(translationContext, translationFlags, result, seconds, Sql.DateParts.Second);
			}

			private protected override ISqlExpression? TranslateDateTimeIntervalDifference(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression leftExpression, ISqlExpression rightExpression, bool isDateTimeOffset)
			{
				var factory      = translationContext.ExpressionFactory;
				var intervalType = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(18, 0).WithSystemType(typeof(TimeSpan));
				return factory.Expression(intervalType, "CDec(DATEDIFF('s', {1}, {0})) * 10000000", leftExpression, rightExpression);
			}

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

			protected override ISqlExpression? TranslateDateTimeOffsetDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				return TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
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
					Sql.DateParts.Day       => "d",
					Sql.DateParts.Week      => "ww",
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

			protected override ISqlExpression? TranslateServerNow(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				return TranslateNow(translationContext, translationFlags);
			}

			protected override ISqlExpression? TranslateNow(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory       = translationContext.ExpressionFactory;
				var nowExpression = factory.NotNullExpression(factory.GetDbDataType(typeof(DateTime)), "Now");
				return nowExpression;
			}

			protected override ISqlExpression? TranslateZonedNow(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				return translationContext.ExpressionFactory.NotNullExpression(dbDataType, "Now");
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
			public override ISqlExpression? TranslateTrimStart(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression? trimChars)
			{
				if (trimChars != null)
					return null;

				return base.TranslateTrimStart(translationContext, methodCall, translationFlags, value, trimChars);
			}

			public override ISqlExpression? TranslateTrimEnd(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression? trimChars)
			{
				if (trimChars != null)
					return null;

				return base.TranslateTrimEnd(translationContext, methodCall, translationFlags, value, trimChars);
			}

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

			protected override Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool nullValuesAsEmptyString, bool isNullableResult, bool withoutSeparator)
			{
				var builder = new AggregateFunctionBuilder();

				if (withoutSeparator)
				{
					ConfigureConcat(builder, wrapByCoalesce: true);
				}
				else
				{
					ConfigureConcatWsEmulation(builder, nullValuesAsEmptyString, isNullableResult, (factory, valueType, separator, valuesExpr) =>
					{
						var intDbType = factory.GetDbDataType(typeof(int));
						var substring = factory.Function(valueType, "Mid",
							valuesExpr,
							factory.Add(intDbType, factory.Length(separator), factory.Value(intDbType, 1)));

						return substring;
					}, withoutSeparator);
				}

				return builder.Build(translationContext, methodCall, isExpression: translationFlags.HasFlag(TranslationFlags.Expression));
			}

			// {value} IS NULL OR LTRIM({value}) = ''
			// (Access LTRIM only trims spaces; full Unicode whitespace handling would require
			// sandbox-mode REPLACE chains. This matches the pre-refactor behavior.)
			public override ISqlExpression? TranslateIsNullOrWhiteSpace(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value)
			{
				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);

				var trimmed   = factory.Function(valueType, "LTRIM", value);
				var predicate = factory.Equal(trimmed, factory.Value(valueType, string.Empty));

				return WrapIsNullOrWhiteSpaceResult(translationContext, value, predicate);
			}
		}

		protected class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr,
				TranslationFlags                                                          translationFlags)
			{
				// IIf(IsNull({0}), NULL, LCase(Mid(CStr({0}), 2, 36)))
				// Access's `CStr(NULL)` throws "Invalid use of Null" at the ODBC layer
				// (does not propagate NULL like other providers), so guard explicitly.
				// Mirrors DB2 / SQLite / Oracle Guid translators.
				// Note: VBA's `IIf` function evaluates both branches eagerly
				// (https://support.microsoft.com/en-us/office/iif-function-32436ecf-c629-48a3-9900-647539c764e3),
				// but Jet/ACE SQL `IIF` short-circuits — the false branch is skipped when the
				// predicate is true (https://nolongerset.com/ternary-operator-iif/).
				// Verified empirically by `StringConcatTests.Concat_StringConcat_StringIntGuidObjectArgs_NullableArgs`
				// driving null `Guid?` rows on Access.Ace.Odbc without hitting the `CStr(NULL)` throw.

				var factory      = translationContext.ExpressionFactory;
				var stringDbType = factory.GetDbDataType(typeof(string));

				var cStrExpression   = factory.Function(stringDbType, "CStr", guidExpr);
				var midExpression    = factory.Function(stringDbType, "Mid",  cStrExpression, factory.Value(2), factory.Value(36));
				var toLower          = factory.ToLower(midExpression);
				var resultExpression = factory.Condition(factory.IsNullPredicate(guidExpr), factory.Value<string?>(stringDbType, null), factory.NotNull(toLower));

				return resultExpression;
			}
		}

		protected class AccessAggregateFunctionsMemberTranslator : AggregateFunctionsMemberTranslatorBase
		{
			protected override bool IsCountDistinctSupported       => false;
			protected override bool IsAggregationDistinctSupported => false;
		}

		protected class AccessWindowFunctionsMemberTranslator : WindowFunctionsMemberTranslator
		{
			protected override bool IsWindowFunctionsSupported => false;
		}

		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new AccessWindowFunctionsMemberTranslator();
		}
	}
}
