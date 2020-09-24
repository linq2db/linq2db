using System;
using System.Linq;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;

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

			if (statement.IsUpdate() || statement.IsDelete()) statement = WrapRootTakeSkipOrderBy(statement);
			statement = AddOrderByForSkip(statement);

			return statement;
		}

		/// <summary>
		/// Adds an ORDER BY clause to queries using OFFSET/FETCH, if none exists
		/// </summary>
		protected SqlStatement AddOrderByForSkip(SqlStatement statement)
		{
			ConvertVisitor.Convert(statement, (visitor, element) => {
				if (element is SelectQuery query && query.Select.SkipValue != null && query.OrderBy.IsEmpty)
					query.OrderBy.ExprAsc(new SqlValue(typeof(int), 1));
				return element;
			});
			return statement;
		}

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);

			switch (func.Name)
			{
				case "CASE"     :

					if (func.Parameters.Length <= 5)
						func = ConvertCase(func.SystemType, func.Parameters, 0);

					break;

				case "Coalesce" :

					if (func.Parameters.Length > 2)
					{
						var parms = new ISqlExpression[func.Parameters.Length - 1];

						Array.Copy(func.Parameters, 1, parms, 0, parms.Length);

						func = new SqlFunction(func.SystemType, func.Name, func.Parameters[0],
							new SqlFunction(func.SystemType, func.Name, parms));

						break;
					}

					var sc = new SqlSearchCondition();

					sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.IsNull(func.Parameters[0], false)));

					func = new SqlFunction(func.SystemType, "IIF", sc, func.Parameters[1], func.Parameters[0]);

					break;
			}

			return base.ConvertFunction(func);
		}

		static SqlFunction ConvertCase(Type systemType, ISqlExpression[] parameters, int start)
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
				return new SqlFunction(systemType, name, cond, parameters[start + 1], parameters[start + 2]);

			return new SqlFunction(systemType, name,
				cond,
				parameters[start + 1],
				ConvertCase(systemType, parameters, start + 2));
		}

	}
}
