using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlConditionalInsertClause : QueryElement
	{
		public SqlInsertClause     Insert { get; private set; }
		public SqlSearchCondition? When   { get; private set; }

		public SqlConditionalInsertClause(SqlInsertClause insert, SqlSearchCondition? when)
		{
			Insert = insert;
			When   = when;
		}

		public void Modify(SqlInsertClause insert, SqlSearchCondition? when)
		{
			Insert = insert;
			When   = when;
		}

		#region IQueryElement

		public override QueryElementType ElementType => QueryElementType.ConditionalInsertClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (When != null)
			{
				writer
					.Append("WHEN ")
					.AppendElement(When)
					.AppendLine(" THEN");
			}

			writer.AppendElement(Insert);

			return writer;
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(
				Insert.GetElementHashCode(),
				When?.GetElementHashCode()
			);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlConditionalInsertClause(this);

		#endregion
	}
}
