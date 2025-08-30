using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions.Types
{
	public interface ICustomMapper
	{
		bool CanMap(Expression expression);
		Expression Map(TypeMapper mapper, Expression expression);
	}
}
