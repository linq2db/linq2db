using System;
using System.Collections.Generic;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.SqlQuery
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

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlKeepClause(this);

		public void Modify(List<SqlWindowOrderItem> orderBy)
		{
			OrderBy = orderBy;
		}
	}
}
