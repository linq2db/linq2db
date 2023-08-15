using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	sealed class ContextRefBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			if (buildInfo.Expression is not ContextRefExpression contextRef)
				return false;

			using var query = ExpressionBuilder.QueryPool.Allocate();
			var ctx = contextRef.BuildContext.GetContext(buildInfo.Expression,
				new BuildInfo(buildInfo, buildInfo.Expression, query.Value));

			return ctx != null;
		}

		public IBuildContext? BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var contextRef = (ContextRefExpression)buildInfo.Expression;

			var context = contextRef.BuildContext;

			if (!buildInfo.CreateSubQuery)
				return context;

			var elementContext = context.GetContext(buildInfo.Expression, buildInfo);
			if (elementContext != null)
				return elementContext;

			return context;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}
	}
}
