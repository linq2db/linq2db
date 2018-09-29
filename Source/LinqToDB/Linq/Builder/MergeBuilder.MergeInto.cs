using LinqToDB.Expressions;
using LinqToDB.SqlQuery;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder
	{
		internal class MergeInto : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.Method.IsGenericMethod
					&& LinqExtensions.MergeIntoMethodInfo.GetGenericMethodDefinition() == methodCall.Method.GetGenericMethodDefinition();
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// MergeInto<TTarget, TSource>(IQueryable<TSource> source, ITable<TTarget> target, string hint)
				var source = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQuery()));
				var target = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1]));

				var targetTable = ((TableBuilder.TableContext)target).SqlTable;

				var merge = new SqlMergeStatement(targetTable)
				{
					Hint = (string)methodCall.Arguments[2].EvaluateExpression()
				};

				target.SetAlias(merge.Target.Alias);
				target.Statement = merge;

				target.Statement = merge;

				source = new MergeSourceQueryContext(source, merge.SourceFields);
				source.SetAlias("Source");
				return new MergeContext(merge, target, source);
			}

			protected override SequenceConvertInfo Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
			{
				return null;
			}
		}
	}
}
