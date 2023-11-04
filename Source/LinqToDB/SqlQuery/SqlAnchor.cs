﻿using System;

namespace LinqToDB.SqlQuery
{
	public class SqlAnchor : ISqlExpression
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

		#region Overrides

//#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
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

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			return this == other;
		}

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public QueryElementType ElementType => QueryElementType.SqlAnchor;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			writer.Append('$')
				.Append(AnchorKind.ToString())
				.Append("$.")
				.AppendElement(SqlExpression);

			return writer;
		}

		#endregion
	}

}
