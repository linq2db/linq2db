using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;

	class SqlServer2000SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2000SqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		protected override string FirstFormat(SelectQuery selectQuery)
		{
			return "TOP {0}";
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2000SqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override void BuildOutputSubclause(SelectQuery selectQuery)
		{
			// OUTPUT clause is only supported by the MS SQL Server starts with 2005 version.
		}

		protected override void BuildGetIdentity(SelectQuery selectQuery)
		{
			StringBuilder
				.AppendLine()
				.AppendLine("SELECT SCOPE_IDENTITY()");
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType)
		{
			switch (type.DataType)
			{
				case DataType.DateTimeOffset :
				case DataType.DateTime2      :
				case DataType.Time           :
				case DataType.Date           : StringBuilder.Append("DateTime"); return;
				case DataType.Xml            : StringBuilder.Append("NText");    return;
				case DataType.NVarChar       :

					if (type.Length == int.MaxValue || type.Length < 0)
					{
						StringBuilder
							.Append(type.DataType)
							.Append("(4000)");
						return;
					}

					break;

				case DataType.VarChar        :
				case DataType.VarBinary      :

					if (type.Length == int.MaxValue || type.Length < 0)
					{
						StringBuilder
							.Append(type.DataType)
							.Append("(8000)");
						return;
					}

					break;
			}

			base.BuildDataType(type, createDbType);
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(func);
		}

		public override string  Name
		{
			get { return ProviderName.SqlServer2000; }
		}

		protected override void BuildDropTableStatement(SqlCreateTableStatement createTable)
		{
			var table = createTable.Table;

			AppendIndent().Append("DROP TABLE ");
			BuildPhysicalTable(table, null);
			StringBuilder.AppendLine();
		}
	}
}
