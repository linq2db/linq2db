using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		internal class Merge : MethodCallBuilder
		{
			static readonly MethodInfo[] _supportedMethods = {MergeMethodInfo1, MergeMethodInfo2};

			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsSameGenericMethod(_supportedMethods);
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// Merge(ITable<TTarget> target, string hint)
				var target = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQuery()) { AssociationsAsSubQueries = true });

				if (target is not TableBuilder.TableContext tableContext
					|| !tableContext.SelectQuery.IsSimple)
				{
					return ThrowHelper.ThrowNotImplementedException<IBuildContext>(
						"Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() before calling .Merge()");
				}

				var targetTable = tableContext.SqlTable;

				var merge = new SqlMergeStatement(targetTable);
				if (methodCall.Arguments.Count == 2)
					merge.Hint = (string?)methodCall.Arguments[1].EvaluateExpression();

				target.SetAlias(merge.Target.Alias!);
				target.Statement = merge;

				return new MergeContext(merge, target);
			}
		}
	}
}
