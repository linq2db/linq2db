using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public class AggregateFunctionsMemberTranslatorBase : MemberTranslatorBase
	{
		protected virtual bool IsCountDistinctSupported       => true;
		protected virtual bool IsAggregationDistinctSupported => true;
		protected virtual bool IsFilterSupported              => false;

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

						SqlSearchCondition? filterCondition = null;
						ISqlExpression      argumentValue;

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

							if (info is { FilterCondition.IsTrue: false })
							{
								if (IsFilterSupported)
								{
									filterCondition = info.FilterCondition;
									argumentValue   = value;
								}
								else
									argumentValue = factory.Condition(info.FilterCondition, value, factory.Null(valueType));
							}
							else
							{
								argumentValue = value;
							}
						}
						else
						{
							if (info is { FilterCondition.IsTrue: false })
							{
								if (IsFilterSupported)
								{
									filterCondition = info.FilterCondition;
									argumentValue   = factory.Fragment("*", factory.Value(info.SelectQuery.SourceID));
								}
								else
									argumentValue = factory.Condition(info.FilterCondition, factory.Value(resultType, 1), factory.Null(resultType));
							}
							else
							{
								argumentValue = factory.Fragment("*", factory.Value(info.SelectQuery.SourceID));
							}
						}

						var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

						var fn = factory.Function(resultType, "COUNT",
							[new SqlFunctionArgument(argumentValue, modifier : aggregateModifier)],
							[true, true],
							isAggregate : true,
							filter: filterCondition,
							canBeAffectedByOrderBy : false
						);

						composer.SetResult(fn);
					}));

			return builder.Build(translationContext, methodCall);
		}

		static TValue CheckNullValue<TValue>(TValue? maybeNull, string context)
			where TValue : struct
		{
			if (maybeNull is null)
			{
				throw new InvalidOperationException(
					$"Function {context} returns non-nullable value, but result is NULL. Use nullable version of the function instead."
				);
			}

			return maybeNull.Value;
		}

		Expression GenerateNullCheckIfNeeded(Expression expression, string methodName)
		{
			// in LINQ Min, Max, Avg aggregates throw exception on empty set(so Sum and Count are exceptions which return 0)

			if (expression.Type.IsNullableOrReferenceType())
				return expression;
			
			var checkExpression = expression;

			if (expression.Type.IsValueType && !expression.Type.IsNullableOrReferenceType())
			{
				checkExpression = Expression.Convert(expression, expression.Type.AsNullable());
			}

			expression = Expression.Call(
				typeof(AggregateFunctionsMemberTranslatorBase),
				nameof(CheckNullValue),
				[expression.Type],
				checkExpression,
				Expression.Constant(methodName)
			);

			return expression;
		}

		protected virtual Expression? TranslateMinMaxSumAverage(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (methodCall.Method.DeclaringType != typeof(Queryable) && methodCall.Method.DeclaringType != typeof(Enumerable))
				return null;

			var methodName = methodCall.Method.Name;
			if (methodName is not (
					nameof(Enumerable.Min)
					or nameof(Enumerable.Max)
					or nameof(Enumerable.Average)
					or nameof(Enumerable.Sum)
				))
			{
				return null;
			}

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

						SqlSearchCondition? filterCondition = null;
						ISqlExpression      argumentValue;

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
									|| !value.Equals(checkValue, SqlQuery.SqlExtensions.DefaultComparer))
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
						var hasFilter  = false;

						if (info is { FilterCondition.IsTrue: false })
						{
							hasFilter = true;
							if (IsFilterSupported)
							{
								filterCondition = info.FilterCondition;
								argumentValue   = value;
							}
							else
							{
								argumentValue = factory.Condition(info.FilterCondition, value, factory.Null(valueType));
							}
						}
						else
						{
							argumentValue = value;
						}

						var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

						var functionName = methodName switch
						{
							nameof(Enumerable.Average) => "AVG",
							nameof(Enumerable.Sum)     => "SUM",
							nameof(Enumerable.Min)     => "MIN",
							_                          => "MAX",
						};

						if (!info.IsGroupBy && argumentValue.SystemType?.IsNullableOrReferenceType() == false && functionName is "AVG" or "MIN" or "MAX")
						{
							composer.SetValidation(p => GenerateNullCheckIfNeeded(p, methodName));
						}

						var canBeNull = info is { IsGroupBy: true, IsEmptyGroupBy: false } && !hasFilter ? (bool?)null : true;

						var fn = factory.Function(resultType, functionName,
							[new SqlFunctionArgument(argumentValue, modifier : aggregateModifier)],
							[true, true],
							canBeNull: canBeNull,
							isAggregate : true,
							filter: filterCondition,
							canBeAffectedByOrderBy : false
						);

						composer.SetResult(fn);
					})
				);

			return builder.Build(translationContext, methodCall);
		}

	}
}
