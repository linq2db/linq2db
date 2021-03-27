using System;
using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	class TagQueryBuilder : MethodCallBuilder
	{
		private static readonly char[] NewLine = new[] { '\r', '\n' };

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var tag = (string?)methodCall.Arguments[1].EvaluateExpression();

			if (!string.IsNullOrWhiteSpace(tag))
			{
				// here we loose empty lines, but I think they are not so precious
				(builder.Tag ??= new ()).Lines.AddRange(tag!.Split(NewLine, StringSplitOptions.RemoveEmptyEntries));
			}

			return sequence;
		}

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(nameof(LinqExtensions.TagQuery));
		}

		protected override SequenceConvertInfo? Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}
	}
}
