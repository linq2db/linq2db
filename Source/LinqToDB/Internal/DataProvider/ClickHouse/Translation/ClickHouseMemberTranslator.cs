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

#pragma warning disable IDE0042

namespace LinqToDB.Internal.DataProvider.ClickHouse.Translation
{
	public class ClickHouseMemberTranslator : ProviderMemberTranslatorDefault
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

		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal128).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal128).WithPrecisionScale(10, 4));

			protected override Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime64));

			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime64));
		}

		protected class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var intDataType  = factory.GetDbDataType(typeof(int));
				var longDataType = factory.GetDbDataType(typeof(long));

				return datepart switch
				{
					Sql.DateParts.Year        => factory.Function(intDataType, "toYear", dateTimeExpression),
					Sql.DateParts.Quarter     => factory.Function(intDataType, "toQuarter", dateTimeExpression),
					Sql.DateParts.Month       => factory.Function(intDataType, "toMonth", dateTimeExpression),
					Sql.DateParts.DayOfYear   => factory.Function(intDataType, "toDayOfYear", dateTimeExpression),
					Sql.DateParts.Day         => factory.Function(intDataType, "toDayOfMonth", dateTimeExpression),
					Sql.DateParts.Week        => factory.Function(intDataType, "toISOWeek", factory.Function(longDataType, "toDateTime64", ParametersNullabilityType.SameAsFirstParameter, dateTimeExpression, factory.Value(intDataType, 1))),
					Sql.DateParts.Hour        => factory.Function(intDataType, "toHour", dateTimeExpression),
					Sql.DateParts.Minute      => factory.Function(intDataType, "toMinute", dateTimeExpression),
					Sql.DateParts.Second      => factory.Function(intDataType, "toSecond", dateTimeExpression),
					Sql.DateParts.WeekDay     => factory.Function(intDataType, "toDayOfWeek", factory.Function(intDataType, "addDays", ParametersNullabilityType.SameAsFirstParameter, dateTimeExpression, factory.Value(intDataType, 1))),
					Sql.DateParts.Millisecond => factory.Mod(factory.Function(intDataType, "toUnixTimestamp64Milli", dateTimeExpression), 1000),
					_                         => null,
				};
			}

			protected override ISqlExpression? TranslateDateTimeOffsetDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				return TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var intDataType  = factory.GetDbDataType(typeof(int));
				var longDataType = factory.GetDbDataType(typeof(long));
				var dateType     = factory.GetDbDataType(dateTimeExpression);

				string? function;
				switch (datepart)
				{
					case Sql.DateParts.Year:    function = "addYears"; break;
					case Sql.DateParts.Quarter: function = "addQuarters"; break;
					case Sql.DateParts.Month:   function = "addMonths"; break;
					case Sql.DateParts.Day:     function = "addDays"; break;
					case Sql.DateParts.Week:    function = "addWeeks"; break;
					case Sql.DateParts.Hour:    function = "addHours"; break;
					case Sql.DateParts.Minute:  function = "addMinutes"; break;
					case Sql.DateParts.Second:  function = "addSeconds"; break;
					case Sql.DateParts.Millisecond:
					{
						var resultExpression = factory.Function(dateType, "fromUnixTimestamp64Nano",
							factory.Add(
								longDataType,
								factory.Function(longDataType, "toUnixTimestamp64Nano", dateTimeExpression),
								factory.Cast(factory.Multiply(factory.GetDbDataType(increment), increment, 1000000), longDataType)
							)
						);

						return resultExpression;
					}
					default:
						return null;
				}

				var result = factory.Function(dateType, function, dateTimeExpression, increment);
				return result;
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
				var factory     = translationContext.ExpressionFactory;
				var dateType    = resulType;
				var intDataType = factory.GetDbDataType(typeof(int));

				ISqlExpression resultExpression;

				if (millisecond == null)
				{
					resultExpression = factory.Function(dateType, "makeDateTime", year, month, day,
						hour        ?? factory.Value(intDataType, 0),
						minute      ?? factory.Value(intDataType, 0),
						second      ?? factory.Value(intDataType, 0)
					);
				}
				else
				{
					resultExpression = factory.Function(dateType, "makeDateTime64",
						year, month, day,
						hour        ?? factory.Value(intDataType, 0),
						minute      ?? factory.Value(intDataType, 0),
						second      ?? factory.Value(intDataType, 0),
						millisecond
					);

					resultExpression = factory.Cast(resultExpression, dateType.WithDataType(DataType.DateTime64));
				}

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var cast = translationContext.ExpressionFactory.Cast(dateExpression, new DbDataType(typeof(DateTime), DataType.Date32), true);
				return cast;
			}

			protected override ISqlExpression? TranslateDateTimeOffsetTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var cast = translationContext.ExpressionFactory.Cast(dateExpression, new DbDataType(typeof(DateTime), DataType.Date32), true);
				return cast;
			}

			static ISqlExpression? CommonTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression)
			{
				//toInt64((toUnixTimestamp64Nano(toDateTime64(t.DateTimeValue, 7)) - toUnixTimestamp64Nano(toDateTime64(toDate32(t.DateTimeValue), 7))) / 100)
				var factory        = translationContext.ExpressionFactory;
				var longDataType   = factory.GetDbDataType(typeof(long));
				var intDataType    = factory.GetDbDataType(typeof(int));
				var resultDataType = longDataType.WithSystemType(typeof(TimeSpan));
				var doubleDataType = factory.GetDbDataType(typeof(double));
				var dateTime64     = factory.GetDbDataType(dateExpression).WithDataType(DataType.DateTime64);
				var dateTime32     = factory.GetDbDataType(dateExpression).WithDataType(DataType.DateTime);

				var precision = factory.Value(intDataType, 7);

				var resultExpression = factory.Cast(
					factory.Div(
						doubleDataType,
						factory.Sub(
							longDataType,
							factory.Function(longDataType, "toUnixTimestamp64Nano", factory.Function(dateTime64, "toDateTime64", dateExpression, precision)),
							factory.Function(longDataType, "toUnixTimestamp64Nano", factory.Function(dateTime64, "toDateTime64", factory.Function(dateTime32, "toDate32", dateExpression), precision))
						),
						factory.Value(intDataType, 100)),
					resultDataType);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				return CommonTruncationToTime(translationContext, dateExpression);
			}

			protected override ISqlExpression? TranslateDateTimeOffsetTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				return CommonTruncationToTime(translationContext, dateExpression);
			}

			protected override ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory     = translationContext.ExpressionFactory;
				var nowFunction = factory.Function(factory.GetDbDataType(typeof(DateTime)), "now", ParametersNullabilityType.NotNullable);
				return nowFunction;
			}

			protected override ISqlExpression? TranslateSqlCurrentTimestampUtc(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.Function(dbDataType, "now", factory.Value("UTC"));
			}
		}

		protected class MathMemberTranslator : MathMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateRoundToEven(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				var factory = translationContext.ExpressionFactory;

				var valueType = factory.GetDbDataType(value);

				ISqlExpression result;

				if (precision != null)
					result = factory.Function(valueType, "roundBankers", value, precision);
				else
					result = factory.Function(valueType, "roundBankers", value);
				
				return result;
			}
		}

		protected class StringMemberTranslator : StringMemberTranslatorBase
		{
			static readonly bool[] OneArgumentNullability = new[] { true };
			static readonly bool[] TwoArgumentNullability = new[] { true, true };

			protected override Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool nullValuesAsEmptyString, bool isNullableResult)
			{
				var builder = new AggregateFunctionBuilder();

				builder
					.ConfigureAggregate(c => c
						.HasSequenceIndex(1)
						.AllowOrderBy()
						.AllowFilter()
						.AllowDistinct()
						.AllowNotNullCheck(false)
						.TranslateArguments(0)
						.OnBuildFunction(composer =>
						{
							var info = composer.BuildInfo;
							if (info.Value == null || info.Argument(0) == null)
								return;

							var f       = info.Factory;
							var sep     = info.Argument(0)!;                 // separator
							var valType = f.GetDbDataType(info.Value);
							var strType = f.GetDbDataType(typeof(string));
							var longType= f.GetDbDataType(typeof(long));

							// 1) string.Join semantics: NULL -> '' unless IsNullFiltered
							ISqlExpression value = info.Value;
							if (!info.IsNullFiltered && nullValuesAsEmptyString)
								value = f.Coalesce(value, f.Value(valType, string.Empty));

							// Ensure String for CH
							value = f.Function(strType, "toString", value);

							// 2) FILTER
							var hasCond = info is { FilterCondition.IsTrue: false };
							var cond    = hasCond ? info.FilterCondition! : null;

							// ---------------------------
							// Helpers (typed, factory-based)
							// ---------------------------

							// groupArray / groupUniqArray (+If) with proper aggregate flags
							ISqlExpression MakeGroupArray(ISqlExpression arg)
							{
								var fn = info.IsDistinct
									? (hasCond ? "groupUniqArrayIf" : "groupUniqArray")
									: (hasCond ? "groupArrayIf" : "groupArray");

								return hasCond
									? f.Function(valType, fn,
										new[] { new SqlFunctionArgument(arg), new SqlFunctionArgument(cond!) },
										TwoArgumentNullability,
										isAggregate: true,
										canBeAffectedByOrderBy: true)
									: f.Function(valType, fn,
										new[] { new SqlFunctionArgument(arg) },
										OneArgumentNullability,
										isAggregate: true,
										canBeAffectedByOrderBy: true);
							}

							var hasOrder = info.OrderBySql.Length > 0;

							// No ORDER BY: simple path
							if (!hasOrder)
							{
								var arr    = MakeGroupArray(value);
								var joined = f.Function(strType, "arrayStringConcat", arr, sep);

								var result = isNullableResult ? joined : f.Coalesce(joined, f.Value(strType, string.Empty));

								composer.SetResult(result);

								return;
							}

							// ---------------------------
							// ORDER BY (simplified rules + fallback)
							// ---------------------------

							var orderItems = info.OrderBySql; // (.expr, .desc, .nulls)

							bool IsString(int i) => f.GetDbDataType(orderItems[i].expr).SystemType == typeof(string);
							bool IsDesc(int i) => orderItems[i].desc;

							bool firstIsStringDesc       = orderItems.Length > 0 && IsString(0) && IsDesc(0);
							bool anyStringAfterFirst     = Enumerable.Range(1, Math.Max(0, orderItems.Length - 1)).Any(IsString);
							bool anyStringDescAfterFirst = Enumerable.Range(1, Math.Max(0, orderItems.Length - 1)).Any(i => IsString(i) && IsDesc(i));

							// Rules:
							//  - allow: numeric/date ASC/DESC anywhere (DESC via inversion)
							//  - allow: string ASC anywhere (bytewise)
							//  - allow: string DESC only if it is the FIRST key AND there are NO other string keys after it
							//  - otherwise => unsupported -> fallback
							bool unsupported =
								anyStringDescAfterFirst ||
								(firstIsStringDesc && anyStringAfterFirst);

							if (unsupported)
							{
								// Fallback: cannot emulate this ORDER pattern with a single arraySort key-selector.
								composer.SetFallback(fc => fc.AllowOrderBy(false));
								return;
							}

							// Build tuple (k1, k2, ..., value)
							ISqlExpression BuildTuple(IReadOnlyList<ISqlExpression> elems)
							{
								var fmt = "(" + string.Join(", ", Enumerable.Range(0, elems.Count).Select(i => "{" + i.ToString(CultureInfo.InvariantCulture) + "}")) + ")";
								return f.Fragment(fmt, elems.ToArray());
							}

							var tupleElems = new List<ISqlExpression>(orderItems.Length + 1);
							foreach (var o in orderItems)
								tupleElems.Add(o.expr);
							tupleElems.Add(value); // last is the aggregated value

							var tupleExpr = BuildTuple(tupleElems);

							// Aggregate tuples
							var tuplesArr = MakeGroupArray(tupleExpr);

							// ---- Build key selector: (t) -> (k1_nullsKey, k1_key, k2_nullsKey, k2_key, ...)
							// Nulls policy: ASC => NULLS FIRST; DESC => NULLS LAST
							ISqlExpression MakeNullsKey(ISqlExpression t_i, bool desc)
							{
								return desc
									? f.Fragment("if(isNull({0}), 1, 0)", t_i)  // DESC: nulls last
									: f.Fragment("if(isNull({0}), 0, 1)", t_i); // ASC : nulls first
							}

							// Numeric: DESC via Negate; ASC as-is
							ISqlExpression MakeKeyDescNumeric(ISqlExpression t_i)
							{
								var tType = f.GetDbDataType(t_i);
								return f.Negate(tType, t_i);
							}

							ISqlExpression MakeKeyAsc(ISqlExpression t_i) => t_i;

							// Date/DateTime: convert to timestamp (long) then Negate for DESC
							ISqlExpression MakeKeyDescDateTime(ISqlExpression t_i)
							{
								var ts = f.Function(longType, "toUnixTimestamp", t_i);
								return f.Negate(longType, ts);
							}

							// Strings: bytewise ASC; DESC only allowed for first key (we’ll reverse whole array later)
							ISqlExpression MakeKeyStringAsc(ISqlExpression t_i) => t_i;
							bool reverseWholeArray = false;
							ISqlExpression MakeKeyStringDescFirst(ISqlExpression t_i)
							{
								reverseWholeArray = true;   // will reverse after sorting
								return t_i;                 // sort ASC first
							}

							ISqlExpression TransformKey(ISqlExpression t_i, DbDataType srcType, bool desc, bool isFirstKey)
							{
								var st = srcType.SystemType;

								if (st == typeof(string))
								{
									if (!desc) return MakeKeyStringAsc(t_i);
									// only valid if first; detector above blocked other cases
									return MakeKeyStringDescFirst(t_i);
								}

								if (st == typeof(DateTime) || st == typeof(DateTimeOffset))
									return desc ? MakeKeyDescDateTime(t_i) : MakeKeyAsc(t_i);

								// numeric & other scalars
								return desc ? MakeKeyDescNumeric(t_i) : MakeKeyAsc(t_i);
							}

							// Collect transformed keys WITH leading nulls-key per ORDER item
							var keyElems = new List<ISqlExpression>(orderItems.Length * 2);
							for (int i = 0; i < orderItems.Length; i++)
							{
								var t_i   = f.Fragment("t.{0}", f.Value(i + 1)); // tuple key i
								var srcT  = f.GetDbDataType(orderItems[i].expr);
								var desc  = orderItems[i].desc;

								// 1) nulls policy key
								keyElems.Add(MakeNullsKey(t_i, desc));

								// 2) transformed key (direction encoded)
								var keyEl = TransformKey(t_i, srcT, desc, isFirstKey: i == 0);
								keyElems.Add(keyEl);
							}

							// (t) -> (k1_nullsKey, k1_key, k2_nullsKey, k2_key, ...)
							ISqlExpression BuildKeyLambda(IReadOnlyList<ISqlExpression> keys)
							{
								var keysFmt   = "(" + string.Join(", ", Enumerable.Range(0, keys.Count).Select(i => "{" + i.ToString(CultureInfo.InvariantCulture) + "}")) + ")";
								var tupleKeys = f.Fragment(keysFmt, keys.ToArray());
								return f.Fragment("(t) -> {0}", tupleKeys);
							}

							var keySelector = BuildKeyLambda(keyElems);

							// Sort: arraySort(keySelector, tuplesArr)
							ISqlExpression sortedTuples = f.Function(valType, "arraySort", keySelector, tuplesArr);

							// Reverse only if FIRST key was string DESC
							if (reverseWholeArray)
								sortedTuples = f.Function(valType, "arrayReverse", sortedTuples);

							// DISTINCT after ordering: keep first by order
							ISqlExpression valuesArr = sortedTuples;
							if (info.IsDistinct)
								valuesArr = f.Function(valType, "arrayDistinct", valuesArr);

							// Project value back: arrayMap(t -> tupleElement(t, N), valuesArr)
							var valIndex  = orderItems.Length + 1;
							var projector = f.Fragment("(t) -> tupleElement(t, {0})", f.Value(valIndex));
							var onlyVals  = f.Function(valType, "arrayMap", projector, valuesArr);

							// Join
							var finalAgg = f.Function(strType, "arrayStringConcat", onlyVals, sep);

							var finalResult = isNullableResult
								? finalAgg
								: f.Coalesce(finalAgg, f.Value(strType, string.Empty));

							composer.SetResult(finalResult);
						}));

				//TODO: For ClickHouse we cah even add filter to ignore nulls in arrayStringConcat function

				ConfigureConcatWs(builder, nullValuesAsEmptyString, isNullableResult, (factory, valueType, separator, values) =>
				{
					// arrayStringConcat([t.Value3, t.Value1, t.Value2], ' -> ')

					var arrayDataType = factory.GetDbDataType(typeof(string[]));

					var argumentsArray = values.Aggregate((v1, v2) => factory.Fragment("{0}, {1}", v1, v2));

					var param = factory.Expression(arrayDataType, Precedence.Primary, "[{0}]", false, argumentsArray);

					var function = factory.Function(valueType, "arrayStringConcat", param, separator);
					return function;
				});

				return builder.Build(translationContext, methodCall);
			}
		}

		protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "generateUUIDv4");

			return timePart;
		}

		protected class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
			{
				// lower(toString({0}))

				var factory        = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string));
				var toChar         = factory.Function(stringDataType, "toString", guidExpr);
				var toLower        = factory.ToLower(toChar);

				return toLower;
			}
		}
	}
}
