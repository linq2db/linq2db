using System;
using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	class ContextRefBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		static ProjectFlags GetRootProjectFlags(BuildInfo buildInfo)
		{
			return buildInfo.IsAggregation ? ProjectFlags.AggregationRoot : ProjectFlags.Root;
		}

		Expression CalcBuildContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			if (buildInfo.Expression is ContextRefExpression)
				return buildInfo.Expression;

			var root = builder.MakeExpression(buildInfo.Expression, GetRootProjectFlags(buildInfo));
			if (ExpressionEqualityComparer.Instance.Equals(root, buildInfo.Expression))
			{
				if (root is ContextRefExpression)
					return root;

				var newExpression = builder.MakeExpression(root, buildInfo.GetFlags());

				return newExpression;
			}

			return root;
		}

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var root = CalcBuildContext(builder, buildInfo);

			if (!ReferenceEquals(root, buildInfo.Expression))
				return builder.IsSequence(new BuildInfo(buildInfo, root));

			return root is ContextRefExpression;
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var root = CalcBuildContext(builder, buildInfo);

			if (root is not ContextRefExpression contextRef)
				return builder.BuildSequence(new BuildInfo(buildInfo, root));

			var context = contextRef.BuildContext;

			if (!buildInfo.CreateSubQuery)
			return context;

			var elementContext = context.GetContext(buildInfo.Expression, 0, buildInfo);
			if (elementContext != null)
				return elementContext;

			return context;
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var root = CalcBuildContext(builder, buildInfo);

			if (root is not ContextRefExpression contextRef)
				return builder.IsSequence(new BuildInfo(buildInfo, root));

			return true;
		}
	}
}
