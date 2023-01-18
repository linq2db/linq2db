using System;

namespace LinqToDB.SqlQuery
{
	public class SqlTruncateTableStatement : SqlStatement
	{
		public SqlTable? Table         { get; set; }
		public bool      ResetIdentity { get; set; }

		public override QueryType          QueryType    => QueryType.TruncateTable;
		public override QueryElementType   ElementType  => QueryElementType.TruncateTableStatement;

		public override bool               IsParameterDependent
		{
			get => false;
			set {}
		}

		public override SelectQuery? SelectQuery { get => null; set {}}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.Append("TRUNCATE TABLE ")
				.AppendElement(Table)
				.AppendLine();

			return writer;
		}

		public override ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			Table = ((ISqlExpressionWalkable?)Table)?.Walk(options, context, func) as SqlTable;
			return base.Walk(options, context, func);
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table)
		{
			return null;
		}
	}
}
