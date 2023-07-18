using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;

	sealed class ContextRefBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			if (buildInfo.Expression is not ContextRefExpression contextRef)
				return false;

			if (contextRef.Type.IsEnumerableType(contextRef.BuildContext.ElementType) || typeof(IQueryable<>).IsSameOrParentOf(contextRef.Type))
			{
				using var query = ExpressionBuilder.QueryPool.Allocate();
				var ctx = contextRef.BuildContext.GetContext(buildInfo.Expression, new BuildInfo(buildInfo, buildInfo.Expression, query.Value));
				return ctx != null;
			}

			return false;
		}

		public IBuildContext? BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var contextRef = (ContextRefExpression)buildInfo.Expression;

			var context = contextRef.BuildContext;

			if (!buildInfo.CreateSubQuery)
				return context;

			if (contextRef.Type.IsEnumerableType(contextRef.BuildContext.ElementType))
			{
				var elementContext = context.GetContext(buildInfo.Expression, buildInfo);
				if (elementContext != null)
					return elementContext;
			}

			return context;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

		public Expression Expand(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			throw new NotImplementedException();
		}
	}
}
