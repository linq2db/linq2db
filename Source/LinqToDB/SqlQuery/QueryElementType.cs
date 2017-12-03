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

		SelectStatement,
		InsertStatement,
		UpdateStatement,
		DeleteStatement,

		CreateTableStatement,
		DropTableStatement,
	}
}
