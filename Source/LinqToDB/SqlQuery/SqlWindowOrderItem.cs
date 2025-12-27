using System;
using System.Diagnostics;
using System.Globalization;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.SqlQuery
{
	public class SqlWindowOrderItem : QueryElement
	{
		public SqlWindowOrderItem(ISqlExpression expression, bool isDescending, Sql.NullsPosition nullsPosition)
		{
			Expression    = expression;
			IsDescending  = isDescending;
			NullsPosition = nullsPosition;
		}

		public ISqlExpression    Expression    { get; private set; }
		public bool              IsDescending  { get; }
		public Sql.NullsPosition NullsPosition { get; set; }

		#region Overrides

		public override QueryElementType ElementType => QueryElementType.SqlWindowOrderItem;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			// writer.DebugAppendUniqueId(this);
			writer.AppendElement(Expression);

			if (IsDescending)
				writer.Append(" DESC");

			if (NullsPosition != Sql.NullsPosition.None)
				writer.Append(" NULLS ").Append(NullsPosition.ToString().ToUpper(CultureInfo.InvariantCulture));

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(Expression.GetElementHashCode());
			hash.Add(IsDescending);
			hash.Add(NullsPosition);

			return hash.ToHashCode();
		}

		public void Modify(ISqlExpression expression)
		{
			Expression = expression;
		}

		public override string ToString()
		{
			return this.ToDebugString();
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlWindowOrderItem(this);

		#endregion
	}
}
