using System.Text;

namespace LinqToDB.SqlQuery;

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

	public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
	{
		sb.Append("TRUNCATE TABLE ");

		((IQueryElement?)Table)?.ToString(sb, dic);

		sb.AppendLine();

		return sb;
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
