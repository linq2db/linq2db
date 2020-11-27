using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.SQLite
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;
	using Common;
	using Mapping;
	using Tools;

	class SQLiteSqlOptimizer : BasicSqlOptimizer
	{
		public SQLiteSqlOptimizer(SqlProviderFlags sqlProviderFlags)
			: base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatementMutable(SqlStatement statement)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete :
					statement = GetAlternativeDelete((SqlDeleteStatement)statement);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update :
					statement = GetAlternativeUpdate((SqlUpdateStatement)statement);
					break;
			}

			return statement;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expr, EvaluationContext context)
		{
			expr = base.ConvertExpressionImpl(expr, context);

			if (expr is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence) : expr;
					case "^": // (a + b) - (a & b) * 2
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							Mul(new SqlBinaryExpression(be.SystemType, be.Expr1, "&", be.Expr2), 2), be.SystemType);
				}
			}
			else if (expr is SqlFunction func)
			{
				switch (func.Name)
				{
					case "Space"   : return new SqlFunction(func.SystemType, "PadR", new SqlValue(" "), func.Parameters[0]);
					case "Convert" :
					{
						var ftype = func.SystemType.ToUnderlying();

						if (ftype == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, 1);
							if (ex != null)
								return ex;
						}

						if (ftype == typeof(DateTime) || ftype == typeof(DateTimeOffset))
						{
							if (IsDateDataType(func.Parameters[0], "Date"))
								return new SqlFunction(func.SystemType, "Date", func.Parameters[1]);
							return new SqlFunction(func.SystemType, "DateTime", func.Parameters[1]);
						}

						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, func.Parameters[1], func.Parameters[0]);
					}
				}
			}

			return expr;
		}

		public override ISqlPredicate ConvertPredicateImpl(MappingSchema mappingSchema, ISqlPredicate predicate, ConvertVisitor visitor, EvaluationContext context)
		{
			if (predicate is SqlPredicate.ExprExpr exprExpr)
			{
				var leftType  = QueryHelper.GetDbDataType(exprExpr.Expr1);
				var rightType = QueryHelper.GetDbDataType(exprExpr.Expr2);

				if ((IsDateTime(leftType) || IsDateTime(rightType)) &&
				    !(exprExpr.Expr1.TryEvaluateExpression(context, out var value1) && value1 == null ||
				      exprExpr.Expr2.TryEvaluateExpression(context, out var value2) && value2 == null))
				{
					if (!(exprExpr.Expr1 is SqlFunction func1 && (func1.Name == "$Convert$" || func1.Name == "DateTime")))
					{
						var left = new SqlFunction(leftType.SystemType, "$Convert$", SqlDataType.GetDataType(leftType.SystemType),
							new SqlDataType(leftType), exprExpr.Expr1);
						exprExpr = new SqlPredicate.ExprExpr(left, exprExpr.Operator, exprExpr.Expr2, null);
					}
					
					if (!(exprExpr.Expr2 is SqlFunction func2 && (func2.Name == "$Convert$" || func2.Name == "DateTime")))
					{
						var right = new SqlFunction(rightType.SystemType, "$Convert$", new SqlDataType(rightType),
							new SqlDataType(rightType), exprExpr.Expr2);
						exprExpr = new SqlPredicate.ExprExpr(exprExpr.Expr1, exprExpr.Operator, right, null);
					}

					predicate = exprExpr;
				}
			}

			predicate = base.ConvertPredicateImpl(mappingSchema, predicate, visitor, context);
			return predicate;
		}


		private static bool IsDateTime(DbDataType dbDataType)
		{
			if (dbDataType.DataType.In(DataType.Date, DataType.Time, DataType.DateTime, DataType.DateTime2,
				DataType.DateTimeOffset, DataType.SmallDateTime, DataType.Timestamp))
				return true;

			if (dbDataType.DataType != DataType.Undefined)
				return false;

			return IsDateTime(dbDataType.SystemType);
		}

		private static bool IsDateTime(Type type)
		{
			return    type == typeof(DateTime)
			          || type == typeof(DateTimeOffset)
			          || type == typeof(DateTime?)
			          || type == typeof(DateTimeOffset?);
		}
	}
}
