using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	internal interface ICustomMapper
	{
		Expression Map(Expression expression);
	}
}
