using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;
using System.Globalization;

namespace LinqToDB.Internal.DataProvider.Ydb.Translation
{
	public class YdbMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected override IMemberTranslator CreateStringMemberTranslator() => new StringMemberTranslator();

		protected override IMemberTranslator CreateDateMemberTranslator() => new DateFunctionsTranslator();

		protected override IMemberTranslator CreateGuidMemberTranslator() => new GuidMemberTranslator();

		protected override IMemberTranslator CreateSqlTypesTranslator() => new SqlTypesTranslation();

		protected override IMemberTranslator CreateMathMemberTranslator() => new MathMemberTranslator();

		//ConvertToString
		//ProcessSqlConvert
		//TranslateConvertToBoolean/ProcessConvertToBoolean
		//ProcessGetValueOrDefault

		// TODO: we cannot use this implementation as it will generate same UUID for all invocations within single query
		//protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		//{
		//	var factory  = translationContext.ExpressionFactory;

		//	var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "RandomUuid", factory.Value(1));

		//	return timePart;
		//}

		protected class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
			{
				// Cast({0} as Utf8)

				var factory        = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string)).WithDataType(DataType.NVarChar);

				var cast  = factory.Cast(guidExpr, stringDataType);

				return cast;
			}
		}

		protected class StringMemberTranslator : StringMemberTranslatorBase
		{
			public override ISqlExpression? TranslatePadLeft(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression padding, ISqlExpression? paddingChar)
			{
				var factory = translationContext.ExpressionFactory;
				var valueTypeString = factory.GetDbDataType(value);

				return factory.Cast(
					paddingChar != null
					? factory.Function(valueTypeString, "String::LeftPad", value, padding, paddingChar)
					: factory.Function(valueTypeString, "String::LeftPad", value, padding)
					, valueTypeString, isMandatory: true);
			}

			public override ISqlExpression? TranslateReplace(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression oldValue, ISqlExpression newValue)
			{
				var factory = translationContext.ExpressionFactory;
				var valueTypeString = factory.GetDbDataType(value);

				newValue = factory.Coalesce(newValue, factory.Value(string.Empty));

				return factory.Function(valueTypeString, "Unicode::ReplaceAll", value, oldValue, newValue);
			}

			protected override Expression? TranslateLike(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;

				using var disposable = translationContext.UsingTypeFromExpression(methodCall.Arguments[0], methodCall.Arguments[1]);

				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedField))
					return translationContext.CreateErrorExpression(methodCall.Arguments[0], type: methodCall.Type);

				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out var translatedValue))
					return translationContext.CreateErrorExpression(methodCall.Arguments[1], type: methodCall.Type);

				ISqlExpression? escape = null;

				if (methodCall.Arguments.Count == 3)
				{
					if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[2], out escape))
						return translationContext.CreateErrorExpression(methodCall.Arguments[2], type: methodCall.Type);

					if (escape is SqlValue { ValueType.DataType: not DataType.Binary } value)
						value.ValueType = value.ValueType.WithDataType(DataType.Binary);
				}

				var predicate       = factory.LikePredicate(translatedField, false, translatedValue, escape);
				var searchCondition = factory.SearchCondition().Add(predicate);

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, searchCondition, methodCall);
			}

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

							if (info.FilterCondition != null && !info.FilterCondition.IsTrue())
							{
								value = factory.Condition(info.FilterCondition, value, factory.Null(valueType));

								if (!info.IsGroupBy)
								{
									composer.SetFallback(f => f.AllowFilter(false));
									return;
								}
							}

							var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

							var list = info.OrderBySql.Length == 0
							? MakeList(factory, info, valueType, value)
							: WithSort(factory, info, composer, valueType, value);

							if (list != null)
							{
								var fn     = factory.Function(factory.GetDbDataType(typeof(string)), "Unicode::JoinFromList", list, separator);
								var result = isNullableResult ? fn : factory.Coalesce(fn, factory.Value(valueType, string.Empty));

								composer.SetResult(result);
							}

							static ISqlExpression MakeList(ISqlExpressionFactory factory, AggregateFunctionBuilder.AggregateBuildInfo info, DbDataType valueType, ISqlExpression value)
							{
								var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

								return factory.Function(
									valueType,
									"AGGREGATE_LIST",
									[new SqlFunctionArgument(value, modifier : aggregateModifier)],
									[true],
									isAggregate : true,
									canBeAffectedByOrderBy : false);
							}

							static ISqlExpression? WithSort(
								ISqlExpressionFactory factory,
								AggregateFunctionBuilder.AggregateBuildInfo info,
								AggregateFunctionBuilder.AggregateComposer composer,
								DbDataType valueType,
								ISqlExpression value)
							{
								var orderItems = info.OrderBySql; // (.expr, .desc, .nulls)

								bool IsString(int i) => factory.GetDbDataType(orderItems[i].expr).SystemType == typeof(string);
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
									return null;
								}

								// Build tuple (k1, k2, ..., value)
								ISqlExpression BuildTuple(IReadOnlyList<ISqlExpression> elems)
								{
									var fmt = "(" + string.Join(", ", Enumerable.Range(0, elems.Count).Select(i => "{" + i.ToString(CultureInfo.InvariantCulture) + "}")) + ")";
									return factory.Fragment(fmt, elems.ToArray());
								}

								var tupleElems = new List<ISqlExpression>(orderItems.Length + 1);
								foreach (var (expr, _, _) in orderItems)
									tupleElems.Add(expr);
								tupleElems.Add(value); // last is the aggregated value

								var tupleExpr = BuildTuple(tupleElems);

								// Aggregate tuples
								var tuplesArr = MakeList(factory, info, valueType, tupleExpr);

								// ---- Build key selector: (t) -> (k1_nullsKey, k1_key, k2_nullsKey, k2_key, ...)
								// Nulls policy: ASC => NULLS FIRST; DESC => NULLS LAST
								ISqlExpression MakeNullsKey(ISqlExpression t_i, bool desc)
								{
									return desc
									? factory.Fragment("if({0} IS NULL, 1, 0)", t_i)  // DESC: nulls last
									: factory.Fragment("if({0} IS NULL, 0, 1)", t_i); // ASC : nulls first
								}

								// Numeric: DESC via Negate; ASC as-is
								ISqlExpression MakeKeyDescNumeric(ISqlExpression t_i)
								{
									var tType = factory.GetDbDataType(t_i);
									return factory.Negate(tType, t_i);
								}

								ISqlExpression MakeKeyAsc(ISqlExpression t_i) => t_i;

								// Date/DateTime: convert to timestamp (long) then Negate for DESC
								var longType = factory.GetDbDataType(typeof(long));
								ISqlExpression MakeKeyDescDateTime(ISqlExpression t_i)
								{
									var ts = factory.Cast(t_i, longType);
									return factory.Negate(longType, ts);
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
									var t_i   = factory.Fragment("$t.{0}", factory.Value(i)); // tuple key i
									var srcT  = factory.GetDbDataType(orderItems[i].expr);
									var desc  = orderItems[i].desc;

									// 1) nulls policy key
									keyElems.Add(MakeNullsKey(t_i, desc));

									// 2) transformed key (direction encoded)
									var keyEl = TransformKey(t_i, srcT, desc, isFirstKey: i == 0);
									keyElems.Add(keyEl);
								}

								// ($t) -> { return (k1_nullsKey, k1_key, k2_nullsKey, k2_key, ...) }
								ISqlExpression BuildKeyLambda(IReadOnlyList<ISqlExpression> keys)
								{
									var keysFmt   = "(" + string.Join(", ", Enumerable.Range(0, keys.Count).Select(i => "{" + i.ToString(CultureInfo.InvariantCulture) + "}")) + ")";
									var tupleKeys = factory.Fragment(keysFmt, keys.ToArray());
									return factory.Fragment("($t) -> {{ return {0} }}", tupleKeys);
								}

								var keySelector = BuildKeyLambda(keyElems);

								// DISTINCT: apply before sort as it will destroy sort
								if (info.IsDistinct)
									tuplesArr = factory.Function(valueType, "ListUniq", tuplesArr);

								// Sort: ListSort(tuplesArr, keySelector)
								ISqlExpression sortedTuples = factory.Function(valueType, "ListSort", tuplesArr, keySelector);

								// Reverse only if FIRST key was string DESC
								if (reverseWholeArray)
									sortedTuples = factory.Function(valueType, "ListReverse", sortedTuples);

								// Project value back: ListMap(valuesArr, ($t) -> { return $t.N })
								var valIndex  = orderItems.Length;
								var projector = factory.Fragment("($t) -> {{ return $t.{0} }}", factory.Value(valIndex));
								var onlyVals  = factory.Function(valueType, "ListMap", sortedTuples, projector);

								return onlyVals;
							}
						}));

				ConfigureConcatWs(builder, nullValuesAsEmptyString, isNullableResult, (factory, valueType, separator, values) =>
				{
					// ListConcat(AsList(t.Value3, t.Value1, t.Value2), ' -> ')

					var arrayDataType = factory.GetDbDataType(typeof(string[]));

					var param = factory.Function(arrayDataType, "AsList", values);

					param = factory.Function(arrayDataType, "ListNotNull", param);

					var function = factory.Function(valueType, "ListConcat", param, separator);
					return function;
				});

				return builder.Build(translationContext, methodCall);
			}
		}

		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertBit(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			{
				//return base.ConvertBit(translationContext, memberExpression, translationFlags);
				throw new NotSupportedException("55");
			}
#if SUPPORTS_DATEONLY

			protected override Expression? ConvertDateOnly(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			{
				//return base.ConvertDateOnly(translationContext, memberExpression, translationFlags);
				throw new NotSupportedException("52");
			}
#endif

		//	// YDB stores DateTime with microsecond precision in the Timestamp type
		//	protected override Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		//		=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

		//	protected override Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		//		=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

		//	protected override Expression? ConvertSmallDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		//		=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

		//	protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		//		=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));
		}

		protected class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeOffsetDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				//return base.TranslateDateTimeOffsetDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
				throw new NotSupportedException("11");
			}

			protected override ISqlExpression? TranslateDateTimeOffsetTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				//return base.TranslateDateTimeOffsetTruncationToDate(translationContext, dateExpression, translationFlags);
				throw new NotSupportedException("10");
			}

			protected override ISqlExpression? TranslateDateTimeOffsetTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				//return base.TranslateDateTimeOffsetTruncationToTime(translationContext, dateExpression, translationFlags);
				throw new NotSupportedException("09");
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;

				var type = factory.GetDbDataType(typeof(TimeSpan)).WithDataType(DataType.Interval);
				var cast = factory.Function(type, "DateTime::TimeOfDay", dateExpression);

				return cast;
			}

			protected override ISqlExpression? TranslateSqlCurrentTimestampUtc(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				return translationContext.ExpressionFactory.Function(dbDataType, "CurrentUtcTimestamp");
			}

			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var f       = translationContext.ExpressionFactory;
				var intType = f.GetDbDataType(typeof(int));

				// QUARTER = (Month + 2) / 3
				if (datepart == Sql.DateParts.Quarter)
				{
					var month = f.Function(intType, "DateTime::GetMonth", dateTimeExpression);
					var two  = f.Value(intType, 2);
					var three = f.Value(intType, 3);
					return f.Div(intType, f.Add(intType, month, two), three);
				}

				string? fn = datepart switch
				{
					Sql.DateParts.Year        => "DateTime::GetYear",
					Sql.DateParts.Month       => "DateTime::GetMonth",
					Sql.DateParts.Day         => "DateTime::GetDayOfMonth",
					Sql.DateParts.DayOfYear   => "DateTime::GetDayOfYear",
					Sql.DateParts.Week        => "DateTime::GetWeekOfYearIso8601",
					Sql.DateParts.Hour        => "DateTime::GetHour",
					Sql.DateParts.Minute      => "DateTime::GetMinute",
					Sql.DateParts.Second      => "DateTime::GetSecond",
					Sql.DateParts.Millisecond => "DateTime::GetMillisecondOfSecond",
					Sql.DateParts.WeekDay     => "DateTime::GetDayOfWeek",
					_                         => null,
				};

				if (fn == null)
					return null;

				var baseExpr = f.Function(intType, fn, dateTimeExpression);

				// Adjust DayOfWeek to match T-SQL format (Sunday=1 ... Saturday=7)
				if (datepart == Sql.DateParts.WeekDay)
				{
					var seven  = f.Value(intType, 7);
					var one    = f.Value(intType, 1);
					return f.Add(intType, f.Mod(baseExpr, seven), one);
				}

				return baseExpr;
			}

			//	protected override ISqlExpression? TranslateDateTimeOffsetDatePart(
			//			ITranslationContext translationContext, TranslationFlags translationFlag,
			//			ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			//		=> TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);

			protected override ISqlExpression? TranslateDateTimeDateAdd(
					ITranslationContext translationContext, TranslationFlags translationFlag,
					ISqlExpression dateTimeExpression, ISqlExpression increment, Sql.DateParts datepart)
			{
				var f        = translationContext.ExpressionFactory;
				var dateType = f.GetDbDataType(dateTimeExpression);
				var intType  = f.GetDbDataType(typeof(int));

				if (datepart is Sql.DateParts.Year or Sql.DateParts.Month or Sql.DateParts.Quarter)
				{
					string shiftFn = datepart switch
					{
						Sql.DateParts.Year    => "DateTime::ShiftYears",
						Sql.DateParts.Month   => "DateTime::ShiftMonths",
						Sql.DateParts.Quarter => "DateTime::ShiftMonths",
						_                     => throw new InvalidOperationException(),
					};

					var shiftArg = increment;

					if (datepart == Sql.DateParts.Quarter)
						shiftArg = f.Multiply(intType, increment, 3);

					var shifted      = f.Function(f.GetDbDataType(dateTimeExpression), shiftFn, dateTimeExpression, shiftArg);
					var makeDateTime = f.Function(dateType, "DateTime::MakeDatetime", shifted);

					return makeDateTime;
				}

				string? intervalFn = datepart switch
				{
					Sql.DateParts.Week        => "DateTime::IntervalFromDays",
					Sql.DateParts.Day         => "DateTime::IntervalFromDays",
					Sql.DateParts.Hour        => "DateTime::IntervalFromHours",
					Sql.DateParts.Minute      => "DateTime::IntervalFromMinutes",
					Sql.DateParts.Second      => "DateTime::IntervalFromSeconds",
					Sql.DateParts.Millisecond => "DateTime::IntervalFromMilliseconds",
					_                         => null,
				};

				if (intervalFn == null)
					return null;

				if (datepart == Sql.DateParts.Week)
				{
					increment = f.Multiply(f.GetDbDataType(increment), increment, 7);
				}

				var interval = f.Function(
						f.GetDbDataType(typeof(TimeSpan)).WithDataType(DataType.Interval),
						intervalFn,
						increment);

				return f.Add(dateType, dateTimeExpression, interval);
			}

			//	protected override ISqlExpression? TranslateDateTimeTruncationToDate(
			//			ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			//	{
			//		var f        = translationContext.ExpressionFactory;
			//		var resType  = f.GetDbDataType(typeof(DateTime)).WithDataType(DataType.Date);
			//		var startDay = f.Function(f.GetDbDataType(dateExpression), "DateTime::StartOfDay", dateExpression);
			//		return f.Function(resType, "DateTime::MakeDate", startDay);
			//	}

			//	protected override ISqlExpression? TranslateDateTimeOffsetTruncationToDate(
			//			ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			//		=> TranslateDateTimeTruncationToDate(translationContext, dateExpression, translationFlags);

			protected override ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.Function(factory.GetDbDataType(typeof(DateTime)), "CurrentUtcTimestamp");
			}
		}

		protected class MathMemberTranslator : MathMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateMaxMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var valueType = factory.GetDbDataType(xValue);

				var result = factory.Function(valueType, "MAX_OF", xValue, yValue);

				return result;
			}

			protected override ISqlExpression? TranslateMinMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var valueType = factory.GetDbDataType(xValue);

				var result = factory.Function(valueType, "MIN_OF", xValue, yValue);

				return result;
			}

			protected override ISqlExpression? TranslatePow(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var xType      = factory.GetDbDataType(xValue);
				var resultType = xType;

				if (xType.DataType is not (DataType.Double or DataType.Single))
				{
					xType = factory.GetDbDataType(typeof(double));
					xValue = factory.Cast(xValue, xType);
				}

				var yType        = factory.GetDbDataType(yValue);
				var yValueResult = yValue;

				if (yType.DataType is not (DataType.Double or DataType.Single))
				{
					yValueResult = factory.Cast(yValue, xType);
				}

				var result = factory.Function(xType, "Math::Pow", xValue, yValueResult);
				if (!resultType.EqualsDbOnly(xType))
				{
					result = factory.Cast(result, resultType);
				}

				return result;
			}

			protected override ISqlExpression? TranslateRoundToEven(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				if (precision != null)
					return base.TranslateRoundToEven(translationContext, methodCall, value, precision);

				var factory = translationContext.ExpressionFactory;

				var valueType = factory.GetDbDataType(value);

				var result = factory.Function(valueType, "Math::NearbyInt", value, factory.Fragment("Math::RoundToNearest()"));

				return result;
			}

		//	/// <summary>
		//	/// Banker's rounding: In YQL, ROUND(value, precision?) already performs to-even rounding for Decimal types.
		//	/// </summary>
		//	protected override ISqlExpression? TranslateRoundToEven(
		//		ITranslationContext translationContext,
		//		MethodCallExpression methodCall,
		//		ISqlExpression value,
		//		ISqlExpression? precision)
		//	{
		//		var factory   = translationContext.ExpressionFactory;
		//		var valueType = factory.GetDbDataType(value);

		//		return precision != null
		//			? factory.Function(valueType, "ROUND", value, precision)
		//			: factory.Function(valueType, "ROUND", value);
		//	}

		//	/// <summary>
		//	/// Away-from-zero rounding: In YQL, ROUND(value, precision?) for Numeric types already uses away-from-zero rounding.
		//	/// </summary>
		//	protected override ISqlExpression? TranslateRoundAwayFromZero(
		//		ITranslationContext translationContext,
		//		MethodCallExpression methodCall,
		//		ISqlExpression value,
		//		ISqlExpression? precision)
		//	{
		//		var factory   = translationContext.ExpressionFactory;
		//		var valueType = factory.GetDbDataType(value);

		//		return precision != null
		//			? factory.Function(valueType, "ROUND", value, precision)
		//			: factory.Function(valueType, "ROUND", value);
		//	}

		//	/// <summary>
		//	/// Exponentiation using the built-in POWER(a, b) function.
		//	/// </summary>
		//	protected override ISqlExpression? TranslatePow(
		//		ITranslationContext translationContext,
		//		MethodCallExpression methodCall,
		//		ISqlExpression xValue,
		//		ISqlExpression yValue)
		//	{
		//		var factory = translationContext.ExpressionFactory;
		//		var xType   = factory.GetDbDataType(xValue);
		//		var yType   = factory.GetDbDataType(yValue);

		//		if (!xType.EqualsDbOnly(yType))
		//			yValue = factory.Cast(yValue, xType);

		//		return factory.Function(xType, "POWER", xValue, yValue);
		//	}
		}

	}
}
