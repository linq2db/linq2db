using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class UnionBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.UnionMethod };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelParser builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var b1 = new ParseBuildInfo();
			var b2 = new ParseBuildInfo();
			builder.BuildSequence(b1, methodCallExpression.Arguments[0]);
			builder.BuildSequence(b2, methodCallExpression.Arguments[1]);
			var union = new UnionClause(methodCallExpression.Arguments[0].Type.GetGenericArguments()[0], "", b1.Sequence, b2.Sequence);
			parseBuildInfo.Sequence.AddClause(union);
			return parseBuildInfo.Sequence;
		}
	}
}
