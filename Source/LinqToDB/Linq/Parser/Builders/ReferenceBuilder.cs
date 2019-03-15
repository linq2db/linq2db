using System.Linq.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class ReferenceBuilder : BaseBuilder
	{
		public override bool CanBuild(ModelTranslator builder, Expression expression)
		{
			return expression.NodeType == QuerySourceReferenceExpression.ExpressionType;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, Expression expression)
		{
			var reference = (QuerySourceReferenceExpression)expression;
			parseBuildInfo.Sequence.AddClause((BaseClause)reference.QuerySource);
			return parseBuildInfo.Sequence;
		}
	}
}
