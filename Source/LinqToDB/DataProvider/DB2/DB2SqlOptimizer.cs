namespace LinqToDB.DataProvider.DB2
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class DB2SqlOptimizer : BasicSqlOptimizer
	{
		public DB2SqlOptimizer(SqlProviderFlags sqlProviderFlags, AstFactory ast) 
			: base(sqlProviderFlags, ast)
		{ }

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			// DB2 LUW 9/10 supports only FETCH, v11 adds OFFSET, but for that we need to introduce versions into DB2 provider first
			statement = SeparateDistinctFromPagination(statement, q => q.Select.SkipValue != null);
			statement = ReplaceDistinctOrderByWithRowNumber(statement, q => q.Select.SkipValue != null);
			statement = ReplaceTakeSkipWithRowNumber(SqlProviderFlags, statement, static (SqlProviderFlags, query) => query.Select.SkipValue != null && SqlProviderFlags.GetIsSkipSupportedFlag(query.Select.TakeValue, query.Select.SkipValue), true);

			// This is mutable part
			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement),
				QueryType.Update => GetAlternativeUpdate((SqlUpdateStatement)statement),
				_                => statement,
			};
		}

		public override bool CanCompareSearchConditions => true;
		
		protected static string[] DB2LikeCharactersToEscape = {"%", "_"};

		public override string[] LikeCharactersToEscape => DB2LikeCharactersToEscape;

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> visitor)
		{
			expression = base.ConvertExpressionImpl(expression, visitor);

			if (expression is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "%":
					{
						var expr1 = !be.Expr1.SystemType!.IsIntegerType() ? new SqlFunction(typeof(int), "Int", be.Expr1) : be.Expr1;
						return new SqlFunction(be.SystemType, "Mod", expr1, be.Expr2);
					}
					case "&": return new SqlFunction(be.SystemType, "BitAnd", be.Expr1, be.Expr2);
					case "|": return new SqlFunction(be.SystemType, "BitOr", be.Expr1, be.Expr2);
					case "^": return new SqlFunction(be.SystemType, "BitXor", be.Expr1, be.Expr2);
					case "+": return be.SystemType == typeof(string) ? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence) : expression;
				}
			}
			else if (expression is SqlFunction func)
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

					case "Millisecond"   : return ast.Divide<int>(ast.Func(func.SystemType, "Microsecond", func.Parameters), ast.Const(1000));
					case "SmallDateTime" :
					case "DateTime"      :
					case "DateTime2"     : return ast.Func(func.SystemType, "TimeStamp", func.Parameters);
					case "UInt16"        : return ast.Func(func.SystemType, "Int",       func.Parameters);
					case "UInt32"        : return ast.Func(func.SystemType, "BigInt",    func.Parameters);
					case "UInt64"        : return ast.Func(func.SystemType, "Decimal",   func.Parameters);
					case "Byte"          :
					case "SByte"         :
					case "Int16"         : return ast.Func(func.SystemType, "SmallInt",  func.Parameters);
					case "Int32"         : return ast.Func(func.SystemType, "Int",       func.Parameters);
					case "Int64"         : return ast.Func(func.SystemType, "BigInt",    func.Parameters);
					case "Double"        : return ast.Func(func.SystemType, "Float",     func.Parameters);
					case "Single"        : return ast.Func(func.SystemType, "Real",      func.Parameters);
					case "Money"         : return ast.Func(func.SystemType, "Decimal",   func.Parameters[0], ast.Const(19), ast.Const(4));
					case "SmallMoney"    : return ast.Func(func.SystemType, "Decimal",   func.Parameters[0], ast.Const(10), ast.Const(4));
					case "VarChar"       :
						if (func.Parameters[0].SystemType!.ToUnderlying() == typeof(decimal))
							return ast.Func(func.SystemType, "Char", func.Parameters[0]);
						break;

					case "NChar"         :
					case "NVarChar"      : return ast.Func(func.SystemType, "Char",      func.Parameters);
				}
			}

			return expression;
		}

		protected override ISqlExpression ConvertFunction(ISqlExpression expr)
		{
			if (expr is not SqlFunction func) return expr;
			func = ConvertFunctionParameters(func, false);
			return base.ConvertFunction(func);
		}

	}
}
