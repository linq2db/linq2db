using System.Linq.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public abstract class BaseBuilder
	{
		public abstract bool CanBuild(ModelTranslator builder, Expression expression);
		public abstract Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, Expression expression);
	}

}
