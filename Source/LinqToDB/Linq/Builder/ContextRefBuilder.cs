using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	class ContextRefBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var root = builder.MakeExpression(buildInfo.Expression, ProjectFlags.Root);
			if (root == buildInfo.Expression)
			{
				if (root is ContextRefExpression)
					return true;
				return false;
			}

			return builder.IsSequence(new BuildInfo(buildInfo, root));
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var root = builder.MakeExpression(buildInfo.Expression, ProjectFlags.Root);
			if (root != buildInfo.Expression)
				return builder.BuildSequence(new BuildInfo(buildInfo, root));

			var context = ((ContextRefExpression)buildInfo.Expression).BuildContext;

			if (buildInfo.IsSubQuery)
			{
				var elementContext = context.GetContext(buildInfo.Expression, 0, buildInfo);
				if (elementContext != null)
					return elementContext;
			}

			return context;
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var root = builder.MakeExpression(buildInfo.Expression, ProjectFlags.Root);
			if (root != buildInfo.Expression)
				return builder.IsSequence(new BuildInfo(buildInfo, root));

			if (buildInfo.Expression is not ContextRefExpression contextRef)
				return false;

			if (buildInfo.InAggregation)
				return contextRef.BuildContext is GroupByBuilder.GroupByContext;

			return true;
		}
	}
}
