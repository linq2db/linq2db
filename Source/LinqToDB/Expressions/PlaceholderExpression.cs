using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public enum PlaceholderType
	{
		Closure,
		Converted,
		FailedToTranslate,
	}

	public class PlaceholderExpression : Expression
	{
		public PlaceholderType PlaceholderType   { get; }
		public Expression      InnerExpression { get; }

		public PlaceholderExpression(Expression innerExpression, PlaceholderType placeholderType)
		{
			PlaceholderType = placeholderType;
			InnerExpression = innerExpression;
		}

		public static PlaceholderExpression Closure(Expression innerExpression) => new(innerExpression, PlaceholderType.Closure);

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => InnerExpression.Type;
		public override bool           CanReduce => true;

		public override Expression     Reduce() => InnerExpression;

		public PlaceholderExpression Update(Expression closureExpression)
		{
			if (ReferenceEquals(InnerExpression, closureExpression))
				return this;
			return new PlaceholderExpression(closureExpression, PlaceholderType);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitPlaceholderExpression(this);
			return base.Accept(visitor);
		}

		public override string ToString()
		{
			return $"$({InnerExpression})";
		}
	}
}
