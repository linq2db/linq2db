using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class SkipBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.TakeMethod };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelParser builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var sequence = builder.BuildSequence(new ParseBuildInfo(), methodCallExpression.Arguments[0]);
			parseBuildInfo.Sequence.AddClause(sequence);
			parseBuildInfo.Sequence.AddClause(new SkipClause(builder.ConvertExpression(methodCallExpression.Arguments[1])));
			return parseBuildInfo.Sequence;
		}
	}
}
