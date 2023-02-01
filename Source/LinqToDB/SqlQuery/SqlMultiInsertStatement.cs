using System;

namespace LinqToDB.SqlQuery
{
	public class SqlMultiInsertStatement : SqlStatement
	{
		public SqlTableLikeSource               Source     { get; private  set; }
		public List<SqlConditionalInsertClause> Inserts    { get; private  set; }
		public MultiInsertType                  InsertType { get; internal set; }

		public SqlMultiInsertStatement(SqlTableLikeSource source)
		{
			Source = source;
			Inserts = new List<SqlConditionalInsertClause>();
		}

		internal SqlMultiInsertStatement(MultiInsertType type, SqlTableLikeSource source, List<SqlConditionalInsertClause> inserts)
		{
			InsertType = type;
			Source     = source;
			Inserts    = inserts;
		}

		public void Add(SqlSearchCondition? when, SqlInsertClause insert)
			=> Inserts.Add(new SqlConditionalInsertClause(insert, when));

		public void Modify(SqlTableLikeSource source, List<SqlConditionalInsertClause> inserts)
		{
			Source  = source;
			Inserts = inserts;
		}

		public override QueryType          QueryType   => QueryType.MultiInsert;
		public override QueryElementType   ElementType => QueryElementType.MultiInsertStatement;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.AppendLine(InsertType == MultiInsertType.First ? "INSERT FIRST " : "INSERT ALL ");

			foreach (var insert in Inserts)
				writer.AppendElement(insert);

			writer.AppendElement(Source);

			return writer;
		}

		public override ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			Source.Walk(options, context, func);

			foreach (var insert in Inserts)
				((ISqlExpressionWalkable)insert).Walk(options, context, func);

			return base.Walk(options, context, func);
		}

		public override bool IsParameterDependent
		{
			get => Source.IsParameterDependent;
			set => Source.IsParameterDependent = value;
		}

		public override SelectQuery? SelectQuery
		{
			get => null;
			set => throw new InvalidOperationException();
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table)
		{
			if (Source == table)
				return Source;

			foreach (var insert in Inserts)
			{
				if (insert.Insert.Into == table)
					return table;
			}

			return null;
		}

	}
}
