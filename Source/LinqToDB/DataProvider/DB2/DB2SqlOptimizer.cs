﻿using System;

namespace LinqToDB.DataProvider.DB2
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class DB2SqlOptimizer : BasicSqlOptimizer
	{
		public DB2SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		static void SetQueryParameter(IQueryElement element)
		{
			if (element.ElementType == QueryElementType.SqlParameter)
			{
				var p = (SqlParameter)element;

				if (p.Type.SystemType.ToNullableUnderlying() == typeof(TimeSpan))
					p.IsQueryParameter = true;
			}
		}

		public override SqlStatement Finalize(SqlStatement statement)
		{
			new QueryVisitor().Visit(statement, SetQueryParameter);

			return base.Finalize(statement);
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete : return GetAlternativeDelete((SqlDeleteStatement)statement);
				case QueryType.Update : return GetAlternativeUpdate((SqlUpdateStatement)statement);
				default               : return statement;
			}
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlBinaryExpression)
			{
				var be = (SqlBinaryExpression)expr;

				switch (be.Operation)
				{
					case "%":
						{
							var expr1 = !be.Expr1.SystemType!.IsIntegerType() ? new SqlFunction(typeof(int), "Int", be.Expr1) : be.Expr1;
							return new SqlFunction(be.SystemType, "Mod", expr1, be.Expr2);
						}
					case "&": return new SqlFunction(be.SystemType, "BitAnd", be.Expr1, be.Expr2);
					case "|": return new SqlFunction(be.SystemType, "BitOr",  be.Expr1, be.Expr2);
					case "^": return new SqlFunction(be.SystemType, "BitXor", be.Expr1, be.Expr2);
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence): expr;
				}
			}
			else if (expr is SqlFunction)
			{
				var func = (SqlFunction) expr;

				switch (func.Name)
				{
					case "Convert"    :
						if (func.SystemType.ToUnderlying() == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, 1);
							if (ex != null)
								return ex;
						}

						if (func.Parameters[0] is SqlDataType type)
						{
							if (type.Type.SystemType == typeof(string) && func.Parameters[1].SystemType != typeof(string))
								return new SqlFunction(func.SystemType, "RTrim", new SqlFunction(typeof(string), "Char", func.Parameters[1]));

							if (type.Type.Length > 0)
								return new SqlFunction(func.SystemType, type.Type.DataType.ToString(), func.Parameters[1], new SqlValue(type.Type.Length));

							if (type.Type.Precision > 0)
								return new SqlFunction(func.SystemType, type.Type.DataType.ToString(), func.Parameters[1], new SqlValue(type.Type.Precision), new SqlValue(type.Type.Scale ?? 0));

							return new SqlFunction(func.SystemType, type.Type.DataType.ToString(), func.Parameters[1]);
						}

						if (func.Parameters[0] is SqlFunction f)
						{
							return
								f.Name == "Char" ?
									new SqlFunction(func.SystemType, f.Name, func.Parameters[1]) :
								f.Parameters.Length == 1 ?
									new SqlFunction(func.SystemType, f.Name, func.Parameters[1], f.Parameters[0]) :
									new SqlFunction(func.SystemType, f.Name, func.Parameters[1], f.Parameters[0], f.Parameters[1]);
						}

						{
							var e = (SqlExpression)func.Parameters[0];
							return new SqlFunction(func.SystemType, e.Expr, func.Parameters[1]);
						}

					case "Millisecond"   : return Div(new SqlFunction(func.SystemType, "Microsecond", func.Parameters), 1000);
					case "SmallDateTime" :
					case "DateTime"      :
					case "DateTime2"     : return new SqlFunction(func.SystemType, "TimeStamp", func.Parameters);
					case "UInt16"        : return new SqlFunction(func.SystemType, "Int",       func.Parameters);
					case "UInt32"        : return new SqlFunction(func.SystemType, "BigInt",    func.Parameters);
					case "UInt64"        : return new SqlFunction(func.SystemType, "Decimal",   func.Parameters);
					case "Byte"          :
					case "SByte"         :
					case "Int16"         : return new SqlFunction(func.SystemType, "SmallInt",  func.Parameters);
					case "Int32"         : return new SqlFunction(func.SystemType, "Int",       func.Parameters);
					case "Int64"         : return new SqlFunction(func.SystemType, "BigInt",    func.Parameters);
					case "Double"        : return new SqlFunction(func.SystemType, "Float",     func.Parameters);
					case "Single"        : return new SqlFunction(func.SystemType, "Real",      func.Parameters);
					case "Money"         : return new SqlFunction(func.SystemType, "Decimal",   func.Parameters[0], new SqlValue(19), new SqlValue(4));
					case "SmallMoney"    : return new SqlFunction(func.SystemType, "Decimal",   func.Parameters[0], new SqlValue(10), new SqlValue(4));
					case "VarChar"       :
						if (func.Parameters[0].SystemType!.ToUnderlying() == typeof(decimal))
							return new SqlFunction(func.SystemType, "Char", func.Parameters[0]);
						break;

					case "NChar"         :
					case "NVarChar"      : return new SqlFunction(func.SystemType, "Char",      func.Parameters);
				}
			}

			return expr;
		}
	}
}
