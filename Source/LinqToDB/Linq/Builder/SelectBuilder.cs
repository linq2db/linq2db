using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	[BuildsMethodCall("Select")]
	sealed class SelectBuilder : MethodCallBuilder
	{
		#region SelectBuilder

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
		{
			if (!call.IsQueryable())
				return false;
			
			var lambda = (LambdaExpression)call.Arguments[1].Unwrap();
			return lambda.Parameters.Count is 1 or 2;
		}

		public override bool IsAggregationContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			// Select is transparent and we can treat it as an aggregation.
			return true;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var selector    = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (buildResult.BuildContext == null)
				return buildResult;

			var sequence = buildResult.BuildContext;

			// finalizing context
			_ = builder.BuildExtractExpression(sequence, new ContextRefExpression(sequence.ElementType, sequence));

			sequence.SetAlias(selector.Parameters[0].Name);

			var body = selector.Parameters.Count == 1
				? SequenceHelper.PrepareBody(selector, sequence)
				: SequenceHelper.PrepareBody(selector, sequence, new CounterContext(sequence));

			var context       = new SelectContext (buildInfo.Parent, body, sequence, buildInfo.IsSubQuery);
			var resultContext = (IBuildContext) context;

			// finalizing context and checking if we need to wrap it into subquery
			var translated = builder.BuildSqlExpressionForTest(context, new ContextRefExpression(context.ElementType, context));

			if (SequenceHelper.ContainsAggregateOrWindowFunction(translated))
			{
				resultContext = new SubQueryContext(resultContext);
			}

#if DEBUG
			context.Debug_MethodCall = methodCall;
#endif
			return BuildSequenceResult.FromContext(resultContext);
		}

		#endregion

		class CounterContext : BuildContextBase
		{
			public CounterContext(IBuildContext sequence) : this(sequence.Builder, sequence.SelectQuery)
			{
			}

			CounterContext(ExpressionBuilder builder, SelectQuery selectQuery) : base(builder, typeof(int), selectQuery)
			{

			}

			public override MappingSchema MappingSchema => Builder.MappingSchema;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this))
				{
					if (flags.IsExpression())
					{
						return ExpressionBuilder.RowCounterParam;
					}
				}

				return path;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new CounterContext(Builder, context.CloneElement(SelectQuery));
			}

			public override SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(SelectQuery);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}
		}
	}
}
