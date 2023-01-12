using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlAnchor : ISqlExpression
	{
		public enum AnchorKindEnum
		{
			Deleted,
			Inserted
		}

		public AnchorKindEnum AnchorKind { get; }
		public ISqlExpression SqlExpression { get; private set; }

		public SqlAnchor(ISqlExpression sqlExpression, AnchorKindEnum anchorKind)
		{
			SqlExpression = sqlExpression;
			AnchorKind    = anchorKind;
		}

		#region Overrides

//#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>()).ToString();
		}

//#endif

		#endregion

		#region ISqlExpression Members

		public bool CanBeNullable(NullabilityContext nullability) => true;

		public bool         CanBeNull => true;

		public bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlAnchor anchor)
				return false;

			return AnchorKind == anchor.AnchorKind && SqlExpression.Equals(anchor.SqlExpression);
		}

		public int   Precedence => SqlQuery.Precedence.Primary;
		public Type? SystemType => SqlExpression.SystemType;

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			SqlExpression = func(context, SqlExpression);
			return func(context, this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			return this == other;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SqlAnchor;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			sb.Append('$')
				.Append(AnchorKind.ToString())
				.Append("$.");

			SqlExpression.ToString(sb, dic);
				
			return sb;
		}

		#endregion
	}

}
