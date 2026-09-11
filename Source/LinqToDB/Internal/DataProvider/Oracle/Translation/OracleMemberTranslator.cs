using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Oracle.Translation
{
	public class OracleMemberTranslator : ProviderMemberTranslatorDefault
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
			return new OracleMathMemberTranslator();
		}

		protected override IMemberTranslator CreateGuidMemberTranslator()
		{
			return new GuidMemberTranslator();
		}

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new OracleStringMemberTranslator();
		}

		protected class SqlTypesTranslation : SqlTypesTranslationDefault
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

		protected class DateFunctionsTranslator : DateFunctionsTranslatorBase
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
					resultExpression = factory.Function(extractDbType, "EXTRACT", factory.Expression(intDataType, extractStr + " FROM {0}", dateTimeExpression));
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

			/// <summary>
			/// Oracle keeps the offset a value was written with, so the components a caller sees after a round-trip
			/// are the ones in that stored offset - but <c>EXTRACT</c> over a <c>TIMESTAMP WITH TIME ZONE</c> answers
			/// in UTC, which the documentation states outright and a probe confirms: extracting the hour of
			/// <c>12:15:32 +05:10</c> gives 7. Casting to <c>TIMESTAMP</c> first keeps the local reading.
			/// </summary>
			/// <remarks>
			/// This also settles an inconsistency that predates the fix: Year/Month/Day/Hour/Minute/Second went
			/// through <c>EXTRACT</c> while Quarter/DayOfYear/Week/Millisecond went through <c>TO_CHAR</c>, which
			/// reads the stored offset - so <c>.Day</c> and <c>.DayOfYear</c> could disagree on one row.
			/// </remarks>
			protected override ISqlExpression? ToDateTimeOffsetFrame(ITranslationContext translationContext, ISqlExpression value)
			{
				var factory = translationContext.ExpressionFactory;

				return factory.Cast(value, factory.GetDbDataType(typeof(DateTime)).WithDataType(DataType.DateTime2), true);
			}

			/// <summary>
			/// Restores the offset the operand carried, after an operation was carried out on its local reading.
			/// </summary>
			/// <remarks>
			/// Oracle performs <c>TIMESTAMP WITH TIME ZONE</c> arithmetic in UTC, so adding a month to
			/// <c>2020-02-01 00:20 +00:40</c> - which is 31 January in UTC - lands on 31 February and raises
			/// ORA-01839, where .NET answers <c>2020-03-01 00:20 +00:40</c>. Doing the shift on the local reading and
			/// re-attaching the operand's own offset is what reproduces the .NET answer. The offset has to be read
			/// back off the operand because nothing else in the expression carries it.
			/// </remarks>
			protected override ISqlExpression? FromDateTimeOffsetFrame(ITranslationContext translationContext, ISqlExpression original, ISqlExpression framed, DbDataType resultType)
			{
				var factory = translationContext.ExpressionFactory;

				var storedOffset = factory.Function(
					factory.GetDbDataType(typeof(string)),
					"To_Char",
					original,
					factory.Value("TZH:TZM"));

				return factory.Function(resultType, "From_Tz", framed, storedOffset);
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var dateType     = factory.GetDbDataType(dateTimeExpression);
				var intervalType = factory.GetDbDataType(increment).WithDataType(DataType.Interval);

				// A month-based shift cannot go through an interval here. Oracle's INTERVAL '1' MONTH raises
				// ORA-01839 whenever the shift lands on a day the target month does not have - 31 January plus a
				// month - while DateTime.AddMonths clamps to the last day of that month, which is what ADD_MONTHS
				// does. ADD_MONTHS answers a DATE, so the result is cast back to the operand's own type and the
				// sub-second remainder DATE cannot hold is added back.
				var monthsPerUnit = datepart switch
				{
					Sql.DateParts.Year    => 12,
					Sql.DateParts.Quarter => 3,
					Sql.DateParts.Month   => 1,
					_                     => 0,
				};

				if (monthsPerUnit != 0)
				{
					var shift = monthsPerUnit == 1
						? increment
						: factory.Multiply(factory.GetDbDataType(increment), increment, monthsPerUnit);

					var intType    = factory.GetDbDataType(typeof(int));
					var doubleType = factory.GetDbDataType(typeof(double));

					var addMonths = factory.Function(dateType, "Add_Months", dateTimeExpression, shift);

					var shiftedDay = TranslateDateTimeDatePart(translationContext, translationFlag, addMonths, Sql.DateParts.Day);
					var sourceDay  = TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, Sql.DateParts.Day);

					if (shiftedDay == null || sourceDay == null)
						return null;

					// ADD_MONTHS is not AddMonths. It clamps to the end of the target month in two cases where .NET
					// clamps in only one: when the target month is too short - which is the case this exists to fix -
					// and also when the *source* is the last day of its own month, where .NET keeps the day number.
					// 29 February less two months is 29 December to .NET and 31 December to ADD_MONTHS. Since the
					// function only ever moves the day forward to a month end, subtracting whatever it gained
					// restores the .NET answer and leaves the genuine clamp alone. The correction needs both the
					// shifted and the original value, so the operand is spelled more than once; accepted rather than
					// hoisted, since removing the repetition would take a CTE or a lateral join.
					var gained = factory.Function(
						intType,
						"GreatEst",
						factory.Sub(intType, shiftedDay, sourceDay),
						factory.Value(intType, 0));

					// Mandatory: ADD_MONTHS answers a DATE whatever its argument was, and the declared type alone
					// would let the cast be elided - leaving a DATE where the caller, FROM_TZ among them, expects a
					// TIMESTAMP.
					var shifted = factory.Cast(factory.Sub(dateType, addMonths, gained), dateType, isMandatory: true);

					// DATE carries whole seconds, so ADD_MONTHS drops whatever was below one. The shift itself cannot
					// change it, so it is read off the operand and put back. Built from EXTRACT rather than by
					// subtracting the truncated value: Oracle reads a difference of two datetimes in an addition as
					// adding two datetimes and raises ORA-30087. The cast makes a DATE operand answer a zero remainder
					// instead of ORA-30076, and the seconds are typed as a real number because EXTRACT answers a
					// fractional one here - declaring it an integer would let MOD be folded away to nothing.
					var seconds = factory.Function(
						doubleType,
						"EXTRACT",
						factory.Expression(doubleType, "SECOND FROM {0}", factory.Cast(dateTimeExpression, dateType.WithDataType(DataType.DateTime2), isMandatory: true)));

					var remainder = factory.Function(
						intervalType,
						"NumToDsInterval",
						factory.Mod(seconds, factory.Value(doubleType, 1)),
						factory.Value("SECOND"));

					return factory.Add(dateType, shifted, remainder);
				}

				string expStr;
				switch (datepart)
				{
					case Sql.DateParts.Day:         expStr = "INTERVAL '1' DAY"; break;
					case Sql.DateParts.Week:        expStr = "INTERVAL '7' DAY"; break;
					case Sql.DateParts.Hour:        expStr = "INTERVAL '1' HOUR"; break;
					case Sql.DateParts.Minute:      expStr = "INTERVAL '1' MINUTE"; break;
					case Sql.DateParts.Second:      expStr = "INTERVAL '1' SECOND"; break;
					case Sql.DateParts.Millisecond: expStr = "INTERVAL '0.001' SECOND"; break;
					default:
						return null;
				}

				var intervalExpression = factory.Multiply(intervalType, increment, factory.NotNullExpression(intervalType, expStr));
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

			protected override ISqlExpression? TranslateUtcNow(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var dbDataType = factory.GetDbDataType(typeof(DateTime));
				return factory.Function(dbDataType, "SYS_EXTRACT_UTC", factory.Fragment("SYSTIMESTAMP"));
			}

			protected override ISqlExpression? TranslateNow(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var dbDataType = factory.GetDbDataType(typeof(DateTime));
				return translationContext.ExpressionFactory.NotNullExpression(dbDataType, "LOCALTIMESTAMP");
			}

			protected override ISqlExpression? TranslateZonedNow(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				return translationContext.ExpressionFactory.NotNullExpression(dbDataType, "CURRENT_TIMESTAMP");
			}

			protected override ISqlExpression? TranslateZonedUtcNow(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				return translationContext.ExpressionFactory.NotNullExpression(dbDataType, "SYSTIMESTAMP AT TIME ZONE 'UTC'");
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

		protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			return factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "Sys_Guid");
		}

		// Similar to SQLite
		protected class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
			{
				// 	lower((substr(rawtohex({0}), 7, 2) || substr(rawtohex({0}), 5, 2) || substr(rawtohex({0}), 3, 2) || substr(rawtohex({0}), 1, 2) || '-' || substr(rawtohex({0}), 11, 2) || substr(rawtohex({0}), 9, 2) || '-' || substr(rawtohex({0}), 15, 2) || substr(rawtohex({0}), 13, 2) || '-' || substr(rawtohex({0}), 17, 4) || '-' || substr(rawtohex({0}), 21, 12)))

				var factory      = translationContext.ExpressionFactory;
				var stringDbType = factory.GetDbDataType(typeof(string));
				var hexExpr      = factory.Function(stringDbType, "RAWTOHEX", guidExpr);

				var dividerExpr = factory.Value(stringDbType, "-");

				var resultExpression = factory.ToLower(
					factory.Concat(
						SubString(hexExpr, 7, 2),
						SubString(hexExpr, 5, 2),
						SubString(hexExpr, 3, 2),
						SubString(hexExpr, 1, 2),
						dividerExpr,
						SubString(hexExpr, 11, 2),
						SubString(hexExpr, 9,  2),
						dividerExpr,
						SubString(hexExpr, 15, 2),
						SubString(hexExpr, 13, 2),
						dividerExpr,
						SubString(hexExpr, 17, 4),
						dividerExpr,
						SubString(hexExpr, 21, 12)
					)
				);

				resultExpression = factory.Condition(factory.IsNullPredicate(guidExpr), factory.Value<string?>(stringDbType, null), factory.NotNull(resultExpression));

				return resultExpression;

				ISqlExpression SubString(ISqlExpression expression, int pos, int length)
				{
					return factory.Function(stringDbType, "SUBSTR", expression, factory.Value(pos), factory.Value(length));
				}
			}
		}

		protected class OracleStringMemberTranslator : StringMemberTranslatorBase
		{
			protected virtual bool IsWithinGroupRequired => true;

			// Oracle treats empty string as NULL, so LTRIM/RTRIM may return NULL even when
			// both arguments are non-null (e.g. LTRIM('aaa','a') -> '' -> NULL). Mark the
			// function nullable so the optimizer keeps `IS NULL` predicates on the result.
			public override ISqlExpression? TranslateTrimStart(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression? trimChars)
			{
				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);

				return trimChars == null
					? factory.Function(valueType, "LTRIM", ParametersNullabilityType.Nullable, value)
					: factory.Function(valueType, "LTRIM", ParametersNullabilityType.Nullable, value, trimChars);
			}

			public override ISqlExpression? TranslateTrimEnd(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression? trimChars)
			{
				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);

				return trimChars == null
					? factory.Function(valueType, "RTRIM", ParametersNullabilityType.Nullable, value)
					: factory.Function(valueType, "RTRIM", ParametersNullabilityType.Nullable, value, trimChars);
			}

			protected override Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool nullValuesAsEmptyString, bool isNullableResult, bool withoutSeparator)
			{
				var builder = new AggregateFunctionBuilder()
					.ConfigureAggregate(c =>
					{
						c.TransformValue(ConvertOperandToString);

						if (withoutSeparator)
							c.HasSequenceIndex(0);
						else
							c.HasSequenceIndex(1).TranslateArguments(0);

						c.AllowOrderBy()
							.AllowFilter()
							.AllowNotNullCheck(true)
							.OnBuildFunction(composer =>
							{
								var info = composer.BuildInfo;
								if (info.Value == null || (!withoutSeparator && info.Argument(0) == null))
								{
									return;
								}

								var factory   = info.Factory;
								var valueType = factory.GetDbDataType(info.Value);
								var separator = withoutSeparator
									? factory.Value(valueType, string.Empty)
									: info.Argument(0)!;

								var value = info.Value;
								if (!info.IsNullFiltered && nullValuesAsEmptyString)
									value = factory.Coalesce(value, factory.Value(valueType, string.Empty));

								if (info is { FilterCondition.IsTrue: false })
								{
									if (!info.IsGroupBy)
									{
										composer.SetFallback(f => f.AllowFilter(false));
										return;
									}

									value = factory.Condition(info.FilterCondition, value, factory.Null(valueType));
								}

								var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

								var withinGroup = info.OrderBySql.Length > 0 ? info.OrderBySql.Select(o => new SqlWindowOrderItem(o.expr, o.desc, o.nulls)) : null;

								if (IsWithinGroupRequired && withinGroup == null)
								{
									withinGroup = [new SqlWindowOrderItem(info.Value, false, Sql.NullsPosition.None)];
								}

								// LISTAGG doesn't work with NVARCHAR values
								if (valueType.DataType is DataType.NVarChar or DataType.NChar)
								{
									// return type also will be VARCHAR
									valueType = valueType.WithDataType(valueType.DataType is DataType.NVarChar ? DataType.VarChar : DataType.Char);
									value     = factory.Cast(value, valueType);
								}

								var fn = factory.Function(valueType, "LISTAGG",
									[new SqlFunctionArgument(value, modifier : aggregateModifier), new SqlFunctionArgument(separator)],
									[true, true],
									isAggregate : true,
									withinGroup : withinGroup,
									canBeAffectedByOrderBy : true);

								SetStringJoinResult(composer, fn, isNullableResult, valueType);
							});
					});

				if (withoutSeparator)
				{
					// Oracle: empty string IS NULL (DoesProviderTreatsEmptyStringAsNull = true), so
					// `Coalesce(v, '')` is a no-op and the verbose
					// `Coalesce(Coalesce(v1,'') || Coalesce(v2,'') || ..., '')` chain that
					// ConfigureConcatWsEmulation emits collapses to `v1 || v2 || ...` semantically.
					// Use ConfigureConcat directly: emits the plain SqlConcatExpression(preserveNull:
					// true), cleaner SQL and accurate CanBeNullable (any operand nullable → result
					// nullable, which is the truth on Oracle when all operands could be null).
					// Safe on Oracle only — other providers' `Coalesce(v, '')` is a real coalesce
					// (returns the literal ''), so they need ConfigureConcatWsEmulation for
					// string.Concat's null-as-empty semantic.
					ConfigureConcat(builder);
				}
				else
				{
					// Oracle's empty-string-is-NULL identity (DoesProviderTreatsEmptyStringAsNull = true)
					// makes `Coalesce(sep || v, '')` a no-op (`'' = NULL`, and `sep || NULL = sep` on
					// Oracle, so the wrap doesn't filter NULL operands the way it does on standards-
					// compliant providers). Skipping the Coalesce wrap produces cleaner SQL and avoids
					// ORA-12704 character-set mismatches when operands mix VARCHAR2 / NVARCHAR2.
					ConfigureConcatWsEmulation(builder, nullValuesAsEmptyString, isNullableResult, (factory, valueType, separator, valuesExpr) =>
					{
						var intDbType = factory.GetDbDataType(typeof(int));
						var substring = factory.Function(valueType, "SUBSTR",
							valuesExpr,
							factory.Add(intDbType, factory.Length(separator), factory.Value(intDbType, 1)));

						return substring;
					}, withoutSeparator, wrapByCoalesce: false);
				}

				return builder.Build(translationContext, methodCall, isExpression: translationFlags.HasFlag(TranslationFlags.Expression));
			}

			// {value} IS NULL OR LTRIM({value}, 'WHITESPACES') IS NULL
			// (Oracle treats empty string as NULL, so a fully-whitespace value trims to NULL.)
			public override ISqlExpression? TranslateIsNullOrWhiteSpace(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value)
			{
				var factory     = translationContext.ExpressionFactory;
				var valueType   = factory.GetDbDataType(value);
				var literalType = factory.GetDbDataType(typeof(string));

				var trimmed   = factory.Function(valueType, "LTRIM", ParametersNullabilityType.Nullable, value, factory.Value(literalType, WHITESPACES));
				var predicate = factory.IsNull(trimmed);

				return WrapIsNullOrWhiteSpaceResult(translationContext, value, predicate);
			}
		}

		protected class OracleWindowFunctionsMemberTranslator : WindowFunctionsMemberTranslator
		{
			protected override bool IsFrameGroupsSupported          => false;
			protected override bool IsFrameExclusionSupported       => false;
			protected override bool IsKeepSupported                 => true;
			protected override bool IsLeadLagNullTreatmentSupported => true;
			protected override bool IsValueNullTreatmentSupported   => true;
			protected override bool IsNthValueFromSupported         => true;
			protected override bool IsAggregateDistinctSupported    => true;
			// Oracle supports the full statistical/regression window-function set with standard SQL names.
			protected override bool IsVarianceSupported             => true;
			protected override bool IsVarianceBareSupported         => true;
			protected override bool IsCorrelationSupported          => true;
			protected override bool IsLinearRegressionSupported     => true;
			protected override bool IsMedianSupported               => true;
			// Oracle supports both the group and windowed ordered-set forms (PERCENTILE_CONT/DISC).
			protected override bool IsOrderedSetWindowedSupported   => true;
			// Oracle supports hypothetical-set RANK/DENSE_RANK/PERCENT_RANK/CUME_DIST.
			protected override bool IsHypotheticalSetSupported      => true;

			public override Expression? TranslateRatioToReport(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
				=> TranslateRatioToReportNative(translationContext, methodCall);
		}

		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new OracleWindowFunctionsMemberTranslator();
		}
	}
}
