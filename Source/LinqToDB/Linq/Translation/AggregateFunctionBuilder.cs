using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Linq.Translation
{
	/// <summary>
	///     Fluent builder to configure aggregate/"plain" aggregation translation using existing BuildAggregationFunction
	///     pipeline.
	/// </summary>
	public sealed class AggregateFunctionBuilder
	{
		readonly ModeConfig _aggregate = new();
		readonly ModeConfig _plain     = new();

		public AggregateFunctionBuilder ConfigureAggregate(Action<AggregateModeBuilder> configAction)
		{
			configAction(new AggregateModeBuilder(_aggregate));
			return this;
		}

		public AggregateFunctionBuilder ConfigurePlain(Action<AggregateModeBuilder> configAction)
		{
			configAction(new AggregateModeBuilder(_plain));
			return this;
		}

		public Expression? Build(ITranslationContext ctx, MethodCallExpression functionCall)
		{
			if (_plain.BuildAction != null)
			{
				if (_plain.SequenceIndex == null)
				{
					throw new InvalidOperationException("Sequence index must be specified for plain mode when aggregate mode is configured.");
				}

				var arrayResult = ctx.BuildArrayAggregationFunction(
					_plain.SequenceIndex.Value,
					functionCall,
					_plain.ToAllowedOps(),
					agg => Combine(ctx, agg, functionCall, _plain, _plain.SequenceIndex.Value, true));

				if (arrayResult is SqlPlaceholderExpression)
				{
					return arrayResult;
				}
			}

			if (_aggregate.BuildAction != null)
			{
				if (_aggregate.SequenceIndex == null)
				{
					throw new InvalidOperationException("Sequence index must be specified for aggregate mode.");
				}

				return ctx.BuildAggregationFunction(
					_aggregate.SequenceIndex.Value,
					functionCall,
					_aggregate.ToAllowedOps(),
					agg => Combine(ctx, agg, functionCall, _aggregate, _aggregate.SequenceIndex.Value, false));
			}

			return null;
		}

		private BuildAggregationFunctionResult Combine(
			ITranslationContext  ctx,
			IAggregationContext  raw,
			MethodCallExpression functionCall,
			ModeConfig           config,
			int                  sequenceExpressionIndex,
			bool                 plainMode)
		{
			var factory = ctx.ExpressionFactory;

			var translatedArgs = new ISqlExpression?[config.ArgumentIndexes.Length];
			for (var i = 0; i < config.ArgumentIndexes.Length; i++)
			{
				var argIndex = config.ArgumentIndexes[i];
				if (argIndex < 0 || argIndex >= functionCall.Arguments.Count)
				{
					return BuildAggregationFunctionResult.Error(ctx.CreateErrorExpression(functionCall, $"Argument index {argIndex.ToString(CultureInfo.InvariantCulture)} out of range", functionCall.Type));
				}

				var argExpr = functionCall.Arguments[argIndex];
				if (!raw.TranslateExpression(argExpr, out var argSql, out var argErr))
				{
					return BuildAggregationFunctionResult.Error(argErr);
				}

				translatedArgs[i] = argSql!;
			}

			ISqlExpression? valueSql = null;
			if (raw.ValueExpression != null && config.HasValue)
			{
				if (!raw.TranslateExpression(raw.ValueExpression, out valueSql, out var vErr))
				{
					return BuildAggregationFunctionResult.Error(vErr);
				}
			}

			var itemsSql = Array.Empty<ISqlExpression>();
			if (plainMode && raw.Items != null)
			{
				itemsSql = new ISqlExpression[raw.Items.Length];
				for (var i = 0; i < raw.Items.Length; i++)
				{
					var itemExpr = raw.Items[i];
					if (!raw.TranslateExpression(itemExpr, out var itemSql, out var itemErr))
					{
						return BuildAggregationFunctionResult.Error(itemErr);
					}

					itemsSql[i] = itemSql!;
				}
			}

			var orderSql = new (ISqlExpression expr, bool desc, Sql.NullsPosition nulls)[raw.OrderBy.Length];
			for (var i = 0; i < raw.OrderBy.Length; i++)
			{
				var obInfo = raw.OrderBy[i];
				if (!raw.TranslateExpression(obInfo.Expr, out var obSql, out var obErr))
				{
					return BuildAggregationFunctionResult.Error(obErr);
				}

				orderSql[i] = (obSql!, obInfo.IsDescending, obInfo.Nulls);
			}

			SqlSearchCondition? filterSql         = null;
			List<Expression>?   filterExpressions = null;
			var                 isNullFiltered    = false;

			if (raw.FilterExpressions != null)
			{
				if (raw is { Items: not null, ValueParameter: not null })
				{
					filterExpressions = new List<Expression>(raw.FilterExpressions.Length);
					filterExpressions.AddRange(raw.FilterExpressions);
					var alreadySpottedNullCheck = false;

					if (config.AllowNotNullCheckMode != null)
					{
						for (int i = 0; i < filterExpressions.Count; i++)
						{
							var expr = filterExpressions[i];
							if (expr.NodeType == ExpressionType.Equal)
								continue;

							var binary = (BinaryExpression)expr;

							if (binary.Left == raw.ValueParameter && binary.Right.IsNullValue() || binary.Right == raw.ValueParameter && binary.Left.IsNullValue())
							{
								if (alreadySpottedNullCheck || config.AllowNotNullCheckMode == true)
								{
									filterExpressions.RemoveAt(i);
									isNullFiltered = true;
									i++;
								}

								alreadySpottedNullCheck = true;
							}
						}

						if (filterExpressions.Count == 0)
						{
							filterExpressions = null;
						}
					}
				}
				else
				{
					var filterCondition = raw.FilterExpressions.Aggregate(Expression.AndAlso);

					if (!raw.TranslateExpression(filterCondition, out var filterExprSql, out var fErr))
					{
						return BuildAggregationFunctionResult.Error(fErr);
					}

					filterSql = filterExprSql as SqlSearchCondition ?? new SqlSearchCondition().Add(new SqlPredicate.Expr(filterExprSql!));
				}
			}

			if (config.FilterLambdaIndex != null)
			{
				var lambdaIndex  = config.FilterLambdaIndex.Value;
				var filterLambda = functionCall.Arguments[lambdaIndex].UnwrapLambda();

				if (!raw.TranslateLambdaExpression(filterLambda, out var filterExprSql, out var error))
				{
					return BuildAggregationFunctionResult.Error(error);
				}

				if (filterSql == null)
				{
					filterSql = new SqlSearchCondition();
				}

				if (filterExprSql is not ISqlPredicate predicate)
				{
					predicate = new SqlPredicate.Expr(filterExprSql);
				}

				filterSql.Predicates.Add(predicate);
			}

			if (filterSql != null && valueSql != null && config.AllowNotNullCheckMode.HasValue && filterSql is { IsAnd: true })
			{
				var isNotNull = filterSql.Predicates.FirstOrDefault(p => p is SqlPredicate.IsNull { IsNot: true } isNull && isNull.Expr1.Equals(valueSql));
				if (isNotNull != null)
				{
					isNullFiltered = true;
					if (config.AllowNotNullCheckMode.Value)
					{
						filterSql.Predicates.Remove(isNotNull);
						if (filterSql.Predicates.Count == 0)
						{
							filterSql = null;
						}
					}
				}
			}

			var info = new AggregateBuildInfo(
				factory,
				valueSql,
				raw.ValueExpression,
				itemsSql,
				filterSql,
				raw.SelectQuery,
				filterExpressions,
				raw.ValueParameter,
				orderSql,
				translatedArgs,
				raw.IsGroupBy,
				raw.IsDistinct,
				isNullFiltered,
				plainMode
			);

			var composer = new AggregateComposer(factory, info, raw);
			config.BuildAction?.Invoke(composer);

			if (composer.Error != null)
			{
				return BuildAggregationFunctionResult.Error(composer.Error);
			}

			if (composer.Fallback != null)
			{
				var fallbackConfig = config.Clone();
				fallbackConfig.FallbackExpression = null;

				var fallbackBuilder = new AggregateFallbackModeBuilder(fallbackConfig);
				composer.Fallback(fallbackBuilder);

				var newCall = fallbackConfig.FallbackExpression ?? functionCall;

				var fallbackResult = ctx.BuildAggregationFunction(
					sequenceExpressionIndex,
					newCall,
					fallbackConfig.ToAllowedOps(),
					agg => Combine(ctx, agg, functionCall, fallbackConfig, sequenceExpressionIndex, true));

				if (fallbackResult is SqlPlaceholderExpression placeholder)
				{
					return BuildAggregationFunctionResult.FromSqlExpression(placeholder.Sql);
				}

				if (fallbackResult is SqlValidateExpression sqlValidateExpression && sqlValidateExpression.InnerExpression is SqlPlaceholderExpression validatePlaceholder)
				{
					return BuildAggregationFunctionResult.FromSqlExpression(validatePlaceholder.Sql, sqlValidateExpression.Validator);
				}

				if (fallbackResult is SqlErrorExpression errorExpression)
				{
					return BuildAggregationFunctionResult.Error(errorExpression);
				}
			
				return BuildAggregationFunctionResult.FromFallback(fallbackResult);
			}

			if (composer.Result == null)
			{
				return BuildAggregationFunctionResult.Error(ctx.CreateErrorExpression(functionCall, "Aggregate builder produced no result", functionCall.Type));
			}

			return BuildAggregationFunctionResult.FromSqlExpression(composer.Result, composer.Validator);
		}

		public sealed class ModeConfig
		{
			public bool        AllowDistinctFlag     { get; set; }
			public bool        AllowFilterFlag       { get; set; }
			public bool?       AllowNotNullCheckMode { get; set; }
			public bool        AllowOrderByFlag      { get; set; }
			public int[]       ArgumentIndexes       { get; set; } = Array.Empty<int>();
			public bool        HasValue              { get; set; } = true;
			public int?        SequenceIndex         { get; set; }
			public int?        FilterLambdaIndex     { get; set; }
			public Expression? FallbackExpression    { get; set; }

			public Action<AggregateComposer>? BuildAction  { get; set; }

			internal ITranslationContext.AllowedAggregationOperators ToAllowedOps()
			{
				var ops = ITranslationContext.AllowedAggregationOperators.None;
				if (AllowOrderByFlag)
				{
					ops |= ITranslationContext.AllowedAggregationOperators.OrderBy;
				}

				if (AllowFilterFlag)
				{
					ops |= ITranslationContext.AllowedAggregationOperators.Filter;
				}

				if (AllowDistinctFlag)
				{
					ops |= ITranslationContext.AllowedAggregationOperators.Distinct;
				}

				return ops;
			}

			/// <summary>
			/// Creates a shallow copy of current configuration.
			/// Arrays are cloned, delegates are referenced.
			/// </summary>
			public ModeConfig Clone()
			{
				return new ModeConfig
				{
					AllowDistinctFlag     = AllowDistinctFlag,
					AllowFilterFlag       = AllowFilterFlag,
					AllowNotNullCheckMode = AllowNotNullCheckMode,
					AllowOrderByFlag      = AllowOrderByFlag,
					ArgumentIndexes       = ArgumentIndexes.Length == 0 ? Array.Empty<int>() : (int[])ArgumentIndexes.Clone(),
					BuildAction           = BuildAction,
					HasValue              = HasValue,
					FilterLambdaIndex     = FilterLambdaIndex,
					SequenceIndex         = SequenceIndex,
					FallbackExpression    = FallbackExpression
				};
			}
		}

		public class AggregateModeBuilderBase<TFinalBuilder>
		where TFinalBuilder : AggregateModeBuilderBase<TFinalBuilder>
		{
			protected readonly ModeConfig Config;
			internal AggregateModeBuilderBase(ModeConfig config) => Config = config;

			public TFinalBuilder AllowOrderBy(bool allow = true)
			{
				Config.AllowOrderByFlag = allow;
				return (TFinalBuilder)this;
			}

			public TFinalBuilder AllowFilter(bool allow = true)
			{
				Config.AllowFilterFlag = allow;
				return (TFinalBuilder)this;
			}

			public TFinalBuilder AllowDistinct(bool allow = true)
			{
				Config.AllowDistinctFlag = allow;
				return (TFinalBuilder)this;
			}

			public TFinalBuilder AllowNotNullCheck(bool? removeFromFilter)
			{
				Config.AllowNotNullCheckMode = removeFromFilter;
				return (TFinalBuilder)this;
			}

			public TFinalBuilder HasValue(bool hasValue = true)
			{
				Config.HasValue = hasValue;
				return (TFinalBuilder)this;
			}

			public TFinalBuilder HasFilterLambda(int? argumentIndex)
			{
				Config.FilterLambdaIndex = argumentIndex;
				return (TFinalBuilder)this;
			}
			
			public TFinalBuilder HasSequenceIndex(int? argumentIndex)
			{
				Config.SequenceIndex = argumentIndex;
				return (TFinalBuilder)this;
			}
		}

		public sealed class AggregateFallbackModeBuilder : AggregateModeBuilderBase<AggregateFallbackModeBuilder>
		{
			internal AggregateFallbackModeBuilder(ModeConfig config) : base(config)
			{
			}

			public void FallbackExpression(Expression? fallbackExpression)
			{
				Config.FallbackExpression = fallbackExpression;
			}
		}

		public sealed class AggregateModeBuilder : AggregateModeBuilderBase<AggregateModeBuilder>
		{
			internal AggregateModeBuilder(ModeConfig config) : base(config)
			{
			}

			public AggregateModeBuilder TranslateArguments(params int[] indexes)
			{
				Config.ArgumentIndexes = indexes;
				return this;
			}

			public AggregateModeBuilder OnBuildFunction(Action<AggregateComposer> build)
			{
				Config.BuildAction = build;
				return this;
			}
		}

		public sealed record AggregateBuildInfo(
			ISqlExpressionFactory                                       Factory,
			ISqlExpression?                                             Value,
			Expression?                                                 ValueExpression,
			ISqlExpression[]                                            Values,
			SqlSearchCondition?                                         FilterCondition,
			SelectQuery?                                                SelectQuery,
			IReadOnlyList<Expression>?                                  FilterExpressions,
			ParameterExpression?                                        ValueParameter,
			(ISqlExpression expr, bool desc, Sql.NullsPosition nulls)[] OrderBySql,
			ISqlExpression?[]                                           Arguments,
			bool                                                        IsGroupBy,
			bool                                                        IsDistinct,
			bool                                                        IsNullFiltered,
			bool                                                        PlainMode)
		{
			public ISqlExpression? Argument(int index) => index >= 0 && index < Arguments.Length ? Arguments[index] : null;
		}

		public sealed class AggregateComposer
		{
			public AggregateComposer(ISqlExpressionFactory factory, AggregateBuildInfo buildInfo, IAggregationContext aggregationContext)
			{
				Factory = factory;
				BuildInfo = buildInfo;
				AggregationContext = aggregationContext;
			}

			public ISqlExpression?                       Result             { get; private set; }
			public SqlErrorExpression?                   Error              { get; private set; }
			public Func<Expression, Expression>?         Validator          { get; private set; }
			public ISqlExpressionFactory                 Factory            { get; }
			public AggregateBuildInfo                    BuildInfo          { get; }
			public ISqlExpressionTranslator              Translator         => AggregationContext;
			public IAggregationContext                   AggregationContext { get; }
			public Action<AggregateFallbackModeBuilder>? Fallback           { get; private set; }

			public void SetResult(ISqlExpression                                 sql)       => Result = sql;
			public void SetError(SqlErrorExpression                              error)     => Error = error;
			public void SetValidation(Func<Expression, Expression> validator) => Validator = validator;

			public void SetFallback(Action<AggregateFallbackModeBuilder> fallback)
			{
				Fallback = fallback;
			}

			public bool GetFilteredToNullValues([NotNullWhen(true)] out ICollection<ISqlExpression>? values, [NotNullWhen(false)] out SqlErrorExpression? error)
			{
				return GetFilteredValues((expression, predicate) => Factory.Condition(predicate, expression, Factory.Null(Factory.GetDbDataType(expression))), out values, out error);
			}

			private bool GetFilteredValues(Func<ISqlExpression, ISqlPredicate, ISqlExpression> decorator, [NotNullWhen(true)] out ICollection<ISqlExpression>? values, [NotNullWhen(false)] out SqlErrorExpression? error)
			{
				values = null;
				error  = null;

				if (BuildInfo.ValueParameter == null)
				{
					throw new InvalidOperationException("Parameter expression is required for filtering.");
				}

				if (BuildInfo.Values.Length == 0)
				{
					error = new SqlErrorExpression("No translated array values", typeof(void));
					return false;
				}

				if (BuildInfo.FilterExpressions is null or [])
				{
					values = BuildInfo.Values;
					return true;
				}

				var resultValues = new List<ISqlExpression>(BuildInfo.Values.Length);

				foreach (var val in BuildInfo.Values)
				{
					SqlSearchCondition searchCondition = new();

					foreach (var filterExpr in BuildInfo.FilterExpressions)
					{
						var placeholder  = new SqlPlaceholderExpression(BuildInfo.SelectQuery, val, filterExpr, convertType: BuildInfo.ValueParameter.Type);
						var replacedExpr = filterExpr.Replace(BuildInfo.ValueParameter, placeholder);

						if (!Translator.TranslateExpression(replacedExpr, out var filterSql, out var filterErr))
						{
							error = filterErr;
							return false;
						}

						searchCondition.Predicates.Add((ISqlPredicate)filterSql);
					}

					var decoratedValue = decorator(val, searchCondition);
					resultValues.Add(decoratedValue);
				}

				values = resultValues;
				return true;
			}
		}
	}
}
