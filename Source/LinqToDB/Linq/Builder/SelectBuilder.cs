using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
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

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var selector = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			// finalizing context
			_ = builder.MakeExpression(sequence, new ContextRefExpression(buildInfo.Expression.Type, sequence),
				ProjectFlags.Expand);

			sequence.SetAlias(selector.Parameters[0].Name);

			var body = selector.Body.Unwrap();

			switch (body.NodeType)
			{
				case ExpressionType.Parameter : break;
				default                       :
					sequence = CheckSubQueryForSelect(sequence, buildInfo);
					break;
			}

			var context = selector.Parameters.Count == 1 ?
				new SelectContext (buildInfo.Parent, selector, buildInfo.IsSubQuery, sequence) :
				new SelectContext2(buildInfo.Parent, selector, buildInfo.IsSubQuery, sequence);

#if DEBUG
			context.Debug_MethodCall = methodCall;
			Debug.WriteLine("BuildMethodCall Select:\n" + context.SelectQuery);
#endif
			return context;
		}

		static IBuildContext CheckSubQueryForSelect(IBuildContext context, BuildInfo buildInfo)
		{
			var createSubquery = context.SelectQuery.Select.IsDistinct;

			if (!createSubquery)
			{
				if (!buildInfo.IsAggregation & !context.SelectQuery.Select.GroupBy.IsEmpty)
					createSubquery = true;
			}

			return createSubquery
				? new SubQueryContext(context)
				: context;
		}

		#endregion

		#region SelectContext2

		sealed class SelectContext2 : SelectContext
		{
			public SelectContext2(IBuildContext? parent, LambdaExpression lambda, bool isSubQuery, IBuildContext sequence)
				: base(parent, lambda, isSubQuery, sequence)
			{
			}

			static readonly ParameterExpression _counterParam = Expression.Parameter(typeof(int), "counter");
		}

		#endregion

		#region Convert

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		#endregion
	}
}
