using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlNullabilityExpression : ISqlExpression
	{
		public ISqlExpression SqlExpression { get; private set; }

		public SqlNullabilityExpression(ISqlExpression sqlExpression)
		{
			SqlExpression = sqlExpression;
		}

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			SqlExpression = SqlExpression.Walk(options, context, func) ?? throw new InvalidOperationException();
			return func(context, this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		internal static Func<ISqlExpression,ISqlExpression,bool> DefaultComparer = (x, y) => true;

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			if (other == null)
				return false;

			return SqlExpression.Equals(other, DefaultComparer);
		}

		#endregion

		#region ISqlExpression Members

		public bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (ElementType != other.ElementType)
				return false;

			return SqlExpression.Equals(((SqlNullabilityExpression)other).SqlExpression, comparer);
		}

		public bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public bool  CanBeNull  => true;
		public int   Precedence => SqlExpression.Precedence;
		public Type? SystemType => SqlExpression.SystemType;

		public override int GetHashCode()
		{
			return SqlExpression.GetHashCode();
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SqlNullabilityExpression;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append('(');
			SqlExpression.ToString(sb, dic);
			sb.Append(")?");
			return sb;
		}

		#endregion
	}
}
