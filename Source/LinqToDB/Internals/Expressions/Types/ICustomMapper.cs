using System.Linq.Expressions;

namespace LinqToDB.Internals.Expressions.Types
{
	public interface ICustomMapper
	{
		bool CanMap(Expression expression);
		Expression Map(Expression expression);
	}
}
