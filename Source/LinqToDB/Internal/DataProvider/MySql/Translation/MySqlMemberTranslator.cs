using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.MySql.Translation
{
	public class MySqlMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertBit(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Boolean));

			protected override Expression? ConvertTinyInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Int16));

			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));

			protected override Expression? ConvertFloat(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(29, 10));

			protected override Expression? ConvertReal(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(29, 10));

			protected override Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));

			protected override Expression? ConvertSmallDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));

			protected override Expression? ConvertVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
					return null;

				return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataType.Char));
			}

			protected override Expression? ConvertDefaultChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Char));

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
		}

		protected class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory     = translationContext.ExpressionFactory;
				var intDataType = factory.GetDbDataType(typeof(int));

				string partStr;

				switch (datepart)
				{
					case Sql.DateParts.Year:    partStr = "year"; break;
					case Sql.DateParts.Quarter: partStr = "quarter"; break;
					case Sql.DateParts.Month:   partStr = "month"; break;
					case Sql.DateParts.DayOfYear:
					{
						return factory.Function(intDataType, "DayOfYear", dateTimeExpression);
					}
					case Sql.DateParts.Day:  partStr = "day"; break;
					case Sql.DateParts.Week: partStr = "week"; break;
					case Sql.DateParts.WeekDay:
					{
						var addDaysFunc = factory.Function(factory.GetDbDataType(dateTimeExpression), "Date_Add",
							ParametersNullabilityType.SameAsFirstParameter,
							dateTimeExpression,
							factory.NotNullExpression(intDataType, "interval 1 day"));

						var weekDayFunc = factory.Function(intDataType, "WeekDay", addDaysFunc);

						return factory.Increment(weekDayFunc);
					}
					case Sql.DateParts.Hour:        partStr = "hour"; break;
					case Sql.DateParts.Minute:      partStr = "minute"; break;
					case Sql.DateParts.Second:      partStr = "second"; break;
					case Sql.DateParts.Millisecond:
					{
						// (MICROSECOND(your_datetime_column) DIV 1000) 

						var microsecondFunc = factory.Div(intDataType, factory.Function(intDataType, "Microsecond", dateTimeExpression), 1000);
						return microsecondFunc;
					}
					case Sql.DateParts.Microsecond:
					{
						return factory.Function(intDataType, "Microsecond", dateTimeExpression);
					}
					default:
						throw new NotImplementedException($"TranslateDateTimePart for datepart (${datepart}) not implemented");
				}

				var extractDbType = intDataType;

				var resultExpression = factory.Function(extractDbType, "Extract", factory.Expression(intDataType, partStr + " from {0}", dateTimeExpression));

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory       = translationContext.ExpressionFactory;
				var dateType      = factory.GetDbDataType(dateTimeExpression);
				var intDbType     = factory.GetDbDataType(typeof(int));
				var intervalType  = intDbType.WithDataType(DataType.Interval);

				string expStr;
				switch (datepart)
				{
					case Sql.DateParts.Year:        expStr = "Interval {0} Year"; break;
					case Sql.DateParts.Quarter:     expStr = "Interval {0} Quarter"; break;
					case Sql.DateParts.Month:       expStr = "Interval {0} Month"; break;
					case Sql.DateParts.Day:         expStr = "Interval {0} Day"; break;
					case Sql.DateParts.Week:        expStr = "Interval {0} Week"; break;
					case Sql.DateParts.Hour:        expStr = "Interval {0} Hour"; break;
					case Sql.DateParts.Minute:      expStr = "Interval {0} Minute"; break;
					case Sql.DateParts.Second:      expStr = "Interval {0} Second"; break;
					case Sql.DateParts.Millisecond: expStr = "Interval {0} Millisecond"; break;
					default:
						throw new NotImplementedException($"TranslateDateTimeDateAdd for datepart (${datepart}) not implemented");
				}

				var resultExpression = factory.Function(dateType, "Date_Add", dateTimeExpression, factory.Expression(intervalType, expStr, increment));

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

				var yearString  = CastToLength(year, 4);
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

				resultExpression = factory.Function(resulType, "STR_TO_DATE", ParametersNullabilityType.SameAsFirstParameter, resultExpression, factory.Value(stringDataType, "%Y-%m-%d %H:%i:%s.%f"));

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;

				return factory.Function(factory.GetDbDataType(dateExpression), "Date", dateExpression);
			}

			protected override ISqlExpression? TranslateSqlCurrentTimestampUtc(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				return translationContext.ExpressionFactory.Function(dbDataType, "UTC_TIMESTAMP");
			}
		}

		protected class MySqlStringMemberTranslator : StringMemberTranslatorBase
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
									if (info.OrderBySql[i].nulls != Sql.NullsPosition.None)
									{
										sb.Value.Append(" NULLS ");
										sb.Value.Append(info.OrderBySql[i].nulls == Sql.NullsPosition.First ? "FIRST" : "LAST");
									}
								}

								suffix = factory.Fragment(sb.Value.ToString(), args);
							}

							suffix = suffix != null
								? factory.Fragment("{0} SEPARATOR {1}", suffix, separator)
								: factory.Fragment("SEPARATOR {0}",     separator);

							if (info.FilterCondition != null && !info.FilterCondition.IsTrue())
							{
								value = factory.Condition(info.FilterCondition, value, factory.Null(valueType));
							}

							var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

							var fn = factory.Function(valueType, "GROUP_CONCAT",
								[new SqlFunctionArgument(value, modifier : aggregateModifier, suffix)],
								[true, true],
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

		protected class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
			{
				// Lower(Cast({0} as CHAR(36)))

				var factory        = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string)).WithDataType(DataType.Char).WithLength(36);

				var cast    = factory.Cast(guidExpr, stringDataType);
				var toLower = factory.ToLower(cast);

				return toLower;
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

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new MySqlStringMemberTranslator();
		}

		protected override IMemberTranslator CreateGuidMemberTranslator()
		{
			return new GuidMemberTranslator();
		}

		protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "Uuid");

			return timePart;
		}
	}
}
