using System;

using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Oracle
{
	public class OracleSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public OracleSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		#region LIKE

		protected static string[] OracleLikeCharactersToEscape = {"%", "_"};

		public override string[] LikeCharactersToEscape => OracleLikeCharactersToEscape;

		#endregion

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%": return new SqlFunction(element.SystemType, "MOD", element.Expr1, element.Expr2);
				case "&": return new SqlFunction(element.SystemType, "BITAND", element.Expr1, element.Expr2);
				case "|": // (a + b) - BITAND(a, b)
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						new SqlFunction(element.SystemType, "BITAND", element.Expr1, element.Expr2),
						element.SystemType);

				case "^": // (a + b) - BITAND(a, b) * 2
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						Mul(new SqlFunction(element.SystemType, "BITAND", element.Expr1, element.Expr2), 2),
						element.SystemType);
				case "+": return element.SystemType == typeof(string) ? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlExpression(SqlExpression element)
		{
			if (element.Expr.StartsWith("To_Number(To_Char(") && element.Expr.EndsWith(", 'FF'))"))
				return Div(new SqlExpression(element.SystemType, element.Expr.Replace("To_Number(To_Char(", "to_Number(To_Char("), element.Parameters), 1000);

			return base.ConvertSqlExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func)
			{
				case {
					Name: "CharIndex",
					Parameters: [var p0, var p1],
					SystemType: var type,
				}:
					return new SqlFunction(type, "InStr", p1, p0);

				case {
					Name: "CharIndex",
					Parameters: [var p0, var p1, var p2],
					SystemType: var type,
				}:
					return new SqlFunction(type, "InStr", p1, p0, p2);

				default:
					return base.ConvertSqlFunction(func);
			};
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var ftype = cast.SystemType.ToUnderlying();

			var toType   = cast.ToType;
			var argument = cast.Expression;

			if (ftype == typeof(DateTime) || ftype == typeof(DateTimeOffset)
#if NET8_0_OR_GREATER
				|| ftype == typeof(DateOnly)
#endif
			   )
			{
				if (IsTimeDataType(toType))
				{
					if (argument.SystemType == typeof(string))
						return argument;

					return new SqlFunction(cast.SystemType, "To_Char", argument, new SqlValue("HH24:MI:SS"));
				}

				if (IsDateDataType(toType, "Date"))
				{
					if (argument.SystemType!.ToUnderlying() == typeof(DateTime)
						|| argument.SystemType!.ToUnderlying() == typeof(DateTimeOffset))
					{
						return new SqlFunction(cast.SystemType, "Trunc", argument, new SqlValue("DD"));
					}

					return new SqlFunction(cast.SystemType, "TO_DATE", argument, new SqlValue("YYYY-MM-DD"));
				}
				else if (IsDateDataOffsetType(toType))
				{
					if (ftype == typeof(DateTimeOffset))
						return argument;

					return new SqlFunction(cast.SystemType, "TO_TIMESTAMP_TZ", argument, new SqlValue("YYYY-MM-DD HH24:MI:SS"));
				}

				return new SqlFunction(cast.SystemType, "TO_TIMESTAMP", argument, new SqlValue("YYYY-MM-DD HH24:MI:SS"));
			}
			else if (ftype == typeof(string))
			{
				var stype = argument.SystemType!.ToUnderlying();

				if (stype == typeof(DateTimeOffset))
				{
					return new SqlFunction(cast.SystemType, "To_Char", argument, new SqlValue("YYYY-MM-DD HH24:MI:SS TZH:TZM"));
				}
				else if (stype == typeof(DateTime))
				{
					return new SqlFunction(cast.SystemType, "To_Char", argument, new SqlValue("YYYY-MM-DD HH24:MI:SS"));
				}
#if NET8_0_OR_GREATER
				else if (stype == typeof(DateOnly))
				{
					return new SqlFunction(cast.SystemType, "To_Char", argument, new SqlValue("YYYY-MM-DD"));
				}
#endif
			}

			return FloorBeforeConvert(cast);
		}
	}
}
