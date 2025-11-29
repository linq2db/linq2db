using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

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

				return paddingChar != null
					? factory.Function(valueTypeString, "String::LeftPad", value, padding, paddingChar)
					: factory.Function(valueTypeString, "String::LeftPad", value, padding);
			}

			public override ISqlExpression? TranslateReplace(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression oldValue, ISqlExpression newValue)
			{
				var factory = translationContext.ExpressionFactory;
				var valueTypeString = factory.GetDbDataType(value);

				return factory.Function(valueTypeString, "String::ReplaceAll", value, oldValue, newValue);
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

							var withinGroup = info.OrderBySql.Length > 0 ? info.OrderBySql.Select(o => new SqlWindowOrderItem(o.expr, o.desc, o.nulls)) : null;

							var fn = factory.Function(valueType, "AGGREGATE_LIST",
								[new SqlFunctionArgument(value, modifier : aggregateModifier)],
								[true],
								isAggregate : true,
								withinGroup : withinGroup,
								canBeAffectedByOrderBy : false);

							fn = factory.Function(factory.GetDbDataType(typeof(string)), "Unicode::JoinFromList", fn, separator);

							var result = isNullableResult ? fn : factory.Coalesce(fn, factory.Value(valueType, string.Empty));

							composer.SetResult(result);
						}));

				ConfigureConcat(builder, nullValuesAsEmptyString, isNullableResult);

				return builder.Build(translationContext, methodCall);
			}

			protected void ConfigureConcat(AggregateFunctionBuilder builder, bool nullValuesAsEmptyString, bool isNullableResult, Func<ISqlExpressionFactory, DbDataType, ISqlExpression, ISqlExpression[], ISqlExpression>? functionFactory = null)
			{
				builder
					.ConfigurePlain(c => c
						.HasSequenceIndex(1)
						.TranslateArguments(0)
						.AllowFilter()
						.AllowNotNullCheck(true)
						.OnBuildFunction(composer =>
						{
							var info = composer.BuildInfo;
							if (info.Values.Length == 0 || info.Argument(0) == null)
							{
								composer.SetResult(info.Factory.Value(info.Factory.GetDbDataType(typeof(string)), string.Empty));
								return;
							}

							var factory   = info.Factory;
							var separator = info.Argument(0)!;
							var dataType  = factory.GetDbDataType(info.Values[0]);

							if (info.Values is [var singleValue])
							{
								singleValue = isNullableResult && !nullValuesAsEmptyString ? singleValue : factory.Coalesce(singleValue, factory.Value(dataType, string.Empty));
								composer.SetResult(singleValue);
								return;
							}

							if (!composer.GetFilteredToNullValues(out ICollection<ISqlExpression>? values, out var error))
							{
								composer.SetError(error);
								return;
							}

							var items = !info.IsNullFiltered && nullValuesAsEmptyString
							? values.Select(i => factory.Coalesce(i, factory.Value(factory.GetDbDataType(i), ""))).ToArray()
							: values;

							ISqlExpression result;

							if (functionFactory != null)
							{
								result = functionFactory(factory, dataType, separator, items.ToArray());
							}
							else
							{
								result = factory.Function(dataType, "ListConcat",
									parametersNullability: ParametersNullabilityType.SameAsFirstParameter,
									[.. items, separator]);
							}

							if (isNullableResult)
							{
								var condition = factory.SearchCondition();
								condition.AddRange(values.Select(v => factory.IsNull(v)));

								result = factory.Condition(condition, factory.Null(dataType), result);
							}

							composer.SetResult(result);

						}));
			}
		}

		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertBit(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			{
				//return base.ConvertBit(translationContext, memberExpression, translationFlags);
				throw new NotImplementedException("55");
			}
#if SUPPORTS_DATEONLY

			protected override Expression? ConvertDateOnly(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			{
				//return base.ConvertDateOnly(translationContext, memberExpression, translationFlags);
				throw new NotImplementedException("52");
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
				throw new NotImplementedException("11");
			}

			protected override ISqlExpression? TranslateDateTimeOffsetTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				//return base.TranslateDateTimeOffsetTruncationToDate(translationContext, dateExpression, translationFlags);
				throw new NotImplementedException("10");
			}

			protected override ISqlExpression? TranslateDateTimeOffsetTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				//return base.TranslateDateTimeOffsetTruncationToTime(translationContext, dateExpression, translationFlags);
				throw new NotImplementedException("09");
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
					_                         => null
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
						_                     => throw new InvalidOperationException()
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
					_                         => null
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
