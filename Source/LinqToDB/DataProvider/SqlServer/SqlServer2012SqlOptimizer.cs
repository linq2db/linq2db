using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;
	using SqlQuery;

	class SqlServer2012SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2012SqlOptimizer(SqlProviderFlags sqlProviderFlags) : this(sqlProviderFlags, SqlServerVersion.v2012)
		{
		}

		protected SqlServer2012SqlOptimizer(SqlProviderFlags sqlProviderFlags, SqlServerVersion version) : base(sqlProviderFlags, version)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			// SQL Server 2012 supports OFFSET/FETCH providing there is an ORDER BY
			// UPDATE queries do not directly support ORDER BY, TOP, OFFSET, or FETCH, but they are supported in subqueries

			if (statement.IsUpdate() || statement.IsDelete())
				statement = WrapRootTakeSkipOrderBy(statement);

			statement = AddOrderByForSkip(statement);

			return statement;
		}

		/// <summary>
		/// Adds an ORDER BY clause to queries using OFFSET/FETCH, if none exists
		/// </summary>
		protected SqlStatement AddOrderByForSkip(SqlStatement statement)
		{
			statement = statement.Convert(static (visitor, element) =>
			{
				if (element.ElementType == QueryElementType.OrderByClause)
				{
					var orderByClause = (SqlOrderByClause)element;
					if (orderByClause.OrderBy.IsEmpty && orderByClause.SelectQuery.Select.SkipValue != null)
					{
						return new SqlOrderByClause(new[] { new SqlOrderByItem(new SqlValue(typeof(int), 1), false) });
					}
				}
				return element;
			});
			return statement;
		}

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);

			switch (func.Name)
			{
				case PseudoFunctions.TRY_CONVERT:
					return new SqlFunction(func.SystemType, "TRY_CONVERT", false, true, func.Parameters[0], func.Parameters[2]) { CanBeNull = true };

				case "CASE"     :

					if (func.Parameters.Length <= 5)
						func = ConvertCase(func.CanBeNull, func.SystemType, func.Parameters, 0);

					break;
			}

			return base.ConvertFunction(func);
		}

		static SqlFunction ConvertCase(bool canBeNull, Type systemType, ISqlExpression[] parameters, int start)
		{
			var len  = parameters.Length - start;
			var name = start == 0 ? "IIF" : "CASE";
			var cond = parameters[start];

			if (start == 0 && SqlExpression.NeedsEqual(cond))
			{
				cond = new SqlSearchCondition(
					new SqlCondition(
						false,
						new SqlPredicate.ExprExpr(cond, SqlPredicate.Operator.Equal, new SqlValue(1), null)));
			}

			if (len == 3)
				return new SqlFunction(systemType, name, cond, parameters[start + 1], parameters[start + 2]) { CanBeNull = canBeNull };

			return new SqlFunction(systemType, name,
				cond,
				parameters[start + 1],
				ConvertCase(canBeNull, systemType, parameters, start + 2)) { CanBeNull = canBeNull };
		}

	}
}
