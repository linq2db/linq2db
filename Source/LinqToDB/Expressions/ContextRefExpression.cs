using System;
using System.Linq.Expressions;

using LinqToDB.Linq.Builder;

namespace LinqToDB.Expressions
{
	sealed class ContextRefExpression : Expression, IEquatable<ContextRefExpression>
	{
		public ContextRefExpression(Type elementType, IBuildContext buildContext, string? alias = null)
		{
			ElementType  = elementType;
			BuildContext = buildContext ?? throw new ArgumentNullException(nameof(buildContext));
			Alias        = alias;
		}

		public Type          ElementType  { get; }
		public IBuildContext BuildContext { get; }
		public string?       Alias        { get; set; }

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type Type => ElementType;

		public override string ToString()
		{
			return $"Ref({BuildContextDebuggingHelper.GetContextInfo(BuildContext)}::{Type.Name})";
		}

		public override bool CanReduce => false;

		public ContextRefExpression WithType(Type type)
		{
			if (type == Type)
				return this;

			return new ContextRefExpression(type, BuildContext);
		}

		public ContextRefExpression WithContext(IBuildContext buildContext)
		{
			if (buildContext == BuildContext)
				return this;

			return new ContextRefExpression(Type, buildContext, Alias);
		}

		public ContextRefExpression WithAlias(string? alias)
		{
			if (Alias == alias)
				return this;

			return new ContextRefExpression(Type, BuildContext, alias);
		}
		
		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitContextRefExpression(this);
			return base.Accept(visitor);
		}

		#region Equality members

		public bool Equals(ContextRefExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return ElementType.Equals(other.ElementType) && BuildContext.Equals(other.BuildContext);
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

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((ContextRefExpression)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = ElementType.GetHashCode();
				hashCode = (hashCode * 397) ^ BuildContext.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator ==(ContextRefExpression? left, ContextRefExpression? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(ContextRefExpression? left, ContextRefExpression? right)
		{
			return !Equals(left, right);
		}

		#endregion
	}
}
