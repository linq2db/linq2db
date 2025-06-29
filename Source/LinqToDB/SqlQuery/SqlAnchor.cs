using System;

namespace LinqToDB.SqlQuery
{
	public class SqlAnchor : SqlExpressionBase
	{
		public enum AnchorKindEnum
		{
			Deleted,
			Inserted,
			TableSource,
			TableName,
			TableAsSelfColumn,
			TableAsSelfColumnOrField,
		}

		public AnchorKindEnum AnchorKind { get; }
		public ISqlExpression SqlExpression { get; private set; }

		public SqlAnchor(ISqlExpression sqlExpression, AnchorKindEnum anchorKind)
		{
			SqlExpression = sqlExpression;
			AnchorKind    = anchorKind;
		}

		public void Modify(ISqlExpression expression)
		{
			SqlExpression = expression;
		}

		public override bool CanBeNullable(NullabilityContext nullability) => true;

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlAnchor anchor)
				return false;

			return AnchorKind == anchor.AnchorKind && SqlExpression.Equals(anchor.SqlExpression);
		}

		public override QueryElementType ElementType => QueryElementType.SqlAnchor;

		public override int   Precedence => SqlQuery.Precedence.Primary;
		public override Type? SystemType => SqlExpression.SystemType;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.Append('$')
				.Append(AnchorKind.ToString())
				.Append("$.")
				.AppendElement(SqlExpression);

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(AnchorKind);
			hash.Add(SqlExpression.GetElementHashCode());

			return hash.ToHashCode();
		}
	}
}
