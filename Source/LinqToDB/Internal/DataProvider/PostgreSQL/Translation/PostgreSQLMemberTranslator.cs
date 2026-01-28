using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.PostgreSQL.Translation
{
	public class PostgreSQLMemberTranslator : ProviderMemberTranslatorDefault
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
			return new MathMemberTranslator();
		}

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new StringMemberTranslator();
		}

		protected override IMemberTranslator CreateGuidMemberTranslator()
		{
			return new GuidMemberTranslator();
		}

		protected override IMemberTranslator CreateAggregateFunctionsMemberTranslator()
		{
			return new PostgreSQLAggregateFunctionsMemberTranslator();
		}

		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertTinyInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Int16));

			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));

			protected override Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertSmallDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertNVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
					return null;

				return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataType.VarChar));
			}

			protected override Expression? ConvertDefaultNVarChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.VarChar));
		}

		protected class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var intDbType    = factory.GetDbDataType(typeof(int));
				var doubleDbType = factory.GetDbDataType(typeof(double));

				string? partStr;

				switch (datepart)
				{
					case Sql.DateParts.Year:        partStr = "year";        break;
					case Sql.DateParts.Quarter:     partStr = "quarter";     break;
					case Sql.DateParts.Month:       partStr = "month";       break;
					case Sql.DateParts.DayOfYear:   partStr = "doy";         break;
					case Sql.DateParts.Day:         partStr = "day";         break;
					case Sql.DateParts.Week:        partStr = "week";        break;
					case Sql.DateParts.WeekDay:     partStr = "dow";         break;
					case Sql.DateParts.Hour:        partStr = "hour";        break;
					case Sql.DateParts.Minute:      partStr = "minute";      break;
					case Sql.DateParts.Second:      partStr = "second";      break;
					case Sql.DateParts.Millisecond:
					{
						// Cast(To_Char({date}, 'MS') as int

						var toCharExpression = factory.Function(factory.GetDbDataType(typeof(string)), "To_Char",ParametersNullabilityType.SameAsFirstParameter, dateTimeExpression, factory.Value("MS"));
						var castExpression   = factory.Cast(toCharExpression, intDbType);

						return castExpression;
					}
					default:
						return null;
				}

				ISqlExpression resultExpression;

				var extractDbType = doubleDbType;
				/*if (datepart == Sql.DateParts.Hour)
				{
					resultExpression = factory.Function(intDbType, "Extract", factory.Fragment(intDbType, $"{partStr} From {{0}}", dateTimeExpression));
				}
				else*/
				{
					resultExpression = factory.Cast(
						factory.Function(extractDbType, "Extract", factory.Expression(doubleDbType, $"{partStr} From {{0}}", dateTimeExpression)),
						intDbType);
				}

				if (datepart == Sql.DateParts.WeekDay)
				{
					resultExpression = factory.Increment(resultExpression);
				}

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeOffsetDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				return TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				// date_trunc('day', dateExpression)

				var factory = translationContext.ExpressionFactory;

				var dateTruncExpression = factory.Function(factory.GetDbDataType(dateExpression), "Date_Trunc", ParametersNullabilityType.SameAsSecondParameter, factory.Value("day"), dateExpression);

				return dateTruncExpression;
			}

			protected override ISqlExpression? TranslateDateTimeOffsetTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				// date_trunc('day', dateExpression AT TIME ZONE 'UTC')::date

				var factory = translationContext.ExpressionFactory;

				var atTimeZone = factory.Expression(factory.GetDbDataType(dateExpression), "{0} AT TIME ZONE {1}", dateExpression, factory.Value("UTC"));

				var dateTruncExpression = factory.Function(factory.GetDbDataType(dateExpression), "Date_Trunc", ParametersNullabilityType.SameAsSecondParameter, factory.Value("day"), atTimeZone);

				dateTruncExpression = factory.Cast(dateTruncExpression, factory.GetDbDataType(typeof(DateTime)).WithDataType(DataType.Date));

				return dateTruncExpression;
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment, Sql.DateParts datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var intervalType = factory.GetDbDataType(typeof(TimeSpan)).WithDataType(DataType.Interval);

				ISqlExpression ToInterval(ISqlExpression numberExpression, string intervalKind)
				{
					var intervalExpr = factory.NotNullExpression(intervalType, "Interval {0}", factory.Value(intervalKind));

					return factory.Multiply(intervalType, numberExpression, intervalExpr);
				}

				ISqlExpression intervalExpr;
				switch (datepart)
				{
					case Sql.DateParts.Year:    intervalExpr = ToInterval(increment, "1 Year"); break;
					case Sql.DateParts.Quarter: intervalExpr = factory.Multiply(intervalType, ToInterval(increment, "1 Month"), 3); break;
					case Sql.DateParts.Month:   intervalExpr = ToInterval(increment, "1 Month"); break;
					case Sql.DateParts.Week:        intervalExpr = factory.Multiply(intervalType, ToInterval(increment, "1 Day"), 7); break;
					case Sql.DateParts.Hour:        intervalExpr = ToInterval(increment, "1 Hour"); break;
					case Sql.DateParts.Minute:      intervalExpr = ToInterval(increment, "1 Minute"); break;
					case Sql.DateParts.Second:      intervalExpr = ToInterval(increment, "1 Second"); break;
					case Sql.DateParts.Millisecond: intervalExpr = ToInterval(increment, "1 Millisecond"); break;
					case Sql.DateParts.Day: intervalExpr = ToInterval(increment, "1 Day"); break;
					default:
						return null;
				}

				var resultExpression = factory.Add(factory.GetDbDataType(dateTimeExpression), dateTimeExpression, intervalExpr);
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
				var dateType       = resulType;
				var intDataType    = factory.GetDbDataType(typeof(int));
				var doubleDataType = factory.GetDbDataType(typeof(double));

				ISqlExpression resultExpression;

				hour   = hour   == null ? factory.Value(intDataType, 0) : factory.Cast(hour, intDataType);
				minute = minute == null ? factory.Value(intDataType, 0) : factory.Cast(minute, intDataType);
				second = second == null ? factory.Value(doubleDataType, 0.0) : factory.Cast(second, doubleDataType);

				if (millisecond != null)
				{
					millisecond = factory.Cast(millisecond, doubleDataType);
					second      = factory.Add(doubleDataType, second, factory.Div(doubleDataType, millisecond, 1000));
				}

				resultExpression = factory.Function(dateType, "make_timestamp",
					year, month, day,
					hour,
					minute,
					second
				);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateSqlCurrentTimestampUtc(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.Function(dbDataType, "timezone", factory.Value("UTC"), factory.Function(dbDataType, "now"));
			}
		}

		protected class MathMemberTranslator : MathMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateRoundToEven(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				var factory     = translationContext.ExpressionFactory;
				var decimalType = factory.GetDbDataType(typeof(decimal));

				var valueType   = factory.GetDbDataType(value);
				var shouldCast  = decimalType != valueType;

				var valueCasted = value;
				if (shouldCast)
				{
					valueCasted = factory.Cast(value, decimalType);
				}

				var result = base.TranslateRoundToEven(translationContext, methodCall, valueCasted, precision);

				if (result != null && shouldCast)
				{
					result = factory.Cast(result, valueType);
				}

				return result;
			}

			protected override ISqlExpression? TranslateRoundAwayFromZero(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);

				ISqlExpression result;

				if (precision == null)
				{
					/*
					CASE
						WHEN value > 0 THEN floor(value + 0.5)
						   ELSE ceil(value - 0.5)
					   END
					 */
					result = factory.Condition(factory.Greater(value, factory.Value(valueType, 0)),
						factory.Function(valueType, "FLOOR", factory.Add(valueType, value, factory.Value(valueType, 0.5))),
						factory.Function(valueType, "CEIL",  factory.Sub(valueType, value, factory.Value(valueType, 0.5)))
					);
				}
				else
				{
					/*
					CASE
						   WHEN value >= 0 THEN floor(value * power(10, precision) + 0.5) / power(10, precision)
						   ELSE ceil(value * power(10, precision) - 0.5) / power(10, precision)
					   END
					 */
					var powerExpr = factory.Function(valueType, "POWER",
						factory.Value(valueType, 10),
						precision);

					result = factory.Condition(factory.GreaterOrEqual(value, factory.Value(valueType, 0)),
						factory.Div(
							valueType,
							factory.Function(valueType, "FLOOR",
								factory.Add(
									valueType,
									factory.Multiply(valueType, value, powerExpr),
									factory.Value(valueType, 0.5))),
							powerExpr),
						factory.Div(
							valueType,
							factory.Function(valueType, "CEIL",
								factory.Sub(
									valueType,
									factory.Multiply(valueType, value, powerExpr),
									factory.Value(valueType, 0.5))),
							powerExpr)
					);
				}

				return result;
			}
		}

		protected class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
			{
				// Cast({0} as VarChar(36))

				var factory        = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string)).WithDataType(DataType.VarChar).WithLength(36);

				var cast  = factory.Cast(guidExpr, stringDataType);
				
				return cast;
			}
		}

		protected class PostgreSQLAggregateFunctionsMemberTranslator : AggregateFunctionsMemberTranslatorBase
		{
			protected override bool IsFilterSupported => true;
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
						.AllowDistinct()
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
							if (!info.IsNullFiltered && nullValuesAsEmptyString)
								value = factory.Coalesce(value, factory.Value(valueType, string.Empty));

							if (info is { IsDistinct: true, OrderBySql.Length: > 0 })
							{
								if (info.OrderBySql.Any(o => o.expr != value))
								{
									composer.SetFallback(fc => fc
										.AllowDistinct(false)
										.AllowNotNullCheck(null)
									);
									return;
								}
							}

							ISqlExpression? suffix = null;
							if (info.OrderBySql.Length > 0)
							{
								using var sb = Pools.StringBuilder.Allocate();

								var args = info.OrderBySql.Select(o => o.expr).ToArray();

								sb.Value.Append("ORDER BY ");
								for (int i = 0; i < info.OrderBySql.Length; i++)
								{
									if (i > 0) sb.Value.Append(", ");
									sb.Value.Append('{').Append(i).Append('}');
									if (info.OrderBySql[i].desc) sb.Value.Append(" DESC");

									if (!info.IsNullFiltered)
									{
										sb.Value.Append(" NULLS ");
										sb.Value.Append(info.OrderBySql[i].nulls is Sql.NullsPosition.First or Sql.NullsPosition.None ? "FIRST" : "LAST");
									}
								}

								suffix = factory.Fragment(sb.Value.ToString(), args);
							}

							SqlSearchCondition? filterCondition = null;

							if (info.FilterCondition != null && !info.FilterCondition.IsTrue())
							{
								filterCondition = info.FilterCondition;
							}

							var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

							var fn = factory.Function(valueType, "STRING_AGG",
								[new SqlFunctionArgument(value, modifier : aggregateModifier), new SqlFunctionArgument(separator, suffix : suffix)],
								[true, true],
								filter: filterCondition,
								isAggregate : true,
								canBeAffectedByOrderBy : true
							);

							var result = isNullableResult ? fn : factory.Coalesce(fn, factory.Value(valueType, string.Empty));

							composer.SetResult(result);
						}));

				ConfigureConcatWs(builder, nullValuesAsEmptyString, isNullableResult);

				return builder.Build(translationContext, methodCall);
			}
		}
	}
}
