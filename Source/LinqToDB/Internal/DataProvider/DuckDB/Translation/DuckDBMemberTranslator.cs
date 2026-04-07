using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DuckDB.Translation
{
	public class DuckDBMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected override IMemberTranslator CreateSqlTypesTranslator()     => new SqlTypesTranslation();
		protected override IMemberTranslator CreateDateMemberTranslator()   => new DateFunctionsTranslator();
		protected override IMemberTranslator CreateStringMemberTranslator() => new StringMemberTranslator();

		protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			return factory.NonPureFunction(factory.GetDbDataType(typeof(System.Guid)), "gen_random_uuid");
		}

		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));

			protected override Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime2));

			protected override Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime2));
		}

		protected class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateSqlCurrentTimestampUtc(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				return translationContext.ExpressionFactory.Expression(dbDataType, "current_timestamp");
			}

			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var intDataType  = factory.GetDbDataType(typeof(int));

				string? partStr;

				switch (datepart)
				{
					case Sql.DateParts.Year        : partStr = "year";      break;
					case Sql.DateParts.Quarter     : partStr = "quarter";   break;
					case Sql.DateParts.Month       : partStr = "month";     break;
					case Sql.DateParts.DayOfYear   : partStr = "dayofyear"; break;
					case Sql.DateParts.Day         : partStr = "day";       break;
					case Sql.DateParts.Week        : partStr = "week";      break;
					case Sql.DateParts.WeekDay     : partStr = "dow";       break;
					case Sql.DateParts.Hour        : partStr = "hour";      break;
					case Sql.DateParts.Minute      : partStr = "minute";    break;
					case Sql.DateParts.Second      : partStr = "second";    break;
					case Sql.DateParts.Millisecond :
					{
						// EXTRACT(millisecond FROM ...) returns total ms including seconds (e.g. 56789 for 56.789s)
						// Use modulo 1000 to get just the millisecond part
						var extractExpr = new SqlExpression(intDataType, "EXTRACT(millisecond FROM {0})", Precedence.Primary, dateTimeExpression);
						return factory.Mod(extractExpr, 1000);
					}
					default:
						return null;
				}

				var resultExpression = new SqlExpression(intDataType, $"EXTRACT({partStr} FROM {{0}})", Precedence.Primary, dateTimeExpression);

				return datepart switch
				{
					Sql.DateParts.WeekDay => factory.Increment(resultExpression),
					_                     => resultExpression,
				};
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment, Sql.DateParts datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var intervalType = factory.GetDbDataType(typeof(System.TimeSpan)).WithDataType(DataType.Interval);

				ISqlExpression ToInterval(ISqlExpression numberExpression, string intervalKind)
				{
					var intervalExpr = factory.NotNullExpression(intervalType, "Interval {0}", factory.Value(intervalKind));
					return factory.Multiply(intervalType, numberExpression, intervalExpr);
				}

				var intervalExpr = datepart switch
				{
					Sql.DateParts.Year        => ToInterval(increment, "1 Year"),
					Sql.DateParts.Quarter     => factory.Multiply(intervalType, ToInterval(increment, "1 Month"), 3),
					Sql.DateParts.Month       => ToInterval(increment, "1 Month"),
					Sql.DateParts.Week        => factory.Multiply(intervalType, ToInterval(increment, "1 Day"), 7),
					Sql.DateParts.Day         => ToInterval(increment, "1 Day"),
					Sql.DateParts.Hour        => ToInterval(increment, "1 Hour"),
					Sql.DateParts.Minute      => ToInterval(increment, "1 Minute"),
					Sql.DateParts.Second      => ToInterval(increment, "1 Second"),
					Sql.DateParts.Millisecond => ToInterval(increment, "1 Millisecond"),
					_                         => null,
				};

				if (intervalExpr == null)
					return null;

				return factory.Add(factory.GetDbDataType(dateTimeExpression), dateTimeExpression, intervalExpr);
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

				hour   = hour   == null ? factory.Value(intDataType, 0) : factory.Cast(hour, intDataType);
				minute = minute == null ? factory.Value(intDataType, 0) : factory.Cast(minute, intDataType);
				second = second == null ? factory.Value(doubleDataType, 0.0) : factory.Cast(second, doubleDataType);

				if (millisecond != null)
				{
					millisecond = factory.Cast(millisecond, doubleDataType);
					second      = factory.Add(doubleDataType, second, factory.Div(doubleDataType, millisecond, 1000));
				}

				return factory.Function(dateType, "make_timestamp", year, month, day, hour, minute, second);
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory  = translationContext.ExpressionFactory;
				var dateType = factory.GetDbDataType(typeof(System.DateTime)).WithDataType(DataType.Date);

				return factory.Cast(dateExpression, dateType);
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory  = translationContext.ExpressionFactory;
				var timeType = factory.GetDbDataType(typeof(System.TimeSpan)).WithDataType(DataType.Time);

				return factory.Cast(dateExpression, timeType);
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
						.AllowDistinct()
						.AllowNotNullCheck(true)
						.TranslateArguments(0)
						.OnBuildFunction(composer =>
						{
							var info = composer.BuildInfo;
							if (info.Value == null || info.Argument(0) == null)
								return;

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

							if (info is { FilterCondition.IsTrue: false })
								filterCondition = info.FilterCondition;

							var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

							var fn = factory.Function(valueType, "STRING_AGG",
								[new SqlFunctionArgument(value, modifier : aggregateModifier), new SqlFunctionArgument(separator, suffix : suffix)],
								[true, true],
								filter: filterCondition,
								isAggregate: true,
								canBeAffectedByOrderBy: true
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
