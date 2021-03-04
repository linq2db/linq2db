namespace LinqToDB.SqlQuery
{
	public enum QueryElementType
	{
		SqlField,
		SqlFunction,
		SqlParameter,
		SqlExpression,
		SqlObjectExpression,
		SqlBinaryExpression,
		SqlValue,
		SqlDataType,
		SqlTable,
		SqlAliasPlaceholder,

		ExprPredicate,
		NotExprPredicate,
		ExprExprPredicate,
		LikePredicate,
		SearchStringPredicate,
		BetweenPredicate,
		IsNullPredicate,
		IsTruePredicate,
		InSubQueryPredicate,
		InListPredicate,
		FuncLikePredicate,

		SqlQuery,
			Column,
			SearchCondition,
				Condition,
			TableSource,
				JoinedTable,

			SelectClause,
			InsertClause,
			UpdateClause,
				SetExpression,
			FromClause,
			WhereClause,
			GroupByClause,
			OrderByClause,
				OrderByItem,
			SetOperator,

		WithClause,
		CteClause,
		SqlCteTable,
		SqlRawSqlTable,
		SqlValuesTable,

		OutputClause,

		SelectStatement,
		InsertStatement,
		InsertOrUpdateStatement,
		UpdateStatement,
		DeleteStatement,
		MergeStatement,

		CreateTableStatement,
		DropTableStatement,
		TruncateTableStatement,

		MergeSourceTable,
		MergeOperationClause,

		GroupingSet,

		Tag
	}
}
