using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	/// <summary>
	///     Fluent builder to configure aggregate/"plain" aggregation translation using existing BuildAggregationFunction
	///     pipeline.
	///     Translates configured argument indexes and exposes only SQL-ready expressions to the combine callbacks.
	/// </summary>
	public sealed class AggregateFunctionBuilder
	{
		readonly ModeConfig _aggregate = new();
		readonly ModeConfig _plain     = new();

		public AggregateFunctionBuilder ConfigureAggregate(Action<ModeConfig> cfg)
		{
			cfg(_aggregate);
			return this;
		}

		public AggregateFunctionBuilder ConfigurePlain(Action<ModeConfig> cfg)
		{
			cfg(_plain);
			return this;
		}

		public Expression? Build(ITranslationContext ctx, Expression sequenceExpr, MethodCallExpression functionCall)
		{
			if (_plain.BuildAction != null)
			{
				var arrayResult = ctx.BuildArrayAggregationFunction(
					sequenceExpr,
					functionCall,
					_aggregate.ToAllowedOps(),
					agg => Combine(ctx, agg, functionCall, _plain, true));

				if (arrayResult is SqlPlaceholderExpression)
				{
					return arrayResult;
				}
			}

			var result = ctx.BuildAggregationFunction(
				sequenceExpr,
				functionCall,
				_aggregate.ToAllowedOps(),
				agg => Combine(ctx, agg, functionCall, _aggregate, false));

			return result;
		}

		private (ISqlExpression? sql, SqlErrorExpression? error) Combine(
			ITranslationContext  ctx,
			IAggregationContext  raw,
			MethodCallExpression functionCall,
			ModeConfig           config,
			bool                 plainMode)
		{
			var factory = ctx.ExpressionFactory;

			// translate configured call arguments
			var translatedArgs = new ISqlExpression?[config.ArgumentIndexes.Length];
			for (int i = 0; i < config.ArgumentIndexes.Length; i++)
			{
				var argIndex = config.ArgumentIndexes[i];
				if (argIndex < 0 || argIndex >= functionCall.Arguments.Count)
				{
					return (null, ctx.CreateErrorExpression(functionCall, $"Argument index {argIndex.ToString(CultureInfo.InvariantCulture)} out of range", functionCall.Type));
				}

				var argExpr = functionCall.Arguments[argIndex];
				if (!raw.TranslateExpression(argExpr, out var argSql, out var argErr))
				{
					return (null, argErr ?? ctx.CreateErrorExpression(argExpr, type: functionCall.Type));
				}

				translatedArgs[i] = argSql!;
			}

			ISqlExpression? valueSql = null;
			if (raw.ValueExpression != null)
			{
				if (!raw.TranslateExpression(raw.ValueExpression, out valueSql, out var vErr))
				{
					return (null, vErr ?? ctx.CreateErrorExpression(raw.ValueExpression, type : functionCall.Type));
				}
			}

			ISqlExpression[] itemsSql = Array.Empty<ISqlExpression>();
			if (plainMode && raw.Items != null)
			{
				itemsSql = new ISqlExpression[raw.Items.Length];
				for (int i = 0; i < raw.Items.Length; i++)
				{
					var itemExpr = raw.Items[i];
					if (!raw.TranslateExpression(itemExpr, out var itemSql, out var itemErr))
					{
						return (null, itemErr ?? ctx.CreateErrorExpression(itemExpr, type : functionCall.Type));
					}

					itemsSql[i] = itemSql!;
				}
			}

			var orderSql = new (ISqlExpression expr, bool desc, Sql.NullsPosition nulls)[raw.OrderBy.Length];
			for (int i = 0; i < raw.OrderBy.Length; i++)
			{
				var obInfo = raw.OrderBy[i];
				if (!raw.TranslateExpression(obInfo.Expr, out var obSql, out var obErr))
				{
					return (null, obErr ?? ctx.CreateErrorExpression(obInfo.Expr, type : functionCall.Type));
				}

				orderSql[i] = (obSql!, obInfo.IsDescending, obInfo.Nulls);
			}

			SqlSearchCondition? filterSql = null;
			if (raw.FilterExpression != null)
			{
				if (!raw.TranslateExpression(raw.FilterExpression, out var filterExprSql, out var fErr))
				{
					return (null, fErr ?? ctx.CreateErrorExpression(raw.FilterExpression, type : functionCall.Type));
				}

				filterSql = filterExprSql as SqlSearchCondition ?? new SqlSearchCondition().Add(new SqlPredicate.Expr(filterExprSql!));
			}

			var IsNullFiltered = false;

			if (filterSql != null && valueSql != null)
			{
				// handle NotNullCheck mode
				if (config.AllowNotNullCheckMode.HasValue)
				{
					var removeFromFilter = config.AllowNotNullCheckMode.Value;

					if (filterSql is { IsAnd: true })
					{
						var isNotNull = filterSql.Predicates.FirstOrDefault(p => p is SqlPredicate.IsNull { IsNot: true } isNull && isNull.Expr1.Equals(valueSql));
						if (isNotNull != null)
						{
							IsNullFiltered = true;
							if (removeFromFilter)
							{
								filterSql.Predicates.Remove(isNotNull);
								if (filterSql.Predicates.Count == 0)
								{
									filterSql = null;
								}
							}
						}
					}

				}
			}

			var info = new AggregateBuildInfo(
				factory,
				valueSql,
				itemsSql,
				filterSql,
				orderSql,
				translatedArgs,
				raw.IsDistinct,
				IsNullFiltered,
				plainMode);

			var composer = new AggregateComposer(factory);
			var buildErr = config.BuildAction?.Invoke(info, composer);
			if (buildErr != null)
			{
				return (null, buildErr);
			}

			if (composer.Result == null)
			{
				return (null, ctx.CreateErrorExpression(functionCall, "Aggregate builder produced no result", functionCall.Type));
			}

			return (composer.Result, null);
		}

		public sealed class ModeConfig
		{
			internal bool                                                              AllowDistinctFlag;
			internal bool                                                              AllowFilterFlag;
			internal bool?                                                             AllowNotNullCheckMode;
			internal bool                                                              AllowOrderByFlag;
			internal int[]                                                             ArgumentIndexes = Array.Empty<int>();
			internal Func<AggregateBuildInfo, AggregateComposer, SqlErrorExpression?>? BuildAction;

			public ModeConfig AllowOrderBy()
			{
				AllowOrderByFlag = true;
				return this;
			}

			public ModeConfig AllowFilter()
			{
				AllowFilterFlag = true;
				return this;
			}

			public ModeConfig AllowDistinct()
			{
				AllowDistinctFlag = true;
				return this;
			}

			public ModeConfig AllowNotNullCheck(bool removeFromFilter)
			{
				AllowNotNullCheckMode = removeFromFilter;
				return this;
			}

			public ModeConfig TranslateArguments(params int[] indexes)
			{
				ArgumentIndexes = indexes ?? Array.Empty<int>();
				return this;
			}

			public ModeConfig OnBuildFunction(Func<AggregateBuildInfo, AggregateComposer, SqlErrorExpression?> build)
			{
				BuildAction = build;
				return this;
			}

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
		}

		public sealed record AggregateBuildInfo(
			ISqlExpressionFactory                                       Factory,
			ISqlExpression?                                             Value,
			ISqlExpression[]                                            Values,
			SqlSearchCondition?                                         FilterCondition,
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
			public AggregateComposer(ISqlExpressionFactory factory) => Factory = factory;
			public ISqlExpression?       Result  { get; private set; }
			public ISqlExpressionFactory Factory { get; }

			public void SetResult(ISqlExpression sql) => Result = sql;
		}
	}
}
