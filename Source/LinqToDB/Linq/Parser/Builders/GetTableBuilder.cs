using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class GetTableBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.GetTable };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, MethodCallExpression methodCallExpression)
		{
			var sequence = new TableSource(methodCallExpression.Method.GetGenericArguments()[0], "");
			builder.RegisterSource(sequence);
			parseBuildInfo.Sequence.AddClause(sequence);
			return parseBuildInfo.Sequence;
		}
	}
}
