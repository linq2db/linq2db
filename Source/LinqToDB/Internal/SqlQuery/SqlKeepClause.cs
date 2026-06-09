using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlQuery
{
	public class SqlKeepClause : QueryElement
	{
		public enum KeepType
		{
			First,
			Last,
		}

		public SqlKeepClause(KeepType type, List<SqlWindowOrderItem> orderBy)
		{
			Type    = type;
			OrderBy = orderBy;
		}

		public KeepType                 Type    { get; }
		public List<SqlWindowOrderItem> OrderBy { get; private set; }

		public override QueryElementType ElementType => QueryElementType.SqlKeepClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.Append("KEEP (DENSE_RANK ").Append(Type == KeepType.First ? "FIRST" : "LAST").Append(" ORDER BY ");
			for (var i = 0; i < OrderBy.Count; i++)
			{
				if (i > 0)
					writer.Append(", ");
				OrderBy[i].ToString(writer);
			}

			writer.Append(')');
			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(Type);

			foreach (var item in OrderBy)
				hash.Add(item.GetElementHashCode());

			return hash.ToHashCode();
		}

		protected bool Equals(SqlKeepClause other)
		{
			if (Type != other.Type || OrderBy.Count != other.OrderBy.Count)
				return false;

			// SqlWindowOrderItem has no structural Equals; its GetElementHashCode is the structural
			// fingerprint (expression + direction + nulls), and is what the parent SqlExtendedFunction
			// hash already keys on — so comparing it keeps Equals consistent with GetElementHashCode.
			for (var i = 0; i < OrderBy.Count; i++)
			{
				if (OrderBy[i].GetElementHashCode() != other.OrderBy[i].GetElementHashCode())
					return false;
			}

			return true;
		}

		public override bool Equals([NotNullWhen(true)] object? obj)
		{
			if (obj is null)
				return false;

			if (ReferenceEquals(this, obj))
				return true;

			if (obj.GetType() != GetType())
				return false;

			return Equals((SqlKeepClause)obj);
		}

		public override int GetHashCode()
		{
			return GetElementHashCode();
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlKeepClause(this);

		public void Modify(List<SqlWindowOrderItem> orderBy)
		{
			OrderBy = orderBy;
		}
	}
}
