using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

		public override bool CanCompareSearchConditions => true;

		public override SqlStatement TransformStatement(SqlStatement statement)
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

		public override bool HasSpecialTimeSpanProcessing => true;

		static SqlValue _formatValue = new SqlValue("%Y-%m-%d %H:%M:%f");
		
		static bool GenerateDateAdd(ISqlExpression expr1, ISqlExpression expr2, bool isSubstraction, EvaluationContext context,
			[MaybeNullWhen(false)] out ISqlExpression generated)
		{
			var dbType1 = expr1.GetExpressionType();
			var dbType2 = expr2.GetExpressionType();

			if (dbType1.SystemType.ToNullableUnderlying().In(typeof(DateTime), typeof(DateTimeOffset))
			    && dbType2.SystemType.ToNullableUnderlying() == typeof(TimeSpan)
			    && expr2.TryEvaluateExpression(context, out var value))
			{
				var ts = value as TimeSpan?;
				var interval = " day";
				long? increment;

				if (ts == null)
				{
					generated = new SqlValue(dbType1, null);
					return true;
				}

				if (ts.Value.Milliseconds > 0)
				{
					increment = (long)ts.Value.TotalMilliseconds;
					interval = " millisecond";
				}
				else if (ts.Value.Seconds > 0)
				{
					increment = (long)ts.Value.TotalSeconds;
					interval = " second";
				}
				else if (ts.Value.Minutes > 0)
				{
					increment = (long)ts.Value.TotalMinutes;
					interval = " minute";
				}
				else if (ts.Value.Hours > 0)
				{
					increment = (long)ts.Value.TotalHours;
					interval = " hour";
				}
				else
				{
					increment = (long)ts.Value.TotalDays;
				}

				if (isSubstraction)
					increment = -increment;

				generated = new SqlFunction(
					dbType1.SystemType!,
					"strftime",
					false,
					true,
					_formatValue,
					expr1,
					new SqlBinaryExpression(typeof(string),
						CreateSqlValue(increment, new DbDataType(typeof(long)), expr2), "||", new SqlValue(interval)))
				{
					CanBeNull = expr1.CanBeNull || expr2.CanBeNull
				};
				return true;
			}


			generated = null;
			return false;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expr, ConvertVisitor visitor,
			EvaluationContext context)
		{
			expr = base.ConvertExpressionImpl(expr, visitor, context);

			if (expr is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "+":
					{
						if (be.SystemType == typeof(string)) 
							return new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence);

						if (GenerateDateAdd(be.Expr1, be.Expr2, false, context, out var generated))
							return generated;

						if (GenerateDateAdd(be.Expr2, be.Expr1, false, context, out generated))
							return generated;

						break;
					}
					case "-":
					{
						if (GenerateDateAdd(be.Expr1, be.Expr2, true, context, out var generated))
							return generated;

						break;
					}

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

		public override ISqlPredicate ConvertPredicateImpl(MappingSchema mappingSchema, ISqlPredicate predicate, ConvertVisitor visitor, OptimizationContext optimizationContext)
		{
			if (predicate is SqlPredicate.ExprExpr exprExpr)
			{
				var leftType  = QueryHelper.GetDbDataType(exprExpr.Expr1);
				var rightType = QueryHelper.GetDbDataType(exprExpr.Expr2);

				if ((IsDateTime(leftType) || IsDateTime(rightType)) &&
				    !(exprExpr.Expr1.TryEvaluateExpression(optimizationContext.Context, out var value1) && value1 == null ||
				      exprExpr.Expr2.TryEvaluateExpression(optimizationContext.Context, out var value2) && value2 == null))
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

			predicate = base.ConvertPredicateImpl(mappingSchema, predicate, visitor, optimizationContext);
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
