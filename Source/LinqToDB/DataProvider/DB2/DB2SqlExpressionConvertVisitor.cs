using LinqToDB.Extensions;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.DB2
{
	public class DB2SqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public DB2SqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		public override bool CanCompareSearchConditions => true;

		static string[] DB2LikeCharactersToEscape = {"%", "_"};

		public override string[] LikeCharactersToEscape => DB2LikeCharactersToEscape;

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%":
				{
					var expr1 = !element.Expr1.SystemType!.IsIntegerType() ? new SqlFunction(typeof(int), "Int", element.Expr1) : element.Expr1;
					return new SqlFunction(element.SystemType, "Mod", expr1, element.Expr2);
				}
				case "&": return new SqlFunction(element.SystemType, "BitAnd", element.Expr1, element.Expr2);
				case "|": return new SqlFunction(element.SystemType, "BitOr", element.Expr1, element.Expr2);
				case "^": return new SqlFunction(element.SystemType, "BitXor", element.Expr1, element.Expr2);
				case "+": return element.SystemType == typeof(string) ? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case "Convert"    :
				{
					var par0 = func.Parameters[0];
					var par1 = func.Parameters[1];

					var isNull = par1 is SqlValue sqlValue && sqlValue.Value == null;

					if (isNull)
					{
						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, par1, par0);
					}

					if (func.SystemType.ToUnderlying() == typeof(bool))
					{
						var ex = AlternativeConvertToBoolean(func, 1);
						if (ex != null)
							return ex;
					}

					if (par0 is SqlDataType type)
					{
						if (type.Type.SystemType == typeof(string) && par1.SystemType != typeof(string))
							return new SqlFunction(func.SystemType, "RTrim", new SqlFunction(typeof(string), "Char", par1));

						if (type.Type.Length > 0)
							return new SqlFunction(func.SystemType, type.Type.DataType.ToString(), par1, new SqlValue(type.Type.Length));

						if (type.Type.Precision > 0)
							return new SqlFunction(func.SystemType, type.Type.DataType.ToString(), par1, new SqlValue(type.Type.Precision), new SqlValue(type.Type.Scale ?? 0));

						return new SqlFunction(func.SystemType, type.Type.DataType.ToString(), par1);
					}

					if (par0 is SqlFunction f)
					{
						return
							f.Name == "Char" ?
								new SqlFunction(func.SystemType, f.Name, par1) :
							f.Parameters.Length == 1 ?
								new SqlFunction(func.SystemType, f.Name, par1, f.Parameters[0]) :
								new SqlFunction(func.SystemType, f.Name, par1, f.Parameters[0], f.Parameters[1]);
					}

					var e = (SqlExpression)par0;
					return new SqlFunction(func.SystemType, e.Expr, par1);
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

			func = ConvertFunctionParameters(func, false);

			return base.ConvertSqlFunction(func);
		}
	}
}
