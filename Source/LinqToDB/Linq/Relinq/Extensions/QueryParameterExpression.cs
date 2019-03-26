using System;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace LinqToDB.Linq.Relinq.Extensions
{
	public class QueryParameterExpression : Expression
	{
		public MemberExpression ParamExpression { get; }
		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type Type => ParamExpression.Type;
		public override bool CanReduce => true;
		public override Expression Reduce() => ParamExpression;

		public QueryParameterExpression([NotNull] MemberExpression paramExpression)
		{
			ParamExpression = paramExpression ?? throw new ArgumentNullException(nameof(paramExpression));
		}

	}
}
