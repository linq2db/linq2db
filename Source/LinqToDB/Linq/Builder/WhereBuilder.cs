using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	class WhereBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("Where", "Having");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var isHaving  = methodCall.Method.Name == "Having";
			var sequence  = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var condition = (LambdaExpression)methodCall.Arguments[1].Unwrap();

			if (sequence.SelectQuery.Select.IsDistinct        ||
			    sequence.SelectQuery.Select.TakeValue != null ||
			    sequence.SelectQuery.Select.SkipValue != null)
				sequence = new SubQueryContext(sequence);

			var result    = builder.BuildWhere(buildInfo.Parent, sequence, condition, !isHaving, isHaving);

			result.SetAlias(condition.Parameters[0].Name);

			return result;
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			var predicate = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var info      = builder.ConvertSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]), predicate.Parameters[0]);

			if (info != null)
			{
				info.Expression = methodCall.Transform(ex => ConvertMethod(methodCall, 0, info, predicate.Parameters[0], ex));

				if (param != null)
				{
					if (param.Type != info.Parameter.Type)
						param = Expression.Parameter(info.Parameter.Type, param.Name);

					if (info.ExpressionsToReplace != null)
						foreach (var path in info.ExpressionsToReplace)
						{
							path.Path = path.Path.Transform(e => e == info.Parameter ? param : e);
							path.Expr = path.Expr.Transform(e => e == info.Parameter ? param : e);
						}
				}

				info.Parameter = param;

				return info;
			}

			return null;
		}
	}
}
