using System;

namespace LinqToDB.SqlQuery
{
	public static class PredicateExtensions
	{
		public static ISqlPredicate MakeNot(this ISqlPredicate predicate)
		{
			return predicate.MakeNot(true);
		}

		public static ISqlPredicate MakeNot(this ISqlPredicate predicate, bool isNot)
		{
			if (!isNot)
				return predicate;

			if (predicate.CanInvert())
			{
				return predicate.Invert();
			}

			return new SqlPredicate.Not(predicate);
		}

		public static SqlSearchCondition AddOr(this SqlSearchCondition search, Action<SqlSearchCondition> orInitializer)
		{
			var sc = new SqlSearchCondition(true);
			orInitializer(sc);
			return search.Add(sc);
		}

		public static SqlSearchCondition AddAnd(this SqlSearchCondition search, Action<SqlSearchCondition> andInitializer)
		{
			var sc = new SqlSearchCondition(false);
			andInitializer(sc);
			return search.Add(sc);
		}

		public static SqlSearchCondition AddGreater(this SqlSearchCondition search,  ISqlExpression expr1, ISqlExpression expr2, bool compareNullsAsValues)
		{
			return search.Add(new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.Greater, expr2, compareNullsAsValues ? true : null));
		}

		public static SqlSearchCondition AddGreaterOrEqual(this SqlSearchCondition search,  ISqlExpression expr1, ISqlExpression expr2, bool compareNullsAsValues)
		{
			return search.Add(new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.GreaterOrEqual, expr2, compareNullsAsValues ? true : null));
		}
	
		public static SqlSearchCondition AddLessOrEqual(this SqlSearchCondition search,  ISqlExpression expr1, ISqlExpression expr2, bool compareNullsAsValues)
		{
			return search.Add(new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.LessOrEqual, expr2, compareNullsAsValues ? true : null));
		}

		public static SqlSearchCondition AddEqual(this SqlSearchCondition search,  ISqlExpression expr1, ISqlExpression expr2, bool compareNullsAsValues)
		{
			return search.Add(new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.Equal, expr2, compareNullsAsValues ? true : null));
		}

		public static SqlSearchCondition AddNotEqual(this SqlSearchCondition search,  ISqlExpression expr1, ISqlExpression expr2, bool compareNullsAsValues)
		{
			return search.Add(new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.NotEqual, expr2, compareNullsAsValues ? true : null));
		}
	
		public static SqlSearchCondition AddExists(this SqlSearchCondition search, SelectQuery selectQuery)
		{
			return search.Add(new SqlPredicate.FuncLike(SqlFunction.CreateExists(selectQuery)));
		}
	
		public static SqlSearchCondition AddNotExists(this SqlSearchCondition search, SelectQuery selectQuery)
		{
			return search.Add(new SqlPredicate.FuncLike(SqlFunction.CreateExists(selectQuery)).MakeNot());
		}

		public static SqlSearchCondition AddNot(this SqlSearchCondition search, ISqlExpression expression)
		{
			return search.Add(new SqlPredicate.Expr(expression).MakeNot());
		}
	}
}
