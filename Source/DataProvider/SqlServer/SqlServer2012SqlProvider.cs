using System;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlBuilder;
	using SqlProvider;

	public class SqlServer2012SqlProvider : SqlServerSqlProvider
	{
		public SqlServer2012SqlProvider(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		protected override string LimitFormat         { get { return SqlQuery.Select.SkipValue != null ? "FETCH NEXT {0} ROWS ONLY" : null; } }
		protected override string OffsetFormat        { get { return "OFFSET {0} ROWS"; } }
		protected override bool   OffsetFirst         { get { return true;              } }
		protected override bool   BuildAlternativeSql { get { return false;             } }

		protected override ISqlProvider CreateSqlProvider()
		{
			return new SqlServer2012SqlProvider(SqlProviderFlags);
		}

		protected override void BuildSql(StringBuilder sb)
		{
			if (NeedSkip && SqlQuery.OrderBy.IsEmpty)
			{
				for (var i = 0; i < SqlQuery.Select.Columns.Count; i++)
					SqlQuery.OrderBy.ExprAsc(new SqlValue(i + 1));
			}

			base.BuildSql(sb);
		}

		protected override void BuildInsertOrUpdateQuery(StringBuilder sb)
		{
			BuildInsertOrUpdateQueryAsMerge(sb, null);
			sb.AppendLine(";");
		}

		public override string  Name
		{
			get { return ProviderName.SqlServer2012; }
		}

		protected override void BuildFunction(StringBuilder sb, SqlFunction func)
		{
			switch (func.Name)
			{
				case "CASE"      : func = ConvertCase(func.SystemType, func.Parameters, 0); break;
				case "Coalesce"  :

					if (func.Parameters.Length > 2)
					{
						var parms = new ISqlExpression[func.Parameters.Length - 1];

						Array.Copy(func.Parameters, 1, parms, 0, parms.Length);
						BuildFunction(sb, new SqlFunction(func.SystemType, func.Name, func.Parameters[0],
						                  new SqlFunction(func.SystemType, func.Name, parms)));
						return;
					}

					var sc = new SqlQuery.SearchCondition();

					sc.Conditions.Add(new SqlQuery.Condition(false, new SqlQuery.Predicate.IsNull(func.Parameters[0], false)));

					func = new SqlFunction(func.SystemType, "IIF", sc, func.Parameters[1], func.Parameters[0]);

					break;
			}

			base.BuildFunction(sb, func);
		}

		SqlFunction ConvertCase(Type systemType, ISqlExpression[] parameters, int start)
		{
			var len = parameters.Length - start;

			if (len < 3)
				throw new SqlException("CASE statement is not supported by the {0}.", GetType().Name);

			if (len == 3)
				return new SqlFunction(systemType, "IIF", parameters[start], parameters[start + 1], parameters[start + 2]);

			return new SqlFunction(systemType, "IIF", parameters[start], parameters[start + 1], ConvertCase(systemType, parameters, start + 2));
		}
	}
}
