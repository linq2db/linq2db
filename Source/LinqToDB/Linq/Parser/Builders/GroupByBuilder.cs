using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class GroupByBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.GroupBy };

		public override MethodInfo[] SupportedMethods()
		{
			return _supported;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo,
			MethodCallExpression methodCallExpression)
		{
			throw new System.NotImplementedException();
		}
	}
}
