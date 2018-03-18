using System;

namespace LinqToDB.SqlQuery
{
	public enum QueryElementType
	{
		SqlField,
		SqlFunction,
		SqlParameter,
		SqlExpression,
		SqlBinaryExpression,
		SqlValue,
		SqlDataType,
		SqlTable,

		ExprPredicate,
		NotExprPredicate,
		ExprExprPredicate,
		LikePredicate,
		BetweenPredicate,
		IsNullPredicate,
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
			Union,

		WithClause,
		CteClause,
		SqlCteTable,

		OutputClause,

		SelectStatement,
		InsertStatement,
		InsertOrUpdateStatement,
		UpdateStatement,
		DeleteStatement,

		CreateTableStatement,
		DropTableStatement,
		TruncateTableStatement,
	}
}
