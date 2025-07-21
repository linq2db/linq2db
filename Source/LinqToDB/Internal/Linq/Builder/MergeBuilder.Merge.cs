using System;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

using static LinqToDB.Internal.Reflection.Methods.LinqToDB.Merge;

namespace LinqToDB.Internal.Linq.Builder
{
	internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.Merge))]
		internal sealed class Merge : MethodCallBuilder
		{
			static readonly MethodInfo[] _supportedMethods = { MergeMethodInfo1, MergeMethodInfo2 };

			public static bool CanBuildMethod(MethodCallExpression call)
				=> call.IsSameGenericMethod(_supportedMethods);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// Merge(ITable<TTarget> target, string hint)

				var disableFilters = methodCall.Arguments[0] is not MethodCallExpression mc || mc.Method.Name != nameof(LinqExtensions.AsCte);
				if (disableFilters)
					builder.PushDisabledQueryFilters([]);

				var target = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQuery()) { AssociationsAsSubQueries = true });

				if (disableFilters)
					builder.PopDisabledFilter();

				var targetTable = GetTargetTable(target);

				if (targetTable == null)
					throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() before calling .Merge()");

				var merge = new SqlMergeStatement(targetTable);
				if (methodCall.Arguments.Count == 2)
					merge.Hint = builder.EvaluateExpression<string>(methodCall.Arguments[1]);

				target.SetAlias(merge.Target.Alias!);

				return BuildSequenceResult.FromContext(new MergeContext(merge, target));
			}
		}
	}
}
