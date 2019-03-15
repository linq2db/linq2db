using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class TakeBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.Take };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var sequence = builder.BuildSequence(new ParseBuildInfo(), methodCallExpression.Arguments[0]);
			parseBuildInfo.Sequence.AddClause(sequence);
			parseBuildInfo.Sequence.AddClause(new TakeClause(builder.ConvertExpression(sequence, methodCallExpression.Arguments[1])));
			return parseBuildInfo.Sequence;
		}
	}
}
