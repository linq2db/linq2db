using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;
	using LinqToDB.Expressions;

	sealed class SelectBuilder : MethodCallBuilder
	{
		#region SelectBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (methodCall.IsQueryable("Select"))
			{
				switch (((LambdaExpression)methodCall.Arguments[1].Unwrap()).Parameters.Count)
				{
					case 1 :
					case 2 : return true;
				}
			}

			return false;
		}

		public override bool IsAggregationContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			// Select is transparent and we can treat it as an aggregation.
			return true;
		}

		protected override IBuildContext? BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var selector = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (sequence == null)
				return null;

			// finalizing context
			_ = builder.MakeExpression(sequence, new ContextRefExpression(sequence.ElementType, sequence),
				ProjectFlags.Expand);

			sequence.SetAlias(selector.Parameters[0].Name);

			var createSubquery = true;

			if (sequence is SubQueryContext)
			{
				if (sequence.SelectQuery.IsSimple)
					createSubquery = false;
			}

			if (createSubquery)
				sequence = new SubQueryContext(sequence);

			var body = selector.Parameters.Count == 1
				? SequenceHelper.PrepareBody(selector, sequence)
				: SequenceHelper.PrepareBody(selector, sequence, new CounterContext(sequence));

			var context = new SelectContext (buildInfo.Parent, body, sequence, buildInfo.IsSubQuery);
#if DEBUG
			context.Debug_MethodCall = methodCall;
			Debug.WriteLine("BuildMethodCall Select:\n" + context.SelectQuery);
#endif
			return context;
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
				throw new NotImplementedException();
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}
		}

		#region Convert

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		#endregion
	}
}
