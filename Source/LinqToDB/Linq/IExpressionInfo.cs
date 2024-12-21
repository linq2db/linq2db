using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.Linq
{
	public interface IExpressionInfo
	{
		LambdaExpression GetExpression(MappingSchema mappingSchema);
	}
}
