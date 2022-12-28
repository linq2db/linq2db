using LinqToDB.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		internal sealed class Using : MethodCallBuilder
		{
			static readonly MethodInfo[] _supportedMethods = {UsingMethodInfo1, UsingMethodInfo2};

			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsSameGenericMethod(_supportedMethods);
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var sourceContext =
					builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

				var genericArgs = methodCall.Method.GetGenericArguments();

				var source = new TableLikeQueryContext(
					new ContextRefExpression(genericArgs[0], mergeContext.TargetContext, "target"),
					new ContextRefExpression(genericArgs[1], sourceContext, "source"));

				mergeContext.Sequences    = new[] { mergeContext.Sequence, source };
				mergeContext.Merge.Source = source.Source;

				return mergeContext;
			}
		}
	}
}
