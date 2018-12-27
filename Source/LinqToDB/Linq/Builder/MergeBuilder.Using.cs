using LinqToDB.Expressions;
using LinqToDB.SqlQuery;
using System;
using System.Collections;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder
	{
		internal class Using : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.Method.IsGenericMethod
					&& (LinqExtensions.UsingMethodInfo1            .GetGenericMethodDefinition() == methodCall.Method.GetGenericMethodDefinition()
					 || LinqExtensions.UsingMethodInfo2            .GetGenericMethodDefinition() == methodCall.Method.GetGenericMethodDefinition());
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				if (LinqExtensions.UsingMethodInfo1.GetGenericMethodDefinition() == methodCall.Method.GetGenericMethodDefinition())
				{
					var sourceContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));
					var source = new MergeSourceQueryContext(
						builder,
						new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()),
						mergeContext.Merge,
						sourceContext,
						methodCall.Method.GetGenericArguments()[1]);
					mergeContext.Sequences = new IBuildContext[] { mergeContext.Sequence, source };
				}
				else
				{
					throw new NotImplementedException("TODO: Enumerable Source");
					//var source = (IEnumerable)methodCall.Arguments[1].EvaluateExpression();
					//mergeContext.Merge.Source.SourceEnumerable = source;
					//mergeContext.Sequences = new IBuildContext[] { mergeContext.Sequence, new EnumerableContext(source) };
				}

				return mergeContext;
			}

			protected override SequenceConvertInfo Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
			{
				return null;
			}
		}
	}
}
