using System.Linq.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class ArrayBuilder : BaseBuilder
	{
		public override bool CanBuild(Expression expression)
		{
			return expression.NodeType == ExpressionType.NewArrayInit;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, Expression expression)
		{
			var newArray = (NewArrayExpression)expression;
			var sequence = new ArraySource(newArray.Type, "", expression);
			builder.RegisterSource(sequence);
			parseBuildInfo.Sequence.AddClause(sequence);
			return parseBuildInfo.Sequence;
		}
	}
}
