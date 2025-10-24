using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public class AggregateFunctionsMemberTranslatorBase : MemberTranslatorBase
	{
		protected virtual bool IsCountDistinctSupported       => true;
		protected virtual bool IsAggregationDistinctSupported => true;

		public AggregateFunctionsMemberTranslatorBase()
		{
			Registration.RegisterMethod((IEnumerable<int> e) => e.Count(),          TranslateCount);
			Registration.RegisterMethod((IEnumerable<int> e) => e.Count(x => true), TranslateCount);
			Registration.RegisterMethod((IQueryable<int>  e) => e.Count(),          TranslateCount);
			Registration.RegisterMethod((IQueryable<int>  e) => e.Count(x => true), TranslateCount);

			Registration.RegisterMethod((IEnumerable<int> e) => e.LongCount(),          TranslateCount);
			Registration.RegisterMethod((IEnumerable<int> e) => e.LongCount(x => true), TranslateCount);
			Registration.RegisterMethod((IQueryable<int>  e) => e.LongCount(),          TranslateCount);
			Registration.RegisterMethod((IQueryable<int>  e) => e.LongCount(x => true), TranslateCount);

			/*Registration.RegisterMethod((IEnumerable<int>    e) => e.Min(),                    TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IQueryable<int>     e) => e.Min(),                    TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IEnumerable<object> e) => e.Min<object, int>(x => 1), TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IQueryable<object>  e) => e.Min<object, int>(x => 1), TranslateMinMaxSumAverage);

			Registration.RegisterMethod((IEnumerable<int>    e) => e.Max(),                    TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IQueryable<int>     e) => e.Max(),                    TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IEnumerable<object> e) => e.Max<object, int>(x => 1), TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IQueryable<object>  e) => e.Max<object, int>(x => 1), TranslateMinMaxSumAverage);

			Registration.RegisterMethod((IEnumerable<int>    e) => e.Sum(),             TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IQueryable<int>     e) => e.Sum(),             TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IEnumerable<object> e) => e.Sum(x => (int)1),  TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IEnumerable<object> e) => e.Sum(x => (int?)1), TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IEnumerable<object> e) => e.Sum(x => (double?)1), TranslateMinMaxSumAverage);


			
			Registration.RegisterMethod((IQueryable<object>  e) => e.Sum(x => 1),       TranslateMinMaxSumAverage);

			Registration.RegisterMethod((IEnumerable<int> e) => e.Average(),          TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IQueryable<int>  e) => e.Average(),          TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IEnumerable<object> e) => e.Average(x => 1), TranslateMinMaxSumAverage);
			Registration.RegisterMethod((IQueryable<object>  e) => e.Average(x => 1), TranslateMinMaxSumAverage);*/
		}

		protected override Expression? TranslateOverrideHandler(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			if (memberExpression.NodeType == ExpressionType.Call)
			{
				var translated = TranslateMinMaxSumAverage(translationContext, (MethodCallExpression)memberExpression, translationFlags);
				if (translated != null)
					return translated;
			}

			return base.TranslateOverrideHandler(translationContext, memberExpression, translationFlags);
		}

		protected Expression? TranslateCount(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var builder = new AggregateFunctionBuilder()
					.ConfigureAggregate(c => c
						.HasSequenceIndex(0)
						.AllowFilter()
						.AllowDistinct(IsCountDistinctSupported)
						.HasFilterLambda(methodCall.Arguments.Count > 1 ? 1 : null)
						.HasValue(false)
						.OnBuildFunction(composer =>
						{
							var info = composer.BuildInfo;

							if (info.SelectQuery == null)
								return;

							if (!info.IsGroupBy)
							{
								if (methodCall.Arguments.Count > 1)
								{
									// Translate Count with predicate in non-aggregate query to Where + Count

									var lambda           = methodCall.Arguments[1].UnwrapLambda();
									var genericArguments = methodCall.Method.GetGenericArguments();
									var whereCall        = Expression.Call(typeof(Enumerable), nameof(Enumerable.Where), genericArguments, methodCall.Arguments[0], lambda);
									var countCall        = Expression.Call(typeof(Enumerable), methodCall.Method.Name,   genericArguments, whereCall);

									composer.SetFallback(c => c
										.AllowFilter(false)
										.HasFilterLambda(null)
										.FallbackExpression(countCall)
									);

									return;
								}

								if (info.FilterCondition != null)
								{
									composer.SetFallback(c => c
										.AllowFilter(false)
									);

									return;
								}
							}

							var factory   = info.Factory;
							var resultType = factory.GetDbDataType(methodCall.Method.ReturnType);

							ISqlExpression argumentValue;

							if (info.IsDistinct)
							{
								if (info.ValueExpression == null)
								{
									return;
								}

								if (!composer.Translator.TranslateExpression(info.ValueExpression, out var value, out var error))
								{
									composer.SetError(error);
									return;
								}

								var valueType = factory.GetDbDataType(value);

								if (info.FilterCondition != null && !info.FilterCondition.IsTrue())
									argumentValue = factory.Condition(info.FilterCondition, value, factory.Null(valueType));
								else
									argumentValue = value;
							}
							else
							{
								if (info.FilterCondition != null && !info.FilterCondition.IsTrue())
									argumentValue = factory.Condition(info.FilterCondition, factory.Value(resultType, 1), factory.Null(resultType));
								else
									argumentValue = factory.Fragment(resultType, "*", factory.Value(info.SelectQuery.SourceID));
							}

							var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

							var fn = factory.Function(resultType, "COUNT",
								[new SqlFunctionArgument(argumentValue, modifier : aggregateModifier)],
								[true, true],
								isAggregate : true,
								canBeAffectedByOrderBy : false
							);

							composer.SetResult(fn);
						}));

			return builder.Build(translationContext, methodCall);
		}

		protected Expression? TranslateMinMaxSumAverage(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (methodCall.Method.DeclaringType != typeof(Queryable) && methodCall.Method.DeclaringType != typeof(Enumerable))
				return null;

			var methodName = methodCall.Method.Name;
			if (methodName != nameof(Enumerable.Min) && methodName != nameof(Enumerable.Max) && methodName != nameof(Enumerable.Average) && methodName != nameof(Enumerable.Sum))
				return null;

			var builder = new AggregateFunctionBuilder()
					.ConfigureAggregate(c => c
						.HasSequenceIndex(0)
						.AllowFilter()
						.AllowDistinct(IsAggregationDistinctSupported)
						.HasValue(false)
						.OnBuildFunction(composer =>
						{
							var info = composer.BuildInfo;

							if (info.SelectQuery == null)
								return;

							var factory = info.Factory;

							ISqlExpression argumentValue;

							if (info.ValueExpression == null)
							{
								return;
							}

							if (!info.IsGroupBy)
							{
								if (info.FilterCondition != null)
								{
									composer.SetFallback(c => c
										.AllowFilter(false)
									);

									return;
								}
							}

							ISqlExpression? value;

							if (methodCall.Arguments.Count > 1)
							{
								if (!composer.AggregationContext.TranslateLambdaExpression(methodCall.Arguments[1].UnwrapLambda(), out value, out var error))
								{
									composer.SetError(error);
									return;
								}

								if (info.IsDistinct)
								{
									if (!composer.Translator.TranslateExpression(info.ValueExpression, out var checkValue, out var checkError)
									    || !value.Equals(checkValue, LinqToDB.Internal.SqlQuery.SqlExtensions.DefaultComparer))
									{
										composer.SetFallback(c => c.AllowDistinct(false));
										return;
									}
								}
							}
							else
							{
								if (!composer.Translator.TranslateExpression(info.ValueExpression, out value, out var error))
								{
									composer.SetError(error);
									return;
								}
							}

							var valueType  = factory.GetDbDataType(value);
							var resultType = factory.GetDbDataType(methodCall.Method.ReturnType);

							if (info.FilterCondition != null && !info.FilterCondition.IsTrue())
								argumentValue = factory.Condition(info.FilterCondition, value, factory.Null(valueType));
							else
								argumentValue = value;

							var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

							var functionName = methodName == nameof(Enumerable.Average) ? "AVG"
								: methodName              == nameof(Enumerable.Sum)     ? "SUM"
								: methodName              == nameof(Enumerable.Min)     ? "MIN" : "MAX";

							var fn = factory.Function(resultType, functionName,
								[new SqlFunctionArgument(argumentValue, modifier : aggregateModifier)],
								[true, true],
								isAggregate : true,
								canBeAffectedByOrderBy : false
							);

							composer.SetResult(fn);
						}));

			return builder.Build(translationContext, methodCall);
		}

	}
}
