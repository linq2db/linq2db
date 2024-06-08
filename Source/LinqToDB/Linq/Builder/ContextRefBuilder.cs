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

			return true;
		}

		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var contextRef = (ContextRefExpression)buildInfo.Expression;

			var context = contextRef.BuildContext;

			if (!buildInfo.CreateSubQuery)
				return BuildSequenceResult.FromContext(context);

			var elementContext = context.GetContext(buildInfo.Expression, buildInfo);

			if (elementContext != null)
				return BuildSequenceResult.FromContext(elementContext);

			return BuildSequenceResult.NotSupported();
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}
	}
}
