using System;
using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Parser
{
	public class QuerySourceReferenceExpression : BaseCustomExpression
	{
		public const ExpressionType ExpressionType = (ExpressionType) 200001;

		public IQuerySource QuerySource { get; }

		public QuerySourceReferenceExpression(IQuerySource querySource)
		{
			QuerySource = querySource;
		}

		public override ExpressionType NodeType => ExpressionType;
		public override Type Type => QuerySource.ItemType;

	    public override string ToString ()
	    {
		      return $"({QuerySource.ItemType})" + "[" + (QuerySource.ItemName ?? string.Empty) + "]";
	    }

	    public override void CustomVisit(Action<Expression> func)
	    {
		    func(this);
	    }

	    public override bool CustomVisit(Func<Expression, bool> func)
	    {
		    return func(this);
	    }

	    public override Expression CustomFind(Func<Expression, bool> func)
	    {
			if (func(this))
				return this;
			return null;
	    }

	    public override Expression CustomTransform(Func<Expression, Expression> func)
	    {
		    return func(this);
	    }
	}
}
