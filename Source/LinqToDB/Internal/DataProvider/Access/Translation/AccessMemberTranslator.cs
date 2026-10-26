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
		protected class SqlTypesTranslation : SqlTypesTranslationDefault;

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
			// Access's own Round already rounds a midpoint to even.
			protected override ISqlExpression? TranslateRoundToEven(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				if (precision is null or SqlValue { Value: 0 })
				{
					var factory = translationContext.ExpressionFactory;

					return factory.Function(factory.GetDbDataType(value), "ROUND", value);
				}

				return base.TranslateRoundToEven(translationContext, methodCall, value, precision);
			}

			protected override ISqlExpression? TranslateSqlRoundToEven(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				var factory = translationContext.ExpressionFactory;

				return factory.Function(factory.GetDbDataType(value), "Round", value, precision ?? factory.Value(0));
			}

			protected override ISqlExpression? TranslateRoundAwayFromZero(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);

				/*
					Sgn([Value]) * Int(Abs([Value]) * (10 ^ [Precision]) + 0.5) / (10 ^ [Precision])

					Int is a floor, so it is applied to the magnitude only.
				*/

				ISqlExpression? power = precision is null or SqlValue { Value: 0 }
					? null
					: factory.Binary(valueType, factory.Value(valueType, 10), "^", precision);

				var magnitude = factory.Function(valueType, "Abs", value);
				var scaled    = power == null ? magnitude : factory.Multiply(valueType, magnitude, power);
				var rounded   = factory.Function(valueType, "Int", factory.Add(valueType, scaled, factory.Value(valueType, 0.5)));
				var unscaled  = power == null ? rounded : factory.Div(valueType, rounded, power);

				return factory.Multiply(valueType, factory.Function(valueType, "Sgn", value), unscaled);
			}

			protected override ISqlExpression? TranslateTruncate(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value)
			{
				return TranslateTruncateAsFloorOrCeiling(translationContext, methodCall);
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
