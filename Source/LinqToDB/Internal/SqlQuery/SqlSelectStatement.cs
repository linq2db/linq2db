using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlSelectStatement : SqlStatementWithQueryBase
	{
		public SqlSelectStatement(SelectQuery? selectQuery) : base(selectQuery)
		{
		}

		public SqlSelectStatement() : base(null)
		{
		}

		public override QueryType          QueryType  => QueryType.Select;
		public override QueryElementType   ElementType => QueryElementType.SelectStatement;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.AppendTag(Tag);

			if (With?.Clauses.Count > 0)
			{
				writer
					.AppendElement(With)
					.AppendLine("--------------------------");
			}

			return writer.AppendElement(SelectQuery);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlSelectStatement(this);
	}
}
