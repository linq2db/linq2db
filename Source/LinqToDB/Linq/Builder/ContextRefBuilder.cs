using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	class ContextRefBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return buildInfo.Expression is ContextRefExpression;
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var context = ((ContextRefExpression)buildInfo.Expression).BuildContext;
			if (buildInfo.IsSubQuery && buildInfo.SelectQuery.From.Tables.Count == 0)
			{
				var parentContext = context.Parent;
				if (parentContext != null)
					context = parentContext.GetContext(buildInfo.Expression, 0, buildInfo);
			};

			return context;
		}

		public SequenceConvertInfo Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return buildInfo.Expression is ContextRefExpression;
		}
	}
}
