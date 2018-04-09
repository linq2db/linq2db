using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;

	class SqlServer2012SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2012SqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.SkipValue != null ? "FETCH NEXT {0} ROWS ONLY" : null;
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return "OFFSET {0} ROWS";
		}

		protected override bool   OffsetFirst         { get { return true;              } }
		protected override bool   BuildAlternativeSql { get { return false;             } }

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2012SqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override void BuildSql()
		{
			var selectQuery = Statement.SelectQuery;
			if (selectQuery != null)
			{
				if (NeedSkip(selectQuery) && selectQuery.OrderBy.IsEmpty)
				{
					for (var i = 0; i < selectQuery.Select.Columns.Count; i++)
						selectQuery.OrderBy.ExprAsc(new SqlValue(i + 1));
				}
			}

			base.BuildSql();
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, null);
			StringBuilder.AppendLine(";");
		}

		public override string  Name
		{
			get { return ProviderName.SqlServer2012; }
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);

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
						BuildFunction(new SqlFunction(func.SystemType, func.Name, func.Parameters[0],
							new SqlFunction(func.SystemType, func.Name, parms)));
						return;
					}

					var sc = new SqlSearchCondition();

					sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.IsNull(func.Parameters[0], false)));

					func = new SqlFunction(func.SystemType, "IIF", sc, func.Parameters[1], func.Parameters[0]);

					break;
			}

			base.BuildFunction(func);
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
						new SqlPredicate.ExprExpr(cond, SqlPredicate.Operator.Equal, new SqlValue(1))));
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
