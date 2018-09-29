using LinqToDB.Expressions;
using LinqToDB.SqlQuery;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder
	{
		internal class Merge : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.Method.IsGenericMethod
					&& LinqExtensions.MergeMethodInfo.GetGenericMethodDefinition() == methodCall.Method.GetGenericMethodDefinition();
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// Merge(ITable<TTarget> target, string hint)
				var target = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQuery()));


				var targetTable = ((TableBuilder.TableContext)target).SqlTable;

				var merge = new SqlMergeStatement(targetTable)
				{
					Hint = (string)methodCall.Arguments[1].EvaluateExpression()
				};

				target.SetAlias(merge.Target.Alias);
				target.Statement = merge;

				return new MergeContext(merge, target);
			}

			protected override SequenceConvertInfo Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
			{
				return null;
			}
		}
	}
}
