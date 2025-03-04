using System;

namespace LinqToDB.Internal.SqlQuery
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

		public static SqlSearchCondition AddGreater(this SqlSearchCondition search,  ISqlExpression expr1, ISqlExpression expr2, CompareNulls compareNulls)
		{
			return search.Add(new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.Greater, expr2, compareNulls == CompareNulls.LikeClr ? true : null));
		}

		public static SqlSearchCondition AddGreaterOrEqual(this SqlSearchCondition search,  ISqlExpression expr1, ISqlExpression expr2, CompareNulls compareNulls)
		{
			return search.Add(new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.GreaterOrEqual, expr2, compareNulls == CompareNulls.LikeClr ? true : null));
		}

		public static SqlSearchCondition AddLess(this SqlSearchCondition search, ISqlExpression expr1, ISqlExpression expr2, CompareNulls compareNulls)
		{
			return search.Add(new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.Less, expr2, compareNulls == CompareNulls.LikeClr ? true : null));
		}
		
		public static SqlSearchCondition AddLessOrEqual(this SqlSearchCondition search,  ISqlExpression expr1, ISqlExpression expr2, CompareNulls compareNulls)
		{
			return search.Add(new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.LessOrEqual, expr2, compareNulls == CompareNulls.LikeClr ? true : null));
		}

		public static SqlSearchCondition AddEqual(this SqlSearchCondition search,  ISqlExpression expr1, ISqlExpression expr2, CompareNulls compareNulls)
		{
			return search.Add(new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.Equal, expr2, compareNulls == CompareNulls.LikeClr ? true : null));
		}

		public static SqlSearchCondition AddIsNull(this SqlSearchCondition search, ISqlExpression expr)
		{
			return search.Add(new SqlPredicate.IsNull(expr, false));
		}

		public static SqlSearchCondition AddIsNull(this SqlSearchCondition search, ISqlExpression expr, bool isNot)
		{
			return search.Add(new SqlPredicate.IsNull(expr, isNot));
		}

		public static SqlSearchCondition AddIsNotNull(this SqlSearchCondition search, ISqlExpression expr)
		{
			return search.Add(new SqlPredicate.IsNull(expr, true));
		}

		public static SqlSearchCondition AddNotEqual(this SqlSearchCondition search,  ISqlExpression expr1, ISqlExpression expr2, CompareNulls compareNulls)
		{
			return search.Add(new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.NotEqual, expr2, compareNulls == CompareNulls.LikeClr ? true : null));
		}
	
		public static SqlSearchCondition AddExists(this SqlSearchCondition search, SelectQuery selectQuery, bool isNot = false)
		{
			return search.Add(new SqlPredicate.Exists(isNot, selectQuery));
		}
	
		public static SqlSearchCondition AddNotExists(this SqlSearchCondition search, SelectQuery selectQuery)
		{
			return search.Add(new SqlPredicate.Exists(true, selectQuery));
		}

		public static SqlSearchCondition AddNot(this SqlSearchCondition search, ISqlExpression expression)
		{
			return search.Add(new SqlPredicate.Expr(expression).MakeNot());
		}
	}
}
