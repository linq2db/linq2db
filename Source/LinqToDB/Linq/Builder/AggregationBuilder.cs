using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	sealed class AggregationBuilder : MethodCallBuilder
	{
		enum AggregationType
		{
			Count,
			Min,
			Max,
			Sum,
			Average,
		}

		static readonly string[] MethodNames      = { "Average"     , "Min"     , "Max"     , "Sum",      "Count"     , "LongCount"      };
		static readonly string[] MethodNamesAsync = { "AverageAsync", "MinAsync", "MaxAsync", "SumAsync", "CountAsync", "LongCountAsync" };


		public static Sql.ExpressionAttribute? GetAggregateDefinition(MethodCallExpression methodCall, MappingSchema mapping)
		{
			var function = methodCall.Method.GetExpressionAttribute(mapping);
			return function != null && (function.IsAggregate || function.IsWindowFunction) ? function : null;
		}

		static AggregationType GetAggregationType(MethodCallExpression methodCallExpression, out int parameterCount)
		{
			AggregationType aggregationType;
			parameterCount = methodCallExpression.Arguments.Count;
			switch (methodCallExpression.Method.Name)
			{
				case "Count":
				{
					aggregationType = AggregationType.Count;
					break;
				}	
				case "CountAsync":
				{
					--parameterCount;
					aggregationType = AggregationType.Count;
					break;
				}	
				case "Min":
				{
					aggregationType = AggregationType.Min;
					break;
				}	
				case "MinAsync":
				{
					--parameterCount;
					aggregationType = AggregationType.Min;
					break;
				}
				case "Max":
				{
					aggregationType = AggregationType.Max;
					break;
				}
				case "MaxAsync":
				{
					--parameterCount;
					aggregationType = AggregationType.Max;
					break;
				}
				case "Sum":
				{
					aggregationType = AggregationType.Sum;
					break;
				}
				case "SumAsync":
				{
					--parameterCount;
					aggregationType = AggregationType.Sum;
					break;
				}
				case "Average":
				{
					aggregationType = AggregationType.Average;
					break;
				}
				case "AverageAsync":
				{
					--parameterCount;
					aggregationType = AggregationType.Average;
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

			return false;
		}

		public override bool IsAggregationContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var methodName = methodCall.Method.Name.Replace("Async", "");
			var returnType = methodCall.Method.ReturnType;

			SqlPlaceholderExpression functionPlaceholder;
			AggregationContext       context;

			var aggregationType = GetAggregationType(methodCall, out var parameterCount);

			var sequenceArgument = builder.CorrectRoot(null, methodCall.Arguments[0]);

			if (!buildInfo.IsSubQuery)
			{
				//shorter path

				var sequence = builder.BuildSequence(new BuildInfo(buildInfo, sequenceArgument, new SelectQuery()));
				if (sequence.SelectQuery.Select.HasModifier)
					sequence = new SubQueryContext(sequence);

				// finalizing context
				_ = builder.MakeExpression(sequence, new ContextRefExpression(buildInfo.Expression.Type, sequence),
					ProjectFlags.Expand);

				if (aggregationType == AggregationType.Count)
				{
					if (parameterCount == 2)
					{
						var lambda = methodCall.Arguments[1].UnwrapLambda();
						sequence = builder.BuildWhere(null, sequence, lambda, false, false, buildInfo.IsTest,
							buildInfo.IsAggregation);
					}

					functionPlaceholder = ExpressionBuilder.CreatePlaceholder(sequence, SqlFunction.CreateCount(returnType, sequence.SelectQuery), buildInfo.Expression);
					context = new AggregationContext(buildInfo.Parent, sequence, methodCall.Method.Name, methodCall.Method.ReturnType);
				}
				else
				{
					Expression valueExpression;
					if (parameterCount == 2)
					{
						var lambda = methodCall.Arguments[1].UnwrapLambda();
						valueExpression = SequenceHelper.PrepareBody(lambda, sequence);
					}
					else
					{
						var elementType = EagerLoading.GetEnumerableElementType(sequenceArgument.Type, builder.MappingSchema);
						valueExpression = new ContextRefExpression(elementType, sequence);
					}

					var sqlPlaceholder = builder.ConvertToSqlPlaceholder(sequence, valueExpression, ProjectFlags.SQL);
					context = new AggregationContext(buildInfo.Parent, sequence, methodCall.Method.Name, methodCall.Method.ReturnType);

					var sql = sqlPlaceholder.Sql;

					functionPlaceholder = ExpressionBuilder.CreatePlaceholder(sequence, 
						new SqlFunction(methodCall.Type, methodName, true, sql) { CanBeNull = true }, buildInfo.Expression);
				}
			}
			else
			{
				var isSimple = false;

				IBuildContext?      sequence            = null;
				IBuildContext?      placeholderSequence = null;


				var parentContext       = buildInfo.Parent!;
				var placeholderSelect   = parentContext.SelectQuery;

				using var testQuery = ExpressionBuilder.QueryPool.Allocate();
				var testSequence = builder.BuildSequence(new BuildInfo(buildInfo, sequenceArgument, testQuery.Value)
					{ AggregationTest = true, IsAggregation = true, IsTest = true });

				// It means that as root we have used fake context
				var testSelectQuery = testSequence.SelectQuery;

				testSelectQuery = testSelectQuery.GetInnerQuery();

				if (testSelectQuery.From.Tables.Count == 0)
				{
					var valid = true;
					if (!testSelectQuery.Where.IsEmpty)
					{
						valid = false;
						/* TODO: we can inject predicate into function if provider supports that
						switch (methodName)
						{
								case "Sum":
								{
									filter = testSelectQuery.
								}
							}
						*/
					}

					if (valid)
					{
						sequence = builder.BuildSequence(
							new BuildInfo(buildInfo, sequenceArgument)
								{ CreateSubQuery = false, IsAggregation = true });

						var sequenceRef = new ContextRefExpression(sequence.ElementType, sequence);

						var rootContext = builder.GetRootContext(buildInfo.Parent, sequenceRef, true);

						placeholderSequence = rootContext?.BuildContext ?? sequence;

						if (placeholderSequence is GroupByBuilder.GroupByContext groupCtx)
						{
							placeholderSequence = groupCtx.SubQuery;
							placeholderSelect   = groupCtx.Element.SelectQuery;
							isSimple            = true;
						}
					}
				}

				if (sequence is null)
				{
					sequence = builder.BuildSequence(new BuildInfo(buildInfo, sequenceArgument, new SelectQuery()) { CreateSubQuery = true, IsAggregation = true });

					placeholderSequence ??= sequence;

					if (sequence is GroupByBuilder.GroupByContext groupByContext)
					{
						placeholderSequence = groupByContext.SubQuery;
					}
				}

				placeholderSequence ??= sequence;

				Expression valueExpression;
				if (parameterCount == 2)
				{
					var lambda = methodCall.Arguments[1].UnwrapLambda();
					valueExpression = SequenceHelper.PrepareBody(lambda, sequence);
				}
				else
				{
					valueExpression = new ContextRefExpression(methodCall.Type, sequence);
				}

				context = new AggregationContext(buildInfo.Parent, placeholderSequence, methodCall.Method.Name,
					methodCall.Method.ReturnType);

				ISqlExpression sql;

				if (aggregationType == AggregationType.Count)
				{
					if (parameterCount == 2)
					{
						var sqlPlaceholder = builder.ConvertToSqlPlaceholder(placeholderSequence, valueExpression,
							buildInfo.GetFlags());

						if (isSimple)
						{
							sql = new SqlFunction(methodCall.Type, "CASE", sqlPlaceholder.Sql, new SqlValue(1),
								new SqlValue(methodCall.Type, null));
						}
						else
						{
							if (sqlPlaceholder.Sql is not SqlSearchCondition sc)
								throw new InvalidOperationException(
									$"Expected SearchCondition, but found: '{sqlPlaceholder.Sql}'");

							sql = new SqlExpression("*", new SqlValue(placeholderSequence.SelectQuery.SourceID));
							placeholderSequence.SelectQuery.Where.ConcatSearchCondition(sc);
						}
					}
					else
					{
						// OR sql = new SqlValue(typeof(int), 1);
						sql = new SqlExpression("*", new SqlValue(placeholderSequence.SelectQuery.SourceID));
					}
				}
				else
				{
					var sqlPlaceholder =
						builder.ConvertToSqlPlaceholder(placeholderSequence, valueExpression, buildInfo.GetFlags());
					sql = sqlPlaceholder.Sql;
				}

				functionPlaceholder = ExpressionBuilder.CreatePlaceholder(placeholderSequence, /*context*/
					new SqlFunction(methodCall.Type, methodName, true, sql) { CanBeNull = true }, buildInfo.Expression);

				if (!isSimple)
				{
					context.OuterJoinParentQuery = buildInfo.Parent!.SelectQuery;
				}
			}


			functionPlaceholder.Alias = methodName;
			context.Placeholder       = functionPlaceholder;

			return context;
		}

		sealed class AggregationContext : SequenceContextBase
		{
			public AggregationContext(IBuildContext? parent, IBuildContext sequence, string methodName, Type returnType)
				: base(parent, sequence, null)
			{
				_returnType = returnType;
				_methodName = methodName;

				if (_returnType.IsGenericType && _returnType.GetGenericTypeDefinition() == typeof(Task<>))
				{
					_returnType = _returnType.GetGenericArguments()[0];
					_methodName = _methodName.Replace("Async", "");
				}
			}

			readonly string     _methodName;
			readonly Type       _returnType;

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

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
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
				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Root))
					return path;

				if (OuterJoinParentQuery != null)
				{
					if (!flags.HasFlag(ProjectFlags.Test))
					{
						CreateWeakOuterJoin(OuterJoinParentQuery, SelectQuery);
					}
				}

				return Placeholder;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				var newContext = new AggregationContext(null, context.CloneContext(Sequence), _methodName, _returnType);

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
