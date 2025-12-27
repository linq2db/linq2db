using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlDeleteStatement : SqlStatementWithQueryBase
	{
		public SqlDeleteStatement(SelectQuery? selectQuery) : base(selectQuery)
		{
		}

		public SqlDeleteStatement() : this(null)
		{
		}

		public override QueryType        QueryType   => QueryType.Delete;
		public override QueryElementType ElementType => QueryElementType.DeleteStatement;

		public override bool             IsParameterDependent
		{
			get => SelectQuery.IsParameterDependent;
			set => SelectQuery.IsParameterDependent = value;
		}

		public SqlTable?       Table   { get; set; }
		public ISqlExpression? Top     { get; set; }

		public SqlOutputClause? Output { get; set; }

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.AppendTag(Tag)
				.AppendElement(With)
				.Append("DELETE FROM ")
				.AppendElement(Table)
				.AppendLine()
				.AppendElement(SelectQuery)
				.AppendLine()
				.AppendElement(Output);

			return writer;
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(
				base.GetElementHashCode(),
				Table?.GetElementHashCode(),
				Top?.GetElementHashCode(),
				Output?.GetElementHashCode()
			);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlDeleteStatement(this);
	}
}
