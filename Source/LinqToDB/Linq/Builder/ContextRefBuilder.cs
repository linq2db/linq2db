using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	[BuildsAny]
	sealed class ContextRefBuilder : ISequenceBuilder
	{
		public static bool CanBuild(BuildInfo info, ExpressionBuilder builder)
		{
			var root = CalcBuildContext(builder, info);

			if (!ReferenceEquals(root, info.Expression))
				return builder.IsSequence(new BuildInfo(info, root) { IsTest = true });

			return root is ContextRefExpression;
		}

		static ProjectFlags GetRootProjectFlags(BuildInfo info)
			=> info.GetFlags(info.IsAggregation ? ProjectFlags.AggregationRoot : ProjectFlags.Root);

		static Expression CalcBuildContext(ExpressionBuilder builder, BuildInfo info)
		{
			if (info.Parent == null || info.Expression is ContextRefExpression)
				return info.Expression;

			var root = builder.MakeExpression(info.Parent, info.Expression, GetRootProjectFlags(info));
			if (ExpressionEqualityComparer.Instance.Equals(root, info.Expression))
			{
				return root is ContextRefExpression
					? root
					: builder.MakeExpression(info.Parent, root, ProjectFlags.Expand);
			}

			return root;
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo info)
		{
			var root = CalcBuildContext(builder, info);

			return root is not ContextRefExpression { BuildContext: var context }
				? builder.BuildSequence(new BuildInfo(info, root))
				: info.CreateSubQuery && context.GetContext(info.Expression, 0, info) is {} exprContext
				? exprContext
				: context;
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo info, ParameterExpression? param)
			=> null;

		public bool IsSequence(ExpressionBuilder builder, BuildInfo info)
		{
			var root = CalcBuildContext(builder, info);

			return root is not ContextRefExpression contextRef
				? builder.IsSequence(new BuildInfo(info, root))
				: contextRef.BuildContext.GetContext(info.Expression, 0, info) != null;
		}
	}
}
