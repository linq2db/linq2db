using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	class WhereBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames = { "Where", "Having" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames);
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

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			var predicate = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var info      = builder.ConvertSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]), predicate.Parameters[0], true);

			if (info != null)
			{
				info.Expression = methodCall.Transform(new { methodCall, info, predicate }, static (context, ex) => ConvertMethod(context.methodCall, 0, context.info, context.predicate.Parameters[0], ex));

				if (param != null)
				{
					if (param.Type != info.Parameter!.Type)
						param = Expression.Parameter(info.Parameter.Type, param.Name);

					if (info.ExpressionsToReplace != null && info.ExpressionsToReplace.Count > 0)
					{
						var ctx = new { p = info.Parameter, param };
						foreach (var path in info.ExpressionsToReplace)
						{
							path.Path = path.Path.Transform(ctx, static (context, e) => e == context.p ? context.param : e);
							path.Expr = path.Expr.Transform(ctx, static (context, e) => e == context.p ? context.param : e);
						}
					}
				}

				info.Parameter = param;

				return info;
			}

			return null;
		}
	}
}
