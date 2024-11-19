using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	[BuildsMethodCall(nameof(LinqExtensions.TagQuery))]
	sealed class TagQueryBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		private static readonly char[] NewLine = ['\r', '\n'];

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var tag = builder.EvaluateExpression<string>(methodCall.Arguments[1]);

			if (!string.IsNullOrWhiteSpace(tag))
			{
				// here we loose empty lines, but I think they are not so precious
				(builder.Tag ??= new ()).Lines.AddRange(tag!.Split(NewLine, StringSplitOptions.RemoveEmptyEntries));
			}

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
