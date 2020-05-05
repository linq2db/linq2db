namespace LinqToDB.DataProvider.Firebird
{
	using System.Linq;
	using LinqToDB.Extensions;
	using LinqToDB.SqlQuery;
	using SqlProvider;

	public class FirebirdSqlOptimizer : BasicSqlOptimizer
	{
		public FirebirdSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement Finalize(SqlStatement statement)
		{
			CheckAliases(statement, int.MaxValue);

			statement = base.Finalize(statement);

			statement = WrapParameters(statement);

			return statement;
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
				SqlBinaryExpression be = (SqlBinaryExpression)expr;

				switch (be.Operation)
				{
					case "%": return new SqlFunction(be.SystemType, "Mod",     be.Expr1, be.Expr2);
					case "&": return new SqlFunction(be.SystemType, "Bin_And", be.Expr1, be.Expr2);
					case "|": return new SqlFunction(be.SystemType, "Bin_Or",  be.Expr1, be.Expr2);
					case "^": return new SqlFunction(be.SystemType, "Bin_Xor", be.Expr1, be.Expr2);
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence): expr;
				}
			}
			else if (expr is SqlFunction)
			{
				SqlFunction func = (SqlFunction)expr;

				switch (func.Name)
				{
					case "Convert" :
						if (func.SystemType.ToUnderlying() == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, 1);
							if (ex != null)
								return ex;
						}

						return new SqlExpression(func.SystemType, CASTEXPR, Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
				}
			}

			return expr;
		}

		#region Wrap Parameters
		private SqlStatement WrapParameters(SqlStatement statement)
		{
			// for some reason Firebird doesn't use parameter type information (not supported?) is some places, so
			// we need to wrap parameter into CAST() to add type information explicitly
			// As it is not clear when type CAST needed, below we should document observations on current behavior.
			//
			// When CAST is not needed:
			// - parameter already in CAST from original query
			// - parameter used as direct inserted/updated value in insert/update queries (including merge)
			//
			// When CAST is needed:
			// - in select column expression at any position (except nested subquery): select, subquery, merge source
			// - in composite expression in insert or update setter: insert, update, merge (not always, in some cases it works)

			statement = ConvertVisitor.Convert(statement, (visitor, e) =>
			{
				if (e is SqlParameter p && p.IsQueryParameter)
				{
					// Don't cast in cast
					if (visitor.ParentElement is SqlExpression expr && expr.Expr == CASTEXPR)
						return e;

					if (p.Type.SystemType == typeof(bool) && visitor.ParentElement is SqlFunction func && func.Name == "CASE")
						return e;

					var replace = false;
					for (var i = visitor.Stack.Count - 1; i >= 0; i--)
					{
						// went outside of subquery, mission abort
						if (visitor.Stack[i] is SelectQuery)
							return e;

						// part of select column
						if (visitor.Stack[i] is SqlColumn)
						{
							replace = true;
							break;
						}

						// insert or update keys used in merge source select query
						if (visitor.Stack[i] is SqlSetExpression set
							&& i == 2
							&& visitor.Stack[i - 1] is SqlInsertClause
							&& visitor.Stack[i - 2] is SqlInsertOrUpdateStatement insertOrUpdate
							&& insertOrUpdate.Update.Keys.Any(k => k.Expression == set.Expression))
						{
							replace = true;
							break;
						}

						// enumerable merge source
						if (visitor.Stack[i] is SqlValuesTable)
						{
							replace = true;
							break;
						}

						// complex insert/update statement, including merge
						if (visitor.Stack[i] is SqlSetExpression
							&& i >= 2
							&& i < visitor.Stack.Count - 1 // not just parameter setter
							&& (visitor.Stack[i - 1] is SqlUpdateClause
								|| visitor.Stack[i - 1] is SqlInsertClause
								|| visitor.Stack[i - 1] is SqlMergeOperationClause))
						{
							replace = true;
							break;
						}
					}

					if (!replace)
						return e;

					return new SqlExpression(p.Type.SystemType, CASTEXPR, Precedence.Primary, p, new SqlDataType(p.Type));
				}

				return e;
			});

			return statement;
		}

		private const string CASTEXPR = "Cast({0} as {1})";
		#endregion
	}
}
