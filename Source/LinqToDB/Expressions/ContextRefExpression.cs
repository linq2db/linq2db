using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using LinqToDB.Linq.Builder;

namespace LinqToDB.Expressions
{
	class ContextRefExpression : Expression
	{
		public ContextRefExpression(Type elementType, IBuildContext buildContext)
		{
			ElementType = elementType;
			BuildContext = buildContext ?? throw new ArgumentNullException(nameof(buildContext));
		}

		public Type ElementType { get; }
		public IBuildContext BuildContext { get; }

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type Type => ElementType;

		public override string ToString()
		{
			return $"Ref({BuildContextDebuggingHelper.GetContextInfo(BuildContext)})";
		}

		public override bool CanReduce => false;

		#region Equality members

		protected bool Equals(ContextRefExpression other)
		{
			return ElementType == other.ElementType && ReferenceEquals(BuildContext, other.BuildContext);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((ContextRefExpression)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((ElementType != null ? ElementType.GetHashCode() : 0) * 397) ^
				       RuntimeHelpers.GetHashCode(BuildContext);
			}
		}

		#endregion
	}
}
