using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	using Linq.Builder;

	class ContextConstructionExpression : Expression, IEquatable<ContextConstructionExpression>
	{
		public ContextConstructionExpression(IBuildContext buildContext, Expression innerExpression)
		{
			BuildContext    = buildContext;
			InnerExpression = innerExpression;
		}
		 
		public IBuildContext           BuildContext    { get; private set; }
		public Expression              InnerExpression { get; private set; }

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => InnerExpression.Type;
		public override bool           CanReduce => true;
		public override Expression     Reduce()  => InnerExpression;

		public override string ToString()
		{
			return $"Ctx({BuildContextDebuggingHelper.GetContextInfo(BuildContext)}): {InnerExpression}";
		}

		public Expression Update(IBuildContext buildContext, Expression inner)
		{
			if (buildContext != BuildContext || inner != InnerExpression)
			{
				return new ContextConstructionExpression(buildContext, inner);
			}

			return this;
		}

		public bool Equals(ContextConstructionExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return BuildContext.Equals(other.BuildContext) && ExpressionEqualityComparer.Instance.Equals(InnerExpression, other.InnerExpression);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((ContextConstructionExpression)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (BuildContext.GetHashCode() * 397) ^  ExpressionEqualityComparer.Instance.GetHashCode(InnerExpression);
			}
		}

		public static bool operator ==(ContextConstructionExpression? left, ContextConstructionExpression? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(ContextConstructionExpression? left, ContextConstructionExpression? right)
		{
			return !Equals(left, right);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitContextConstructionExpression(this);
			return base.Accept(visitor);
		}
	}
}
