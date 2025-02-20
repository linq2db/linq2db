using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	public struct TransformInfo
	{
		public TransformInfo(Expression expression, bool stop)
		{
			Expression = expression;
			Stop       = stop;
			Continue   = false;
		}

		public TransformInfo(Expression expression)
		{
			Expression = expression;
			Stop       = false;
			Continue   = false;
		}

		public TransformInfo(Expression expression, bool stop, bool @continue)
		{
			Expression = expression;
			Stop       = stop;
			Continue   = @continue;
		}

		public Expression Expression;
		public bool       Stop;
		public bool       Continue;
	}
}
