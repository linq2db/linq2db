﻿using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Reflection;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.RemoveOrderBy))]
	sealed class RemoveOrderByBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsSameGenericMethod(Methods.LinqToDB.RemoveOrderBy);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (sequence.SelectQuery.Select is { TakeValue: null, SkipValue: null })
				sequence.SelectQuery.OrderBy.Items.Clear();

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
