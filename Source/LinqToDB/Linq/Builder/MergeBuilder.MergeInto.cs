using LinqToDB.Expressions;
using LinqToDB.SqlQuery;
using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		internal class MergeInto : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.Method.IsGenericMethod
					&& (   MergeIntoMethodInfo1 == methodCall.Method.GetGenericMethodDefinition()
						|| MergeIntoMethodInfo2 == methodCall.Method.GetGenericMethodDefinition());
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// MergeInto<TTarget, TSource>(IQueryable<TSource> source, ITable<TTarget> target, string hint)
				var sourceContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQuery()));
				var target        = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1]) { AssociationsAsSubQueries = true });

				if (target is not TableBuilder.TableContext tableContext
					|| !tableContext.SelectQuery.IsSimple)
				{
					throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() on the parameter before passing into .MergeInto().");
				}

				var targetTable = tableContext.SqlTable;

				var merge = new SqlMergeStatement(targetTable);
				if (methodCall.Arguments.Count == 3)
					merge.Hint = (string?)methodCall.Arguments[2].EvaluateExpression();

				target.SetAlias(merge.Target.Alias!);
				target.Statement = merge;

				var source = new TableLikeQueryContext(sourceContext);

				return new MergeContext(merge, target, source);
			}

			protected override SequenceConvertInfo? Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
			{
				return null;
			}
		}
	}
}
