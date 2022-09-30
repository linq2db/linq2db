using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	class AggregationBuilder : MethodCallBuilder
	{
		public  static readonly string[] MethodNames      = { "Average"     , "Min"     , "Max"     , "Sum",      "Count"     , "LongCount"      };
		private static readonly string[] MethodNamesAsync = { "AverageAsync", "MinAsync", "MaxAsync", "SumAsync", "CountAsync", "LongCountAsync" };

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
					Expression refExpression = new ContextRefExpression(sequenceArgument.Type, sequence);

					var sqlPlaceholder = builder.ConvertToSqlPlaceholder(sequence, refExpression, ProjectFlags.SQL);
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
					{ AggregationTest = true, IsAggregation = true });

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

						var rootContext = builder.GetRootContext(null, sequenceArgument, false);

						if (rootContext != null)
						{
							placeholderSequence = rootContext.BuildContext;
							placeholderSelect   = rootContext.BuildContext.SelectQuery;

							if (placeholderSequence is GroupByBuilder.GroupByContext groupCtx)
							{
								placeholderSequence = groupCtx.SubQuery;
								placeholderSelect   = groupCtx.Element.SelectQuery;
							}
						}
						else
						{
							throw new NotImplementedException();
						}

						isSimple = true;
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
					Expression refExpression = new ContextRefExpression(sequenceArgument.Type, sequence);

					var sqlPlaceholder = builder.ConvertToSqlPlaceholder(placeholderSequence, refExpression, ProjectFlags.SQL);
					context = new AggregationContext(buildInfo.Parent, sequence, methodCall.Method.Name, methodCall.Method.ReturnType);

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

			/*// force ExpressionBuilder to cache Aggregation SQL. It will be used later for BuildWhere.
			_ = builder.ConvertToSqlExpr(context, new ContextRefExpression(methodCall.Method.ReturnType, context),
				buildInfo.GetFlags());*/

			return context;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		class AggregationContext : SequenceContextBase
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

			static int CheckNullValue(bool isNull, object context)
			{
				if (isNull)
					throw new InvalidOperationException(
						$"Function {context} returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
				return 0;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new NotImplementedException();
				/*var expr = Builder.FinalizeProjection(this,
					Builder.MakeExpression(new ContextRefExpression(typeof(T), this), ProjectFlags.Expression));

				var mapper = Builder.BuildMapper<object>(expr);

				QueryRunner.SetRunQuery(query, mapper);*/
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<object>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			private bool _joinCreated;

			void CreateWeakOuterJoin(SelectQuery parentQuery, SelectQuery selectQuery)
			{
				if (!_joinCreated)
				{
					_joinCreated = true;

					var join = selectQuery.OuterApply();
					join.JoinedTable.IsWeak = true;

					parentQuery.From.Tables[0].Joins.Add(join.JoinedTable);
				}
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
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
				return new AggregationContext(null, context.CloneContext(Sequence), _methodName, _returnType);
            }

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}
		}
	}
}
