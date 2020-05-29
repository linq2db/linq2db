using System;

namespace LinqToDB.DataProvider.Informix
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class InformixSqlOptimizer : BasicSqlOptimizer
	{
		public InformixSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		static void SetQueryParameter(IQueryElement element)
		{
			if (element is SqlParameter p)
			{
				// TimeSpan parameters created for IDS provider and must be converted to literal as IDS doesn't support
				// intervals explicitly
				if ((p.Type.SystemType == typeof(TimeSpan) || p.Type.SystemType == typeof(TimeSpan?))
						&& p.Type.DataType != DataType.Int64)
					p.IsQueryParameter = false;
			}
		}

		static void ClearQueryParameter(IQueryElement element)
		{
			if (element is SqlParameter p && p.IsQueryParameter)
				p.IsQueryParameter = false;
		}

		public override SqlStatement Finalize(SqlStatement statement, bool inlineParameters)
		{
			CheckAliases(statement, int.MaxValue);

			new QueryVisitor().VisitAll(statement, SetQueryParameter);

			// TODO: test if it works and enable support with type-cast like it is done for Firebird
			// Informix doesn't support parameters in select list
			var ignore = statement.QueryType == QueryType.Insert && statement.SelectQuery!.From.Tables.Count == 0;
			if (!ignore)
				new QueryVisitor().VisitAll(statement, e =>
				{
					if (e is SqlSelectClause select)
						new QueryVisitor().VisitAll(select, ClearQueryParameter);
				});

			return base.Finalize(statement, inlineParameters);
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete:
					var deleteStatement = GetAlternativeDelete((SqlDeleteStatement)statement);
					statement = deleteStatement;
					if (deleteStatement.SelectQuery != null)
						deleteStatement.SelectQuery.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update:
					statement = GetAlternativeUpdate((SqlUpdateStatement)statement);
					break;
			}

			return statement;
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlBinaryExpression)
			{
				var be = (SqlBinaryExpression)expr;

				switch (be.Operation)
				{
					case "%": return new SqlFunction(be.SystemType, "Mod",    be.Expr1, be.Expr2);
					case "&": return new SqlFunction(be.SystemType, "BitAnd", be.Expr1, be.Expr2);
					case "|": return new SqlFunction(be.SystemType, "BitOr",  be.Expr1, be.Expr2);
					case "^": return new SqlFunction(be.SystemType, "BitXor", be.Expr1, be.Expr2);
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence): expr;
				}
			}
			else if (expr is SqlFunction)
			{
				var func = (SqlFunction)expr;

				switch (func.Name)
				{
					case "Coalesce" : return new SqlFunction(func.SystemType, "Nvl", func.Parameters);
					case "Convert"  :
						{
							var par0 = func.Parameters[0];
							var par1 = func.Parameters[1];

							switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
							{
								case TypeCode.String   : return new SqlFunction(func.SystemType, "To_Char", func.Parameters[1]);
								case TypeCode.Boolean  :
									{
										var ex = AlternativeConvertToBoolean(func, 1);
										if (ex != null)
											return ex;
										break;
									}

								case TypeCode.UInt64:
									if (func.Parameters[1].SystemType!.IsFloatType())
										par1 = new SqlFunction(func.SystemType, "Floor", func.Parameters[1]);
									break;

								case TypeCode.DateTime :
									if (IsDateDataType(func.Parameters[0], "Date"))
									{
										if (func.Parameters[1].SystemType == typeof(string))
										{
											return new SqlFunction(
												func.SystemType,
												"Date",
												new SqlFunction(func.SystemType, "To_Date", func.Parameters[1], new SqlValue("%Y-%m-%d")));
										}

										return new SqlFunction(func.SystemType, "Date", func.Parameters[1]);
									}

									if (IsTimeDataType(func.Parameters[0]))
										return new SqlExpression(func.SystemType, "Cast(Extend({0}, hour to second) as Char(8))", Precedence.Primary, func.Parameters[1]);

									return new SqlFunction(func.SystemType, "To_Date", func.Parameters[1]);

								default:
									if (func.SystemType.ToUnderlying() == typeof(DateTimeOffset))
										goto case TypeCode.DateTime;
									break;
							}

							return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, par1, par0);
						}
				}
			}

			return expr;
		}

	}
}
