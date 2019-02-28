using System.Linq.Expressions;

namespace LinqToDB.Linq.Parser.Builders
{
	public abstract class BaseBuilder
	{
		public abstract bool CanBuild(Expression expression);
		public abstract Sequence BuildSequence(ModelParser builder, ParseBuildInfo parseBuildInfo, Expression expression);
	}

}
