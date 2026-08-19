using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlQuery
{
	public class SqlFrameClause : QueryElement
	{
		public enum FrameTypeKind
		{
			Rows,
			Range,
			Groups,
		}

		public enum FrameExclusionKind
		{
			None,
			CurrentRow,
			Group,
			Ties,
		}

		public SqlFrameClause(FrameTypeKind frameType, SqlFrameBoundary start, SqlFrameBoundary end, FrameExclusionKind exclusion = FrameExclusionKind.None)
		{
			FrameType = frameType;
			Start     = start;
			End       = end;
			Exclusion = exclusion;
		}

		public FrameTypeKind      FrameType { get; }
		public SqlFrameBoundary   Start     { get; private set; }
		public SqlFrameBoundary   End       { get; private set; }
		public FrameExclusionKind Exclusion { get; }

		public override QueryElementType ElementType => QueryElementType.SqlFrameClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.Append(FrameType).Append(" BETWEEN ");
			Start.ToString(writer);
			writer.Append(" AND ");
			End.ToString(writer);

			switch (Exclusion)
			{
				case FrameExclusionKind.CurrentRow:
					writer.Append(" EXCLUDE CURRENT ROW");
					break;
				case FrameExclusionKind.Group:
					writer.Append(" EXCLUDE GROUP");
					break;
				case FrameExclusionKind.Ties:
					writer.Append(" EXCLUDE TIES");
					break;
			}

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(FrameType);
			hash.Add(Start.GetElementHashCode());
			hash.Add(End.GetElementHashCode());
			hash.Add(Exclusion);

			return hash.ToHashCode();
		}

		protected bool Equals(SqlFrameClause other)
		{
			// SqlFrameBoundary has no object-level structural Equals; its GetElementHashCode is the structural
			// fingerprint and is what GetElementHashCode/GetHashCode key on — compare it to stay consistent.
			return FrameType == other.FrameType
				&& Exclusion == other.Exclusion
				&& Start.GetElementHashCode() == other.Start.GetElementHashCode()
				&& End.GetElementHashCode() == other.End.GetElementHashCode();
		}

		public override bool Equals([NotNullWhen(true)] object? obj)
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
			return GetElementHashCode();
		}

		public bool Equals(SqlFrameClause other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return FrameType == other.FrameType
				&& Exclusion == other.Exclusion
				&& Start.Equals(other.Start, comparer)
				&& End.Equals(other.End, comparer);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlFrameClause(this);

		public void Modify(SqlFrameBoundary start, SqlFrameBoundary end)
		{
			Start = start;
			End   = end;
		}
	}
}
