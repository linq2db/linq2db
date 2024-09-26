using System.Linq.Expressions;

namespace LinqToDB.Expressions.Types
{
	public interface ICustomMapper
	{
		bool CanMap(Expression expression);
		Expression Map(Expression expression);
	}
}
