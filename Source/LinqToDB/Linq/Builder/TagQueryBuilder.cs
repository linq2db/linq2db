using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	sealed class TagQueryBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(nameof(LinqExtensions.TagQuery));
		}

		private static readonly char[] NewLine = new[] { '\r', '\n' };

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var tag = methodCall.Arguments[1].EvaluateExpression<string>(builder.DataContext);

			if (!string.IsNullOrWhiteSpace(tag))
			{
				// here we loose empty lines, but I think they are not so precious
				(builder.Tag ??= new ()).Lines.AddRange(tag!.Split(NewLine, StringSplitOptions.RemoveEmptyEntries));
			}

			return sequence;
		}
	}
}
