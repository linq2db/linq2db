using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	public readonly struct TransformInfo
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

		public readonly Expression Expression;
		public readonly bool       Stop;
		public readonly bool       Continue;
	}
}
