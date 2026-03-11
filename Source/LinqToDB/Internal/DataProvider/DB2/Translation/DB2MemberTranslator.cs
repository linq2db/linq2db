using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DB2.Translation
{
	public class DB2MemberTranslator : ProviderMemberTranslatorDefault
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
			return new DB2MathMemberTranslator();
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
			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));

			protected override Expression? ConvertDefaultNChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			{
				var dbDataType = translationContext.MappingSchema.GetDbDataType(typeof(char));

				dbDataType = dbDataType.WithDataType(DataType.Char).WithSystemType(typeof(string)).WithLength(null);

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translationContext.ExpressionFactory.Value(dbDataType, ""), memberExpression);
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

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translationContext.ExpressionFactory.Value(dbDataType, ""), memberExpression);
			}

			protected override Expression? ConvertDefaultChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			{
				var dbDataType = translationContext.MappingSchema.GetDbDataType(typeof(char));
				dbDataType = dbDataType.WithSystemType(typeof(string)).WithDataType(DataType.Char).WithLength(null);

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translationContext.ExpressionFactory.Value(dbDataType, ""), memberExpression);
			}

			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));
		}

		protected class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateMakeDateTime(
				ITranslationContext translationContext,
				DbDataType resulType,
				ISqlExpression year,
				ISqlExpression month,
				ISqlExpression day,
				ISqlExpression? hour,
				ISqlExpression? minute,
				ISqlExpression? second,
				ISqlExpression? millisecond)
			{
				var factory        = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string)).WithDataType(DataType.NVarChar);
				var intDataType    = factory.GetDbDataType(typeof(int));

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

				var yearString  = PartExpression(year, 4);
				var monthString = PartExpression(month, 2);
				var dayString   = PartExpression(day, 2);

				var resultExpression = factory.Concat(
					yearString, factory.Value(stringDataType, "-"),
					monthString, factory.Value(stringDataType, "-"), dayString);

				if (hour != null || minute != null || second != null || millisecond != null)
				{
					hour ??= factory.Value(intDataType, 0);
					minute ??= factory.Value(intDataType, 0);
					second ??= factory.Value(intDataType, 0);
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
					resultExpression = factory.Function(extractDbType, "Extract", factory.Expression(intDataType, extractStr + " from {0}", dateTimeExpression));
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
						expStr = "MONTH";
						incrementValueExpr = factory.Multiply(incrementValueType, increment, 3);
						break;
					}
					case Sql.DateParts.Month: expStr = "MONTH"; break;
					case Sql.DateParts.Day: expStr = "DAY"; break;
					case Sql.DateParts.Week:
					{
						expStr = "DAY";
						incrementValueExpr = factory.Multiply(incrementValueType, increment, 7);
						break;
					}
					case Sql.DateParts.Hour: expStr = "HOUR"; break;
					case Sql.DateParts.Minute: expStr = "MINUTE"; break;
					case Sql.DateParts.Second: expStr = "SECOND"; break;
					case Sql.DateParts.Millisecond:
					{
						expStr = "MICROSECONDS";
						incrementValueExpr = factory.Multiply(doubleDataType, increment, 1000.0);
						break;
					}
					default:
						return null;
				}

				var intervalExpression = factory.Expression(intervalType, "{0} " + expStr, incrementValueExpr);
				var resultExpression   = factory.Add(dateType, dateTimeExpression, intervalExpression);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var dateFunction = translationContext.ExpressionFactory.Function(QueryHelper.GetDbDataType(dateExpression, translationContext.MappingSchema), "DATE", dateExpression);

				return dateFunction;
			}

			protected override ISqlExpression? TranslateSqlCurrentTimestampUtc(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				// For CURRENT TIMEZONE there is no type
				return factory.Sub(dbDataType, factory.Expression(dbDataType, "CURRENT TIMESTAMP"), factory.Expression(factory.GetDbDataType(typeof(int)), "CURRENT TIMEZONE"));
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

		protected class StringMemberTranslator : StringMemberTranslatorBase
		{
			protected override Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool nullValuesAsEmptyString, bool isNullableResult)
			{
				var builder = new AggregateFunctionBuilder()
					.ConfigureAggregate(c => c
						.HasSequenceIndex(1)
						.AllowOrderBy()
						.AllowFilter()
						.AllowNotNullCheck(true)
						.TranslateArguments(0)
						.OnBuildFunction(composer =>
						{
							var info = composer.BuildInfo;
							if (info.Value == null || info.Argument(0) == null)
							{
								return;
							}

							var factory   = info.Factory;
							var separator = info.Argument(0)!;
							var valueType = factory.GetDbDataType(info.Value);

							var value = info.Value;
							if (!info.IsNullFiltered && !isNullableResult)
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

							var fn = factory.Function(valueType, "LISTAGG",
								[new SqlFunctionArgument(value, modifier : aggregateModifier), new SqlFunctionArgument(separator)],
								[true, true],
								isAggregate : true,
								withinGroup : withinGroup,
								canBeAffectedByOrderBy : true);

							var result = isNullableResult ? fn : factory.Coalesce(fn, factory.Value(valueType, string.Empty));

							composer.SetResult(result);
						}));

				ConfigureConcatWsEmulation(builder, nullValuesAsEmptyString, isNullableResult, (factory, valueType, separator, valuesExpr) =>
				{
					var intDbType = factory.GetDbDataType(typeof(int));
					var substring = factory.Function(valueType, "SUBSTRING",
						valuesExpr,
						factory.Add(intDbType, factory.Length(separator), factory.Value(intDbType, 1))
					);

					return substring;
				});

				return builder.Build(translationContext, methodCall);
			}

		}

		// Same as SQLite
		protected class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
			{
				// 	lower((substr(hex({0}), 7, 2) || substr(hex({0}), 5, 2) || substr(hex({0}), 3, 2) || substr(hex({0}), 1, 2) || '-' || substr(hex({0}), 11, 2) || substr(hex({0}), 9, 2) || '-' || substr(hex({0}), 15, 2) || substr(hex({0}), 13, 2) || '-' || substr(hex({0}), 17, 4) || '-' || substr(hex({0}), 21, 12)))

				var factory      = translationContext.ExpressionFactory;
				var stringDbType = factory.GetDbDataType(typeof(string));
				var hexExpr      = factory.Function(stringDbType, "hex", guidExpr);

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
					return factory.Function(stringDbType, "substr", expression, factory.Value(pos), factory.Value(length));
				}
			}
		}
	}
}
