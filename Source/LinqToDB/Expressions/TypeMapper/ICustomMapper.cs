using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public interface ICustomMapper
	{
		bool CanMap(Expression expression);
		Expression Map(Expression expression);
	}
}
