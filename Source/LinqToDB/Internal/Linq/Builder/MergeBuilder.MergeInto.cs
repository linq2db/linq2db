using System;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

namespace LinqToDB.Internal.Linq.Builder
{
	internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.MergeInto))]
		internal sealed class MergeInto : MethodCallBuilder
		{
			static readonly MethodInfo[] _supportedMethods = { MergeIntoMethodInfo1, MergeIntoMethodInfo2 };

			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsSameGenericMethod(_supportedMethods);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// MergeInto<TTarget, TSource>(IQueryable<TSource> source, ITable<TTarget> target, string hint)
				var sourceContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQuery()));
				var target        = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1]) { AssociationsAsSubQueries = true });

				var targetTable = GetTargetTable(target);
				if (targetTable == null)
					throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() on the parameter before passing into .MergeInto().");

				var merge = new SqlMergeStatement(targetTable);
				if (methodCall.Arguments.Count == 3)
					merge.Hint = builder.EvaluateExpression<string>(methodCall.Arguments[2]);

				target.SetAlias(merge.Target.Alias!);

				var genericArguments = methodCall.Method.GetGenericArguments();

				var source = new TableLikeQueryContext(sourceContext.TranslationModifier, new ContextRefExpression(genericArguments[0], target, "t"),
					new ContextRefExpression(genericArguments[1], sourceContext, "s"));

				return BuildSequenceResult.FromContext(new MergeContext(sourceContext.TranslationModifier, merge, target, source));
			}
		}
	}
}
