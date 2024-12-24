using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.Internals.Linq
{
	public interface IExpressionInfo
	{
		LambdaExpression GetExpression(MappingSchema mappingSchema);
	}
}
