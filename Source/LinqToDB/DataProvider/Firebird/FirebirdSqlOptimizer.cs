﻿namespace LinqToDB.DataProvider.Firebird
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	public class FirebirdSqlOptimizer : BasicSqlOptimizer
	{
		public FirebirdSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		static void SetNonQueryParameter(IQueryElement element)
		{
			if (element.ElementType == QueryElementType.SqlParameter)
			{
				var p = (SqlParameter) element;
				if (p.Type.SystemType.IsScalar(false))
					p.IsQueryParameter = false;
			}
		}

		private bool SearchSelectClause(IQueryElement element)
		{
			if (element.ElementType != QueryElementType.SelectClause) return true;

			new QueryVisitor().VisitParentFirst(element, SetNonQueryParameterInSelectClause);

			return false;
		}

		private bool SetNonQueryParameterInSelectClause(IQueryElement element)
		{
			if (element.ElementType == QueryElementType.SqlParameter)
			{
				var p = (SqlParameter)element;
				if (p.Type.SystemType.IsScalar(false))
					p.IsQueryParameter = false;
				return false;
			}

			if (element.ElementType == QueryElementType.SqlQuery)
			{
				new QueryVisitor().VisitParentFirst(element, SearchSelectClause);
				return false;
			}

			return true;
		}

		public override SqlStatement Finalize(SqlStatement statement)
		{
			CheckAliases(statement, int.MaxValue);

			new QueryVisitor().VisitParentFirst(statement, SearchSelectClause);

			if (statement.QueryType == QueryType.InsertOrUpdate)
			{
				var insertOrUpdate = (SqlInsertOrUpdateStatement)statement;
				foreach (var key in insertOrUpdate.Insert.Items)
					new QueryVisitor().Visit(key.Expression!, SetNonQueryParameter);

				foreach (var key in insertOrUpdate.Update.Items)
					new QueryVisitor().Visit(key.Expression!, SetNonQueryParameter);

				foreach (var key in insertOrUpdate.Update.Keys)
					new QueryVisitor().Visit(key.Expression!, SetNonQueryParameter);
			}
			else if (statement.QueryType == QueryType.Update)
			{
				var update = (SqlUpdateStatement)statement;
				foreach (var key in update.Update.Items)
					new QueryVisitor().Visit(key.Expression!, SetNonQueryParameter);

				foreach (var key in update.Update.Keys)
					new QueryVisitor().Visit(key.Expression!, SetNonQueryParameter);
			}

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

						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
				}
			}

			return expr;
		}

	}
}
