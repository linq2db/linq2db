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

		protected override string LimitFormat         { get { return SelectQuery.Select.SkipValue != null ? "FETCH NEXT {0} ROWS ONLY" : null; } }
		protected override string OffsetFormat        { get { return "OFFSET {0} ROWS"; } }
		protected override bool   OffsetFirst         { get { return true;              } }
		protected override bool   BuildAlternativeSql { get { return false;             } }

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2012SqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override void BuildSql()
		{
			if (NeedSkip && SelectQuery.OrderBy.IsEmpty)
			{
				for (var i = 0; i < SelectQuery.Select.Columns.Count; i++)
					SelectQuery.OrderBy.ExprAsc(new SqlValue(i + 1));
			}

			base.BuildSql();
		}

		protected override void BuildInsertOrUpdateQuery()
		{
			BuildInsertOrUpdateQueryAsMerge(null);
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
				case "CASE"     : func = ConvertCase(func.SystemType, func.Parameters, 0); break;
				case "Coalesce" :

					if (func.Parameters.Length > 2)
					{
						var parms = new ISqlExpression[func.Parameters.Length - 1];

						Array.Copy(func.Parameters, 1, parms, 0, parms.Length);
						BuildFunction(new SqlFunction(func.SystemType, func.Name, func.Parameters[0],
						              new SqlFunction(func.SystemType, func.Name, parms)));
						return;
					}

					var sc = new SelectQuery.SearchCondition();

					sc.Conditions.Add(new SelectQuery.Condition(false, new SelectQuery.Predicate.IsNull(func.Parameters[0], false)));

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
				cond = new SelectQuery.SearchCondition(
					new SelectQuery.Condition(
						false,
						new SelectQuery.Predicate.ExprExpr(cond, SelectQuery.Predicate.Operator.Equal, new SqlValue(1))));
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
