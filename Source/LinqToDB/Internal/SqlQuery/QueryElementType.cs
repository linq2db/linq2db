namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// SQL AST node types.
	/// </summary>
	public enum QueryElementType
	{
		SqlField,
		SqlFunction,
		SqlParameter,
		SqlExpression,
		SqlNullabilityExpression,
		SqlAnchor,
		SqlObjectExpression,
		SqlBinaryExpression,
		SqlValue,
		SqlDataType,
		SqlTable,
		SqlAliasPlaceholder,
		SqlRow,

		NotPredicate,
		TruePredicate, 
		FalsePredicate,
		ExprPredicate,
		ExprExprPredicate,
		LikePredicate,
		SearchStringPredicate,
		BetweenPredicate,
		IsNullPredicate,
		IsDistinctPredicate,
		IsTruePredicate,
		InSubQueryPredicate,
		InListPredicate,
		ExistsPredicate,

		SqlQuery,
			Column,
			SearchCondition,
			TableSource,
				JoinedTable,

			SelectClause,
			InsertClause,
			UpdateClause,
				SetExpression,
			FromClause,
			WhereClause,
			HavingClause,
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
		MultiInsertStatement,
			ConditionalInsertClause,

		CreateTableStatement,
		DropTableStatement,
		TruncateTableStatement,

		SqlTableLikeSource,
		MergeOperationClause,

		GroupingSet,

		Comment,

		SqlExtension,

		/// <summary>
		/// ISqlExpression used in LINQ query directly
		/// </summary>
		SqlInlinedExpression,

		/// <summary>
		/// IToSqlConverter used in LINQ query directly
		/// </summary>
		SqlInlinedToSqlExpression,

		/// <summary>
		/// Custom query extensions, e.g. hints, applied to specific query fragment.
		/// Implemented by <see cref="SqlQuery.SqlQueryExtension"/>.
		/// </summary>
		SqlQueryExtension,

		SqlCast,
		SqlCoalesce,
		SqlCondition,
		SqlCase,
		SqlSimpleCase,
		CompareTo,
	}
}
