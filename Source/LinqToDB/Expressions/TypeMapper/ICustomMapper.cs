using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	internal interface ICustomMapper
	{
		bool CanMap(Expression expression);
		Expression Map(Expression expression);
	}
}
