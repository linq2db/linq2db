﻿namespace LinqToDB.DataProvider.PostgreSQL
{
	using LinqToDB.Extensions;
	using LinqToDB.SqlProvider;
	using LinqToDB.SqlQuery;

	public class PostgreSQLSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public PostgreSQLSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		public override bool CanCompareSearchConditions => true;

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var searchPredicate = ConvertSearchStringPredicateViaLike(predicate);

			if (false == predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) && searchPredicate is SqlPredicate.Like likePredicate)
			{
				searchPredicate = new SqlPredicate.Like(likePredicate.Expr1, likePredicate.IsNot, likePredicate.Expr2, likePredicate.Escape, "ILIKE");
			}

			return searchPredicate;
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "^": return new SqlBinaryExpression(element.SystemType, element.Expr1, "#", element.Expr2);
				case "+": return element.SystemType == typeof(string) ? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case "CharIndex" :
				{
					return func.Parameters.Length == 2
						? new SqlExpression(func.SystemType, "Position({0} in {1})", Precedence.Primary,
							func.Parameters[0], func.Parameters[1])
						: Add<int>(
							new SqlExpression(func.SystemType, "Position({0} in {1})", Precedence.Primary,
								func.Parameters[0],
								(ISqlExpression)Visit(
									new SqlFunction(typeof(string), "Substring",
										func.Parameters[1],
										func.Parameters[2],
										Sub<int>(
											(ISqlExpression)Visit(
												new SqlFunction(typeof(int), "Length", func.Parameters[1])),
											func.Parameters[2]))
								)),
							Sub(func.Parameters[2], 1));
				}
			}

			return base.ConvertSqlFunction(func);
		}

		protected override ISqlExpression ConvertConversion(SqlFunction func)
		{
			if (func.SystemType.ToUnderlying() == typeof(bool))
			{
				var ex = AlternativeConvertToBoolean(func, 2);
				if (ex != null)
					return ex;
			}

			// Another cast syntax
			//
			// rreturn new SqlExpression(func.SystemType, "{0}::{1}", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
			return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary,
				FloorBeforeConvert(func, func.Parameters[2]), func.Parameters[0]);
		}
	}
}