using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class UnionBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.Union };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var sequence1 = builder.BuildSequence(new ParseBuildInfo(), methodCallExpression.Arguments[0]);
			var sequence2 = builder.BuildSequence(new ParseBuildInfo(), methodCallExpression.Arguments[1]);
			var union = new UnionClause(methodCallExpression.Arguments[0].Type.GetGenericArguments()[0], "", sequence1, sequence2);

			parseBuildInfo.Sequence.AddClause(union);
			return parseBuildInfo.Sequence;
		}
	}
}
