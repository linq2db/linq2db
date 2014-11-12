using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	using Mapping;

	public interface IExpressionInfo
	{
		LambdaExpression GetExpression(MappingSchema mappingSchema);
	}
}