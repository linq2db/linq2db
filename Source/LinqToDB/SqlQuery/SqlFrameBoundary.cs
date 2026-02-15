using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.SqlQuery
{
	public class SqlFrameBoundary : QueryElement
	{
		public enum FrameBoundaryType
		{
			Unbounded,
			CurrentRow,
			Offset,
		}

		public SqlFrameBoundary(bool isPreceding, FrameBoundaryType boundaryType, ISqlExpression? offset)
		{
			IsPreceding  = isPreceding;
			BoundaryType = boundaryType;
			Offset       = offset;
		}

		public bool              IsPreceding  { get; }
		public FrameBoundaryType BoundaryType { get; }
		public ISqlExpression?   Offset       { get; private set; }

		public override QueryElementType ElementType => QueryElementType.SqlFrameBoundary;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			switch (BoundaryType)
			{
				case FrameBoundaryType.Unbounded:
					writer.Append(IsPreceding ? "UNBOUNDED PRECEDING" : "UNBOUNDED FOLLOWING");
					break;
				case FrameBoundaryType.CurrentRow:
					writer.Append("CURRENT ROW");
					break;
				case FrameBoundaryType.Offset:
					writer.AppendElement(Offset);
					writer.Append(IsPreceding ? " PRECEDING" : " FOLLOWING");
					break;
			}

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(IsPreceding);
			hash.Add(BoundaryType);
			hash.Add(Offset?.GetElementHashCode());

			return hash.ToHashCode();
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlFrameBoundary(this);

		public void Modify(ISqlExpression offset)
		{
			if (BoundaryType != FrameBoundaryType.Offset)
				throw new InvalidOperationException("Cannot modify non-offset boundary");
			Offset = offset;
		}
	}
}
