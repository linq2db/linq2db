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
					agg => Combine(ctx, agg, functionCall, _plain, true));

				if (arrayResult is SqlPlaceholderExpression)
				{
					return arrayResult;
				}
			}

			return ctx.BuildAggregationFunction(
				sequenceExpr,
				functionCall,
				_aggregate.ToAllowedOps(),
				agg => Combine(ctx, agg, functionCall, _aggregate, false));
		}

		private (ISqlExpression? sql, SqlErrorExpression? error) Combine(
			ITranslationContext  ctx,
			IAggregationContext  raw,
			MethodCallExpression functionCall,
			ModeConfig           config,
			bool                 plainMode)
		{
			var factory = ctx.ExpressionFactory;

			var translatedArgs = new ISqlExpression?[config.ArgumentIndexes.Length];
			for (var i = 0; i < config.ArgumentIndexes.Length; i++)
			{
				var argIndex = config.ArgumentIndexes[i];
				if (argIndex < 0 || argIndex >= functionCall.Arguments.Count)
				{
					return (null, ctx.CreateErrorExpression(functionCall, $"Argument index {argIndex.ToString(CultureInfo.InvariantCulture)} out of range", functionCall.Type));
				}

				var argExpr = functionCall.Arguments[argIndex];
				if (!raw.TranslateExpression(argExpr, out var argSql, out var argErr))
				{
					return (null, argErr);
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

			var itemsSql = Array.Empty<ISqlExpression>();
			if (plainMode && raw.Items != null)
			{
				itemsSql = new ISqlExpression[raw.Items.Length];
				for (var i = 0; i < raw.Items.Length; i++)
				{
					var itemExpr = raw.Items[i];
					if (!raw.TranslateExpression(itemExpr, out var itemSql, out var itemErr))
					{
						return (null, itemErr);
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
					return (null, obErr);
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
							if (expr.NodeType != ExpressionType.Equal)
							{
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
						return (null, fErr ?? ctx.CreateErrorExpression(filterCondition, type: functionCall.Type));
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
			var buildErr = config.BuildAction?.Invoke(composer);
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

		/// <summary>Configuration data container.</summary>
		public sealed class ModeConfig
		{
			internal bool                                          AllowDistinctFlag;
			internal bool                                          AllowFilterFlag;
			internal bool?                                         AllowNotNullCheckMode;
			internal bool                                          AllowOrderByFlag;
			internal int[]                                         ArgumentIndexes = Array.Empty<int>();
			internal Func<AggregateComposer, SqlErrorExpression?>? BuildAction;

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

		/// <summary>Fluent builder for mode configuration.</summary>
		public sealed class AggregateModeBuilder
		{
			readonly ModeConfig _config;
			internal AggregateModeBuilder(ModeConfig config) => _config = config;

			public AggregateModeBuilder AllowOrderBy()
			{
				_config.AllowOrderByFlag = true;
				return this;
			}

			public AggregateModeBuilder AllowFilter()
			{
				_config.AllowFilterFlag = true;
				return this;
			}

			public AggregateModeBuilder AllowDistinct()
			{
				_config.AllowDistinctFlag = true;
				return this;
			}

			public AggregateModeBuilder AllowNotNullCheck(bool removeFromFilter)
			{
				_config.AllowNotNullCheckMode = removeFromFilter;
				return this;
			}

			public AggregateModeBuilder TranslateArguments(params int[] indexes)
			{
				_config.ArgumentIndexes = indexes;
				return this;
			}

			public AggregateModeBuilder OnBuildFunction(Func<AggregateComposer, SqlErrorExpression?> build)
			{
				_config.BuildAction = build;
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
				Factory    = factory;
				BuildInfo  = buildInfo;
				Translator = translator;
			}

			public ISqlExpression?          Result                        { get; private set; }
			public ISqlExpressionFactory    Factory                       { get; }
			public AggregateBuildInfo       BuildInfo                     { get; }
			public ISqlExpressionTranslator Translator                    { get; }
			public void                     SetResult(ISqlExpression sql) => Result = sql;

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
