using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public interface ICustomExpression
	{
		void CustomVisit(Action<Expression> func);
		bool CustomVisit(Func<Expression, bool> func);
		Expression CustomFind(Func<Expression, bool> func);
		Expression CustomTransform(Func<Expression, Expression> func);
		bool CustomEquals(Expression other);
	}
}
