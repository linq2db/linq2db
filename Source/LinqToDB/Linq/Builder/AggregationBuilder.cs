using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;
	using SqlQuery;
	using Common;
	using LinqToDB.Common.Internal;
	using LinqToDB.Expressions;

	sealed class AggregationBuilder : MethodCallBuilder
	{
		enum AggregationType
		{
			Count,
			Min,
			Max,
			Sum,
			Average,
			Custom
		}

		static readonly string[] MethodNames      = { "Average"     , "Min"     , "Max"     , "Sum",      "Count"     , "LongCount"      };
		static readonly string[] MethodNamesAsync = { "AverageAsync", "MinAsync", "MaxAsync", "SumAsync", "CountAsync", "LongCountAsync" };

		public static Sql.ExpressionAttribute? GetAggregateDefinition(MethodCallExpression methodCall, MappingSchema mapping)
		{
			var function = methodCall.Method.GetExpressionAttribute(mapping);
			return function != null  && function is not Sql.ExtensionAttribute && (function.IsAggregate || function.IsWindowFunction) ? function : null;
		}

		static Type ExtractTaskType(Type taskType)
		{
			return taskType.GetGenericArguments()[0];
		}

		static AggregationType GetAggregationType(MethodCallExpression methodCallExpression, out int argumentsCount, out string functionName, out Type returnType)
		{
			AggregationType aggregationType;
			argumentsCount = methodCallExpression.Arguments.Count;
			returnType     = methodCallExpression.Method.ReturnType;

			switch (methodCallExpression.Method.Name)
			{
				case "Count":
				case "LongCount":
				{
					aggregationType = AggregationType.Count;
					functionName    = "COUNT";
					break;
				}
				case "LongCountAsync":
				{
					--argumentsCount;
					returnType      = typeof(long);
					aggregationType = AggregationType.Count;
					functionName    = "COUNT";
					break;
				}
				case "CountAsync":
				{
					--argumentsCount;
					returnType      = typeof(int);
					aggregationType = AggregationType.Count;
					functionName    = "COUNT";
					break;
				}
				case "Min":
				{
					aggregationType = AggregationType.Min;
					functionName    = "MIN";
					break;
				}
				case "MinAsync":
				{
					--argumentsCount;
					returnType      = ExtractTaskType(returnType);
					aggregationType = AggregationType.Min;
					functionName    = "MIN";
					break;
				}
				case "Max":
				{
					aggregationType = AggregationType.Max;
					functionName    = "MAX";
					break;
				}
				case "MaxAsync":
				{
					--argumentsCount;
					returnType      = ExtractTaskType(returnType);
					aggregationType = AggregationType.Max;
					functionName    = "MAX";
					break;
				}
				case "Sum":
				{
					aggregationType = AggregationType.Sum;
					functionName    = "SUM";
					break;
				}
				case "SumAsync":
				{
					--argumentsCount;
					returnType      = ExtractTaskType(returnType);
					aggregationType = AggregationType.Sum;
					functionName    = "SUM";
					break;
				}
				case "Average":
				{
					aggregationType = AggregationType.Average;
					functionName    = "AVG";
					break;
				}
				case "AverageAsync":
				{
					--argumentsCount;
					returnType      = ExtractTaskType(returnType);
					aggregationType = AggregationType.Average;
					functionName    = "AVG";
					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(methodCallExpression), methodCallExpression.Method.Name, "Invalid aggregation function");
			}

			return aggregationType;
		}

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (methodCall.IsQueryable(MethodNames) || methodCall.IsAsyncExtension(MethodNamesAsync))
				return true;

			var definition = GetAggregateDefinition(methodCall, builder.MappingSchema);

			if (definition != null)
			{
				if (methodCall.Arguments.Count > 0)
				{
					if (builder.IsSequence(new BuildInfo(buildInfo, methodCall.Arguments[0])))
						return true;
				}
			}

			return false;
		}

		public override bool IsAggregationContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

		static string[] AllowedNames = [nameof(Queryable.Select), nameof(Queryable.Where), nameof(Queryable.Distinct)];

		internal class AggregationInfo
		{
			public ISqlExpression?                DistinctValue      { get; set; }
			public ISqlExpression?                ValueSqlExpression { get; set; }
			public bool                           IsDistinct         { get; set; }
			public GroupByBuilder.GroupByContext? GroupByContext     { get; set; }
			public Expression?                    FilterExpression   { get; set; }
			public Expression?                    ValueExpression    { get; set; }
		}

		bool GetSmiplifiedAggregationInfo(
			AggregationType                                        aggregationType, 
			IBuildContext                                          context, 
			BuildInfo                                              buildInfo, 
			Expression                                             expression,
			LambdaExpression?                                      inputValueLambda,
			LambdaExpression?                                      inputFilterLambda,
			out                     Expression?                    filterExpression,
			[NotNullWhen(true)] out GroupByBuilder.GroupByContext? groupByContext,
			out                     Expression?                    valueExpression,
			out                     ISqlExpression?                valueSqlExpression,
			out                     bool                           isDistinct
		)
		{
			filterExpression   = null;
			groupByContext     = null;
			valueSqlExpression = null;
			isDistinct         = false;
			valueExpression    = null;

			var builder    = context.Builder;
			var chain      = new List<MethodCallExpression>();
			var current    = expression;

			ContextRefExpression? contextRef;

			while (true)
			{
				if (current is ContextRefExpression refExpression)
				{
					var root = builder.CorrectRoot(refExpression.BuildContext, current);
					if (ExpressionEqualityComparer.Instance.Equals(root, current))
					{
						contextRef = refExpression;
						break;
					}

					current = root;
					continue;
				}

				if (current is MethodCallExpression methodCall)
				{
					if (methodCall.IsQueryable(AllowedNames))
					{
						chain.Add(methodCall);
						current = methodCall.Arguments[0];
						continue;
					}
				}

				return false;
			}

			if (contextRef is not { BuildContext: GroupByBuilder.GroupByContext groupBy })
			{
				return false;
			}

			groupByContext = groupBy;

			var currentRef = contextRef;
			
			for (int i = chain.Count - 1; i >= 0; i--)
			{
				var method = chain[i];
				if (method.IsQueryable(nameof(Queryable.Distinct)))
				{
					// Distinct should be the first method in the chain
					if (i != 0)
					{
						return false;
					}

					if (aggregationType is AggregationType.Average or AggregationType.Sum or AggregationType.Min or AggregationType.Max)
					{
						if (!builder.DataContext.SqlProviderFlags.IsAggregationDistinctSupported)
						{
							return false;
						}
					}
					else if (aggregationType == AggregationType.Count)
					{
						if (!builder.DataContext.SqlProviderFlags.IsCountDistinctSupported)
						{
							return false;
						}
					}
					else
					{
						return false;
					}

					isDistinct = true;
				}
				else if (method.IsQueryable(nameof(Queryable.Select)))
				{
					// do not support complex projections
					if (method.Arguments.Count != 2)
					{
						return false;
					}

					var body = SequenceHelper.PrepareBody(method.Arguments[1].UnwrapLambda(), currentRef.BuildContext);

					var selectContext = new SelectContext(buildInfo.Parent, body, contextRef.BuildContext, false);
					currentRef = new ContextRefExpression(selectContext.ElementType, selectContext);
				}
				else if (method.IsQueryable(nameof(Queryable.Where)))
				{
					if (aggregationType is not (AggregationType.Count or AggregationType.Sum or AggregationType.Average or AggregationType.Min or AggregationType.Max))
					{
						return false;
					}

					var filter = SequenceHelper.PrepareBody(method.Arguments[1].UnwrapLambda(), currentRef.BuildContext);
					if (filterExpression == null)
						filterExpression = filter;
					else
						filterExpression = Expression.AndAlso(filterExpression, filter);
				}
				else
				{
					return false;
				}
			}

			valueExpression = currentRef;

			if (inputValueLambda != null)
			{
				valueExpression = SequenceHelper.PrepareBody(inputValueLambda, currentRef.BuildContext);
			}

			if (aggregationType != AggregationType.Count || isDistinct)
			{
				var convertedExpr = builder.ConvertToSqlExpr(groupByContext.SubQuery, valueExpression, buildInfo.GetFlags());

				if (!SequenceHelper.IsSqlReady(convertedExpr))
				{
					return false;
				}

				var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(convertedExpr);

				if (placeholders.Count != 1)
				{
					return false;
				}

				valueSqlExpression = placeholders[0].Sql;
			}

			if (inputFilterLambda != null)
			{
				var filter = SequenceHelper.PrepareBody(inputFilterLambda, currentRef.BuildContext);
				if (filterExpression == null)
					filterExpression = filter;
				else
					filterExpression = Expression.AndAlso(filterExpression, filter);
			}

			if (inputFilterLambda != null || filterExpression != null)
			{
				if (aggregationType == AggregationType.Count && isDistinct)
				{
					return false;
				}
			}

			return true;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			SqlPlaceholderExpression functionPlaceholder;
			AggregationContext       context;

			var functionName = methodCall.Method.Name;

			AggregationType aggregationType;

			int  argumentsCount;
			Type returnType;
			var  definition = GetAggregateDefinition(methodCall, builder.MappingSchema);

			if (definition != null)
			{
				aggregationType = AggregationType.Custom;
				returnType      = methodCall.Method.ReturnType;
				argumentsCount  = methodCall.Arguments.Count;
			}
			else
			{
				aggregationType = GetAggregationType(methodCall, out argumentsCount, out functionName, out returnType);
			}

			var sequenceArgument = builder.CorrectRoot(null, methodCall.Arguments[0]);

			if (!buildInfo.IsSubQuery)
			{
				//shorter path

				var sequence = builder.BuildSequence(new BuildInfo(buildInfo, sequenceArgument, new SelectQuery()));

				// finalizing context
				var projected = builder.BuildSqlExpression(sequence,
					new ContextRefExpression(sequence.ElementType, sequence), buildInfo.GetFlags(ProjectFlags.Keys),
					buildFlags : ExpressionBuilder.BuildFlags.ForceAssignments);

				sequence  = new SubQueryContext(sequence);
				projected = builder.UpdateNesting(sequence, projected);

				if (aggregationType == AggregationType.Count)
				{
					if (argumentsCount == 2)
					{
						var lambda = methodCall.Arguments[1].UnwrapLambda();
						sequence = builder.BuildWhere(null, sequence, lambda, false, false, buildInfo.IsTest,
							buildInfo.IsAggregation);

						if (sequence == null)
							return BuildSequenceResult.Error(methodCall);
					}

					functionPlaceholder = ExpressionBuilder.CreatePlaceholder(sequence,
						SqlFunction.CreateCount(returnType, sequence.SelectQuery), buildInfo.Expression,
						convertType : returnType);

					context = new AggregationContext(buildInfo.Parent, sequence, aggregationType, functionName, returnType);
				}
				else
				{
					Expression valueExpression;
					if (argumentsCount == 2)
					{
						var lambda = methodCall.Arguments[1].UnwrapLambda();
						valueExpression = SequenceHelper.PrepareBody(lambda, sequence);
					}
					else
					{
						var elementType = EagerLoading.GetEnumerableElementType(sequenceArgument.Type, sequence.MappingSchema);
						valueExpression = new ContextRefExpression(elementType, sequence);
					}

					var sqlPlaceholder = builder.ConvertToSqlPlaceholder(sequence, valueExpression, ProjectFlags.SQL);
					context = new AggregationContext(buildInfo.Parent, sequence, aggregationType, functionName, returnType);

					var sql = sqlPlaceholder.Sql;

					functionPlaceholder = ExpressionBuilder.CreatePlaceholder(sequence,
						new SqlFunction(returnType, functionName, true, sql) { CanBeNull = true }, buildInfo.Expression, convertType: returnType);
				}
			}
			else
			{
				var isSimple = false;

				IBuildContext? sequence;
				IBuildContext? placeholderSequence;

				var                 parentContext     = buildInfo.Parent!;
				var                 placeholderSelect = parentContext.SelectQuery;
				Expression?         valueExpression;
				ISqlExpression?     valueSqlExpression;
				Expression?         filterExpression;
				LambdaExpression?   inputFilterLambda   = null;
				LambdaExpression?   inputValueLambda    = null;
				SqlSearchCondition? filterSqlExpression = null;

				if (argumentsCount > 1 && aggregationType is AggregationType.Average or AggregationType.Max or AggregationType.Min or AggregationType.Sum)
				{
					inputValueLambda = methodCall.Arguments[1].UnwrapLambda();
				}

				if (argumentsCount > 1 && aggregationType == AggregationType.Count)
				{
					inputFilterLambda = methodCall.Arguments[1].UnwrapLambda();
				}

				if (GetSmiplifiedAggregationInfo(aggregationType, buildInfo.Parent!, buildInfo, sequenceArgument, inputValueLambda, inputFilterLambda, out filterExpression, out var groupByContext, out valueExpression, out valueSqlExpression, out var isDistinct))
				{
					isSimple = true;

					placeholderSequence = groupByContext.SubQuery;
					placeholderSelect   = groupByContext.Element.SelectQuery;
					sequence            = groupByContext;
				}
				else
				{
					var sequenceResult = builder.TryBuildSequence(new BuildInfo(buildInfo, sequenceArgument, new SelectQuery()) { CreateSubQuery = true, IsAggregation = true });

					if (sequenceResult.BuildContext == null)
						return sequenceResult;

					sequence = sequenceResult.BuildContext;
					sequence = new SubQueryContext(sequence);

					if (inputFilterLambda != null)
					{
						sequence = builder.BuildWhere(buildInfo.Parent, sequence, inputFilterLambda, false, false, buildInfo.IsTest, false);
						if (sequence == null)
							return BuildSequenceResult.Error(methodCall);
					}

					valueSqlExpression = null;
					if (inputValueLambda != null)
					{
						valueExpression = SequenceHelper.PrepareBody(inputValueLambda, sequence);
					}
					else
					{
						valueExpression = new ContextRefExpression(sequence.ElementType, sequence);
					}

					placeholderSequence = sequence;
				}

				context = new AggregationContext(buildInfo.Parent, placeholderSequence, aggregationType, functionName, returnType);

				ISqlExpression? sql = null;

				if (isSimple && filterExpression != null)
				{
					var sqlExpr = builder.ConvertToSqlExpr(placeholderSequence, filterExpression, buildInfo.GetFlags());

					if (sqlExpr is not SqlPlaceholderExpression { Sql: SqlSearchCondition searchCondition })
						return BuildSequenceResult.Error(filterExpression);

					filterSqlExpression = searchCondition;
				}

				switch (aggregationType)
				{
					case AggregationType.Count:
					{
						if (isSimple)
						{
							if (isDistinct)
							{
								sql = new SqlExpression("DISTINCT {0}", valueSqlExpression!);
							}
							else
							{
								if (filterSqlExpression != null)
								{
									sql = new SqlFunction(returnType, "CASE", filterSqlExpression, new SqlValue(1), new SqlValue(returnType, null));
								}								
								else
								{
									sql = new SqlExpression("*", new SqlValue(placeholderSequence.SelectQuery.SourceID));
								}
							}

						}
						else
						{
							sql = new SqlExpression("*", new SqlValue(placeholderSequence.SelectQuery.SourceID));
						}

						break;
					}
					case AggregationType.Min:
					case AggregationType.Max:
					case AggregationType.Sum:
					case AggregationType.Average:
					{
						if (isSimple)
						{
							if (valueExpression == null)
								throw new InvalidOperationException();

							if (filterSqlExpression != null)
							{
								sql = new SqlFunction(returnType, "CASE", filterSqlExpression, valueSqlExpression!, new SqlValue(returnType, null));
							}
							else
							{
								sql = valueSqlExpression!;
							}

							if (isDistinct)
							{
								sql = new SqlExpression("DISTINCT {0}", sql);
							}
						}
						else
						{
							if (valueExpression == null)
								throw new InvalidOperationException();

							var sqlExpr = builder.ConvertToSqlExpr(placeholderSequence, valueExpression, buildInfo.GetFlags());
							if (!SequenceHelper.IsSqlReady(sqlExpr))
								return BuildSequenceResult.Error(valueExpression);

							var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(sqlExpr);
							if (placeholders.Count != 1)
								return BuildSequenceResult.Error(valueExpression);

							valueSqlExpression = placeholders[0].Sql;

							sql = valueSqlExpression;
						}
						break;
					}
					case AggregationType.Custom:
					{
						if (definition != null)
						{
							var sqlExpr = definition.GetExpression((builder, context : placeholderSequence, flags: buildInfo.GetFlags()),
								builder.DataContext,
								builder,
								placeholderSelect,
								methodCall,
								static (ctx, e, descriptor, inline) => ctx.builder.ConvertToExtensionSql(ctx.context, ctx.flags, e, descriptor, inline));

							if (sqlExpr is not SqlPlaceholderExpression placeholder)
								return BuildSequenceResult.Error(methodCall);

							builder.RegisterExtensionAccessors(methodCall);

							sql = builder.PosProcessCustomExpression(methodCall, placeholder.Sql, NullabilityContext.GetContext(placeholder.SelectQuery));
						}
						else
						{
							return BuildSequenceResult.Error(methodCall);
						}

						break;
					}
					
				}

				if (sql == null)
					throw new InvalidOperationException();

				if (definition == null)
				{
					sql = new SqlFunction(returnType, functionName, true, sql) { CanBeNull = true };
				}

				functionPlaceholder = ExpressionBuilder.CreatePlaceholder(placeholderSequence, /*context*/sql, buildInfo.Expression, convertType: returnType);

				if (!isSimple)
				{
					context.OuterJoinParentQuery = buildInfo.Parent!.SelectQuery;
				}
			}

			functionPlaceholder.Alias = functionName;
			context.Placeholder       = functionPlaceholder;

			return BuildSequenceResult.FromContext(context);
		}

		sealed class AggregationContext : SequenceContextBase
		{
			public AggregationContext(
				IBuildContext?  parent,
				IBuildContext   sequence,
				AggregationType aggregationType,
				string          methodName,
				Type            returnType)
				: base(parent, sequence, null)
			{
				_returnType      = returnType;
				_aggregationType = aggregationType;
				_methodName      = methodName;
			}

			readonly AggregationType _aggregationType;
			readonly string          _methodName;
			readonly Type            _returnType;

			public SqlPlaceholderExpression Placeholder = null!;
			public SelectQuery?             OuterJoinParentQuery { get; set; }

			SqlJoinedTable? _joinedTable;

			static int CheckNullValue(bool isNull, object context)
			{
				if (isNull)
					throw new InvalidOperationException(
						$"Function {context} returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
				return 0;
			}

			Expression GenerateNullCheckIfNeeded(Expression expression)
			{
				if ((_aggregationType != AggregationType.Sum && _aggregationType != AggregationType.Count) && !expression.Type.IsNullableType())
				{
					var checkExpression = expression;

					if (expression.Type.IsValueType && !expression.Type.IsNullable())
					{
						checkExpression = Expression.Convert(expression, expression.Type.AsNullable());
					}

					expression = Expression.Block(
						Expression.Call(null, MemberHelper.MethodOf(() => CheckNullValue(false, null!)),
							Expression.Equal(checkExpression, Expression.Default(checkExpression.Type)),
							Expression.Constant(_methodName)),
						expression);
				}

				return expression;
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				expr = GenerateNullCheckIfNeeded(expr);

				var mapper = Builder.BuildMapper<object>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			void CreateWeakOuterJoin(SelectQuery parentQuery, SelectQuery selectQuery)
			{
				if (_joinedTable == null)
				{
					var join = selectQuery.OuterApply();
					join.JoinedTable.IsWeak = true;

					_joinedTable = join.JoinedTable;

					parentQuery.From.Tables[0].Joins.Add(join.JoinedTable);

					Placeholder = (SqlPlaceholderExpression)Builder.UpdateNesting(parentQuery, Placeholder);
				}
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (!SequenceHelper.IsSameContext(path, this))
					return path;

				if (flags.HasFlag(ProjectFlags.Root))
					return path;

				if (OuterJoinParentQuery != null)
				{
					if (!flags.HasFlag(ProjectFlags.Test))
					{
						CreateWeakOuterJoin(OuterJoinParentQuery, SelectQuery);
					}
				}

				var result = (Expression)Placeholder;

				if (flags.IsExpression())
					result = GenerateNullCheckIfNeeded(result);

				return result;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				var newContext = new AggregationContext(null, context.CloneContext(Sequence), _aggregationType, _methodName, _returnType);

				newContext.Placeholder          = context.CloneExpression(Placeholder);
				newContext.OuterJoinParentQuery = context.CloneElement(OuterJoinParentQuery);
				newContext._joinedTable         = context.CloneElement(_joinedTable);

				return newContext;
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				return null;
			}
		}
	}
}
