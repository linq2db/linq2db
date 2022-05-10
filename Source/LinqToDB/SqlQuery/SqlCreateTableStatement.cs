using System.Text;

namespace LinqToDB.SqlQuery;

public class SqlCreateTableStatement : SqlStatement
{
	public SqlCreateTableStatement(SqlTable sqlTable)
	{
		Table = sqlTable;
	}

	public SqlTable        Table           { get; private set; }
	public string?         StatementHeader { get; set; }
	public string?         StatementFooter { get; set; }
	public DefaultNullable DefaultNullable { get; set; }

	public override QueryType        QueryType   => QueryType.CreateTable;
	public override QueryElementType ElementType => QueryElementType.CreateTableStatement;

	public override bool             IsParameterDependent
	{
		get => false;
		set {}
	}

	public override SelectQuery? SelectQuery { get => null; set {}}

	public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
	{
		sb.Append("CREATE TABLE ");

		((IQueryElement?)Table)?.ToString(sb, dic);

		sb.AppendLine();

		return sb;
	}

	public override ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
	{
		Table = (SqlTable)((ISqlExpressionWalkable)Table).Walk(options, context, func)!;
		return base.Walk(options, context, func);
	}

	public override ISqlTableSource? GetTableSource(ISqlTableSource table)
	{
		return null;
	}

	public override void WalkQueries<TContext>(TContext context, Func<TContext, SelectQuery, SelectQuery> func)
	{
		if (SelectQuery != null)
		{
			var newQuery = func(context, SelectQuery);
			if (!ReferenceEquals(newQuery, SelectQuery))
				SelectQuery = newQuery;
		}
	}
}
