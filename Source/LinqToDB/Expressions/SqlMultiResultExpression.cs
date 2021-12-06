using System;
using System.Linq.Expressions;
using LinqToDB.Linq.Builder;

namespace LinqToDB.Expressions
{
	class SqlMultiResultExpression: Expression
	{
		public IBuildContext BuildContext { get; }
		public Expression    Path         { get; }

		public SqlMultiResultExpression(IBuildContext buildContext, Expression path)
		{
			BuildContext = buildContext;
			Path              = path;
		}

		public override string ToString()
		{
			return $"Multi({BuildContextDebuggingHelper.GetContextInfo(BuildContext)})";
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => Path.Type;
	}
}
