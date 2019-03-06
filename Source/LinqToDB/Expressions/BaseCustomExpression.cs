using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public abstract class BaseCustomExpression : Expression, ICustomExpression
	{
		public abstract void CustomVisit(Action<Expression> func);
		public abstract bool CustomVisit(Func<Expression, bool> func);
		public abstract Expression CustomFind(Func<Expression, bool> func);
		public abstract Expression CustomTransform(Func<Expression, Expression> func);
		public abstract bool CustomEquals(Expression other);
	}
}
