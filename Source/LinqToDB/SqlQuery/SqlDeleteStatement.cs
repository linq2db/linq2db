using System;

namespace LinqToDB.SqlQuery
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
			var hash = new HashCode();
			hash.Add(base.GetElementHashCode());

			hash.Add(Table?.GetElementHashCode() ?? 0);
			hash.Add(Top?.GetElementHashCode() ?? 0);
			hash.Add(Output?.GetElementHashCode() ?? 0);
			return hash.ToHashCode();
		}
	}
}
