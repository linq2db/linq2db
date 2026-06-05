using System;
using System.Diagnostics;
using System.Globalization;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlOrderByItem : QueryElement
	{
		public SqlOrderByItem(ISqlExpression expression, bool isDescending, bool isPositioned)
			: this(expression, isDescending, isPositioned, Sql.NullsPosition.None)
		{
		}

		public SqlOrderByItem(ISqlExpression expression, bool isDescending, bool isPositioned, Sql.NullsPosition nullsPosition)
		{
			Expression    = expression;
			IsDescending  = isDescending;
			IsPositioned  = isPositioned;
			NullsPosition = nullsPosition;
		}

		public ISqlExpression    Expression    { get; internal set; }
		public bool              IsDescending  { get; }
		public bool              IsPositioned  { get; }
		public Sql.NullsPosition NullsPosition { get; }

		#region Overrides

		public override QueryElementType ElementType => QueryElementType.OrderByItem;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.AppendElement(Expression);

			if (IsPositioned)
				writer.Append(":by_index");

			if (IsDescending)
				writer.Append(" DESC");

			if (NullsPosition != Sql.NullsPosition.None)
				writer.Append(" NULLS ").Append(NullsPosition.ToString().ToUpper(CultureInfo.InvariantCulture));

			return writer;
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(
				ElementType,
				Expression.GetElementHashCode(),
				IsDescending,
				IsPositioned,
				NullsPosition
			);
		}

		public override string ToString()
		{
			return this.ToDebugString();
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlOrderByItem(this);

		#endregion
	}
}
