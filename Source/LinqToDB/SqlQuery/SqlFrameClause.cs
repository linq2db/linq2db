using System;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.SqlQuery
{
	public class SqlFrameClause : QueryElement
	{
		public enum FrameTypeKind
		{
			Rows,
			Range,
			Groups,
		}

		public SqlFrameClause(FrameTypeKind frameType, SqlFrameBoundary start, SqlFrameBoundary end)
		{
			FrameType = frameType;
			Start     = start;
			End       = end;
		}

		public FrameTypeKind    FrameType { get; }
		public SqlFrameBoundary Start     { get; private set; }
		public SqlFrameBoundary End       { get; private set; }

		public override QueryElementType ElementType => QueryElementType.SqlFrameClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.Append(FrameType).Append(" BETWEEN ");
			Start.ToString(writer);
			writer.Append(" AND ");
			End.ToString(writer);
			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(FrameType);
			hash.Add(Start.GetElementHashCode());
			hash.Add(End.GetElementHashCode());

			return hash.ToHashCode();
		}

		protected bool Equals(SqlFrameClause other)
		{
			return FrameType == other.FrameType && Start.Equals(other.Start) && End.Equals(other.End);
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)
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

			return Equals((SqlFrameClause)obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FrameType, Start, End);
		}

		public void Modify(SqlFrameBoundary start, SqlFrameBoundary end)
		{
			Start = start;
			End   = end;
		}
	}
}
