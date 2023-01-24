using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	sealed class AggregationBuilder : MethodCallBuilder
	{
		public static readonly string[] MethodNames      = { "Average"     , "Min"     , "Max"     , "Sum",      "Count"     , "LongCount"      };
		       static readonly string[] MethodNamesAsync = { "AverageAsync", "MinAsync", "MaxAsync", "SumAsync", "CountAsync", "LongCountAsync" };

		public static Sql.ExpressionAttribute? GetAggregateDefinition(MethodCallExpression methodCall, MappingSchema mapping)
		{
			var function = methodCall.Method.GetExpressionAttribute(mapping);
			return function != null && (function.IsAggregate || function.IsWindowFunction) ? function : null;
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

				if (methodName == "Count" || methodName == "LongCount")
				{
					functionPlaceholder = ExpressionBuilder.CreatePlaceholder(sequence, SqlFunction.CreateCount(returnType, sequence.SelectQuery), buildInfo.Expression);
					context = new AggregationContext(buildInfo.Parent, sequence, methodCall.Method.Name, methodCall.Method.ReturnType);
				}
				else
				{
					Expression valueExpression;
					if (methodCall.Arguments.Count == 2)
					{
						var lambda = methodCall.Arguments[1].UnwrapLambda();
						valueExpression = SequenceHelper.PrepareBody(lambda, sequence);
					}
					else
						valueExpression = new ContextRefExpression(sequenceArgument.Type, sequence);

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

				var testSequence = builder.BuildSequence(new BuildInfo(buildInfo, sequenceArgument, new SelectQuery())
					{ AggregationTest = true, IsAggregation = true, IsTest = true });

				// It means that as root we have used fake context
				var testSelectQuery = testSequence.SelectQuery;
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

						var rootContext = builder.GetRootContext(buildInfo.Parent, sequenceArgument, false);

						if (rootContext != null)
						{
							placeholderSequence = rootContext.BuildContext;
							placeholderSelect   = rootContext.BuildContext.SelectQuery;

							if (placeholderSequence is GroupByBuilder.GroupByContext groupCtx)
							{
								placeholderSequence = groupCtx.SubQuery;
								placeholderSelect   = groupCtx.Element.SelectQuery;
							}

							isSimple = true;
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
						placeholderSelect   = groupByContext.Element.SelectQuery;
					}
				}

				placeholderSequence ??= sequence;

				if (methodName == "Count" || methodName == "LongCount")
				{
					functionPlaceholder = ExpressionBuilder.CreatePlaceholder(placeholderSequence, SqlFunction.CreateCount(returnType, placeholderSelect), buildInfo.Expression);
					context = new AggregationContext(buildInfo.Parent, sequence, methodCall.Method.Name, methodCall.Method.ReturnType);
				}
				else
				{
					Expression valueExpression;
					if (methodCall.Arguments.Count == 2)
					{
						var lambda = methodCall.Arguments[1].UnwrapLambda();
						valueExpression = SequenceHelper.PrepareBody(lambda, sequence);
					}
					else
					{
						valueExpression = new ContextRefExpression(methodCall.Type, sequence);
					};

					var sqlPlaceholder = builder.ConvertToSqlPlaceholder(placeholderSequence, valueExpression, buildInfo.GetFlags());
					context = new AggregationContext(buildInfo.Parent, placeholderSequence, methodCall.Method.Name, methodCall.Method.ReturnType);

					var sql = sqlPlaceholder.Sql;

					functionPlaceholder = ExpressionBuilder.CreatePlaceholder(placeholderSequence, /*context*/
						new SqlFunction(methodCall.Type, methodName, true, sql) { CanBeNull = true }, buildInfo.Expression);
				}

				if (!isSimple)
				{
					context.OuterJoinParentQuery = buildInfo.Parent!.SelectQuery;
				}
			}


			functionPlaceholder.Alias = methodName;
			context.Placeholder       = functionPlaceholder;

			return context;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
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
