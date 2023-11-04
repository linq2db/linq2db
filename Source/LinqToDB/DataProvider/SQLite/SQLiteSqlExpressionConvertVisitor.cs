using System;

namespace LinqToDB.DataProvider.SQLite
{
	using LinqToDB.Common;
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	public class SQLiteSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SQLiteSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		public override bool CanCompareSearchConditions => true;

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "+": return element.SystemType == typeof(string)? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
				case "^": // (a + b) - (a & b) * 2
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						Mul(new SqlBinaryExpression(element.SystemType, element.Expr1, "&", element.Expr2), 2), element.SystemType);
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case "Space"   : return new SqlFunction(func.SystemType, "PadR", new SqlValue(" "), func.Parameters[0]);
			}

			return base.ConvertSqlFunction(func);
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var like = ConvertSearchStringPredicateViaLike(predicate);

			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == true)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(string), "Substr", predicate.Expr1, new SqlValue(1),
									new SqlFunction(typeof(int), "Length", predicate.Expr2)),
								SqlPredicate.Operator.Equal,
								predicate.Expr2, null);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(string), "Substr", predicate.Expr1,
									new SqlBinaryExpression(typeof(int),
										new SqlFunction(typeof(int), "Length", predicate.Expr2), "*", new SqlValue(-1),
										Precedence.Multiplicative)
								),
								SqlPredicate.Operator.Equal,
								predicate.Expr2, null);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(int), "InStr", predicate.Expr1, predicate.Expr2),
								SqlPredicate.Operator.Greater,
								new SqlValue(0), null);

						break;
					}

				}

				if (subStrPredicate != null)
				{
					var result = new SqlSearchCondition(
						new SqlCondition(false, like, predicate.IsNot),
						new SqlCondition(predicate.IsNot, subStrPredicate));

					return result;
				}
			}

			return like;
		}

		private static bool IsDateTime(DbDataType dbDataType)
		{
			if (dbDataType.DataType == DataType.Date           ||
			    dbDataType.DataType == DataType.Time           ||
			    dbDataType.DataType == DataType.DateTime       ||
			    dbDataType.DataType == DataType.DateTime2      ||
			    dbDataType.DataType == DataType.DateTimeOffset ||
			    dbDataType.DataType == DataType.SmallDateTime  ||
			    dbDataType.DataType == DataType.Timestamp)
				return true;

			if (dbDataType.DataType != DataType.Undefined)
				return false;

			return IsDateTime(dbDataType.SystemType);
		}

		private static bool IsDateTime(Type type)
		{
			return    type    == typeof(DateTime)
			          || type == typeof(DateTimeOffset)
			          || type == typeof(DateTime?)
			          || type == typeof(DateTimeOffset?);
		}

		public override IQueryElement ConvertExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var leftType  = QueryHelper.GetDbDataType(predicate.Expr1);
			var rightType = QueryHelper.GetDbDataType(predicate.Expr2);

			if ((IsDateTime(leftType) || IsDateTime(rightType)) &&
			    !(predicate.Expr1.TryEvaluateExpression(EvaluationContext, out var value1) && value1 == null ||
			      predicate.Expr2.TryEvaluateExpression(EvaluationContext, out var value2) && value2 == null))
			{
				var expr1 = QueryHelper.UnwrapNullablity(predicate.Expr1);
				if (!(expr1 is SqlFunction func1 && (func1.Name == PseudoFunctions.CONVERT || func1.Name == "DateTime")))
				{
					var left = PseudoFunctions.MakeMandatoryConvert(new SqlDataType(leftType), new SqlDataType(leftType), predicate.Expr1);
					predicate = new SqlPredicate.ExprExpr(left, predicate.Operator, predicate.Expr2, null);
				}

				var expr2 = QueryHelper.UnwrapNullablity(predicate.Expr2);

				if (!(expr2 is SqlFunction func2 && (func2.Name == PseudoFunctions.CONVERT || func2.Name == "DateTime")))
				{
					var right = PseudoFunctions.MakeMandatoryConvert(new SqlDataType(rightType), new SqlDataType(rightType), predicate.Expr2);
					predicate = new SqlPredicate.ExprExpr(predicate.Expr1, predicate.Operator, right, null);
				}
			}

			return base.ConvertExprExprPredicate(predicate);
		}

		protected override ISqlExpression ConvertConversion(SqlFunction func)
		{
			var ftype = func.SystemType.ToUnderlying();

			if (ftype == typeof(bool))
			{
				var ex = AlternativeConvertToBoolean(func, 2);
				if (ex != null)
					return ex;
			}

			if (ftype == typeof(DateTime) || ftype == typeof(DateTimeOffset)
#if NET6_0_OR_GREATER
			                              || ftype == typeof(DateOnly)
#endif
			   )
			{
				if (IsDateDataType(func.Parameters[0], "Date"))
					return new SqlFunction(func.SystemType, "Date", func.Parameters[2]);
				return new SqlFunction(func.SystemType, "DateTime", func.Parameters[2]);
			}

			return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, func.Parameters[2], func.Parameters[0]);
		}
	}
}
