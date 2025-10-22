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

		public Expression? Build(ITranslationContext ctx, Expression sequenceExpr, MethodCallExpression functionCall)
		{
			if (_plain.BuildAction != null)
			{
				var arrayResult = ctx.BuildArrayAggregationFunction(
					sequenceExpr,
					functionCall,
					_plain.ToAllowedOps(),
					agg => Combine(ctx, agg, functionCall, _plain, sequenceExpr, true));

				if (arrayResult is SqlPlaceholderExpression)
				{
					return arrayResult;
				}
			}

			return ctx.BuildAggregationFunction(
				sequenceExpr,
				functionCall,
				_aggregate.ToAllowedOps(),
				agg => Combine(ctx, agg, functionCall, _aggregate, sequenceExpr, false));
		}

		private (ISqlExpression? sql, SqlErrorExpression? error, Expression? fallbackExpression) Combine(
			ITranslationContext  ctx,
			IAggregationContext  raw,
			MethodCallExpression functionCall,
			ModeConfig           config,
			Expression           sequenceExpr,
			bool                 plainMode)
		{
			var factory = ctx.ExpressionFactory;

			var translatedArgs = new ISqlExpression?[config.ArgumentIndexes.Length];
			for (var i = 0; i < config.ArgumentIndexes.Length; i++)
			{
				var argIndex = config.ArgumentIndexes[i];
				if (argIndex < 0 || argIndex >= functionCall.Arguments.Count)
				{
					return (null, ctx.CreateErrorExpression(functionCall, $"Argument index {argIndex.ToString(CultureInfo.InvariantCulture)} out of range", functionCall.Type), null);
				}

				var argExpr = functionCall.Arguments[argIndex];
				if (!raw.TranslateExpression(argExpr, out var argSql, out var argErr))
				{
					return (null, argErr, null);
				}

				translatedArgs[i] = argSql!;
			}

			ISqlExpression? valueSql = null;
			if (raw.ValueExpression != null)
			{
				if (!raw.TranslateExpression(raw.ValueExpression, out valueSql, out var vErr))
				{
					return (null, vErr, null);
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
						return (null, itemErr, null);
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
					return (null, obErr, null);
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
						return (null, fErr, null);
					}

					filterSql = filterExprSql as SqlSearchCondition ?? new SqlSearchCondition().Add(new SqlPredicate.Expr(filterExprSql!));
				}

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
				itemsSql,
				filterSql,
				raw.SelectQuery,
				filterExpressions,
				raw.ValueParameter,
				orderSql,
				translatedArgs,
				raw.IsDistinct,
				isNullFiltered,
				plainMode
			);

			var composer = new AggregateComposer(factory, info, raw);
			config.BuildAction?.Invoke(composer);

			if (composer.Error != null)
			{
				return (null, composer.Error, null);
			}

			if (composer.Fallback != null)
			{
				var fallbackConfig = config.Clone();
				var fallbackBuilder = new AggregateFallbackModeBuilder(fallbackConfig);
				composer.Fallback(fallbackBuilder);

				var fallbackResult = ctx.BuildAggregationFunction(
					sequenceExpr,
					functionCall,
					_plain.ToAllowedOps(),
					agg => Combine(ctx, agg, functionCall, _plain, sequenceExpr, true));

				if (fallbackResult is SqlPlaceholderExpression placeholder)
				{
					return (placeholder.Sql, null, null);
				}

				if (fallbackResult is SqlErrorExpression errorExpression)
				{
					return (null, errorExpression, null);
				}
			
				return (null, null, fallbackResult);
			}

			if (composer.Result == null)
			{
				return (null, ctx.CreateErrorExpression(functionCall, "Aggregate builder produced no result", functionCall.Type), null);
			}

			return (composer.Result, null, null);
		}

		public sealed class ModeConfig
		{
			internal bool  AllowDistinctFlag;
			internal bool  AllowFilterFlag;
			internal bool? AllowNotNullCheckMode;
			internal bool  AllowOrderByFlag;
			internal int[] ArgumentIndexes = Array.Empty<int>();

			internal Action<AggregateComposer>? BuildAction;

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
					BuildAction           = BuildAction
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
		}

		public sealed class AggregateFallbackModeBuilder : AggregateModeBuilderBase<AggregateFallbackModeBuilder>
		{
			internal AggregateFallbackModeBuilder(ModeConfig config) : base(config)
			{
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
			ISqlExpression[]                                            Values,
			SqlSearchCondition?                                         FilterCondition,
			SelectQuery?                                                SelectQuery,
			IReadOnlyList<Expression>?                                  FilterExpressions,
			ParameterExpression?                                        ValueParameter,
			(ISqlExpression expr, bool desc, Sql.NullsPosition nulls)[] OrderBySql,
			ISqlExpression?[]                                           Arguments,
			bool                                                        IsDistinct,
			bool                                                        IsNullFiltered,
			bool                                                        PlainMode)
		{
			public ISqlExpression? Argument(int index) => index >= 0 && index < Arguments.Length ? Arguments[index] : null;
		}

		public sealed class AggregateComposer
		{
			public AggregateComposer(ISqlExpressionFactory factory, AggregateBuildInfo buildInfo, ISqlExpressionTranslator translator)
			{
				Factory = factory;
				BuildInfo = buildInfo;
				Translator = translator;
			}

			public ISqlExpression?                       Result     { get; private set; }
			public SqlErrorExpression?                   Error      { get; private set; }
			public ISqlExpressionFactory                 Factory    { get; }
			public AggregateBuildInfo                    BuildInfo  { get; }
			public ISqlExpressionTranslator              Translator { get; }
			public Action<AggregateFallbackModeBuilder>? Fallback   { get; private set; }

			public void SetResult(ISqlExpression sql) => Result = sql;
			public void SetError(SqlErrorExpression error) => Error = error;

			public void SetFallback(Action<AggregateFallbackModeBuilder> fallback)
			{
				Fallback = fallback;
			}

			public bool GetFilteredToNullValues([NotNullWhen(true)] out IEnumerable<ISqlExpression>? values, [NotNullWhen(false)] out SqlErrorExpression? error)
			{
				return GetFilteredValues((expression, predicate) => Factory.Condition(predicate, expression, Factory.Null(Factory.GetDbDataType(expression))), out values, out error);
			}

			private bool GetFilteredValues(Func<ISqlExpression, ISqlPredicate, ISqlExpression> decorator, [NotNullWhen(true)] out IEnumerable<ISqlExpression>? values, [NotNullWhen(false)] out SqlErrorExpression? error)
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

				if (BuildInfo.FilterExpressions == null || BuildInfo.FilterExpressions.Count == 0)
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
