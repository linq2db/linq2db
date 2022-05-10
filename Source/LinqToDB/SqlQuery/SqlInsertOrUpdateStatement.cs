using System.Text;

namespace LinqToDB.SqlQuery;

public class SqlInsertOrUpdateStatement: SqlStatementWithQueryBase
{
	public override QueryType QueryType          => QueryType.InsertOrUpdate;
	public override QueryElementType ElementType => QueryElementType.InsertOrUpdateStatement;

	private SqlInsertClause? _insert;
	public  SqlInsertClause   Insert
	{
		get => _insert ??= new SqlInsertClause();
		set => _insert = value;
	}

	private SqlUpdateClause? _update;
	public  SqlUpdateClause   Update
	{
		get => _update ??= new SqlUpdateClause();
		set => _update = value;
	}

	internal bool HasInsert => _insert != null;
	internal bool HasUpdate => _update != null;

	public SqlInsertOrUpdateStatement(SelectQuery? selectQuery) : base(selectQuery)
	{
	}

	public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
	{
		((IQueryElement)Insert).ToString(sb, dic);
		((IQueryElement)Update).ToString(sb, dic);
		return sb;
	}

	public override ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
	{
		With?.Walk(options, context, func);
		((ISqlExpressionWalkable?)_insert)?.Walk(options, context, func);
		((ISqlExpressionWalkable?)_update)?.Walk(options, context, func);

		SelectQuery = (SelectQuery)SelectQuery.Walk(options, context, func);

		return base.Walk(options, context, func);
	}

	public override ISqlTableSource? GetTableSource(ISqlTableSource table)
	{
		if (_update?.Table == table)
			return table;
		if (_insert?.Into == table)
			return table;

		return SelectQuery!.GetTableSource(table);
	}
}
