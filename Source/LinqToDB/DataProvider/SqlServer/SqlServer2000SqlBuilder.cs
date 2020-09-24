namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;
	using LinqToDB.Mapping;

	class SqlServer2000SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2000SqlBuilder(SqlServerDataProvider? provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		public SqlServer2000SqlBuilder(MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(null, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override string FirstFormat(SelectQuery selectQuery)
		{
			return "TOP {0}";
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2000SqlBuilder(Provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override void BuildOutputSubclause(SqlStatement statement, SqlInsertClause insertClause)
		{
			// OUTPUT clause is only supported by the MS SQL Server starts with 2005 version.
		}

		protected override void BuildOutputSubclause(SqlDeleteStatement deleteStatement)
		{
			// OUTPUT clause is only supported by the MS SQL Server starts with 2005 version.
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			StringBuilder
				.AppendLine()
				.AppendLine("SELECT SCOPE_IDENTITY()");
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.DateTimeOffset :
				case DataType.DateTime2      :
				case DataType.Time           :
				case DataType.Date           : StringBuilder.Append("DateTime"); return;
				case DataType.Xml            : StringBuilder.Append("NText");    return;
				case DataType.NVarChar       :

					if (type.Type.Length == null || type.Type.Length > 4000 || type.Type.Length < 1)
					{
						StringBuilder
							.Append(type.Type.DataType)
							.Append("(4000)");
						return;
					}

					break;

				case DataType.VarChar        :
				case DataType.VarBinary      :

					if (type.Type.Length == null || type.Type.Length > 8000 || type.Type.Length < 1)
					{
						StringBuilder
							.Append(type.Type.DataType)
							.Append("(8000)");
						return;
					}

					break;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable);
		}

		public override string  Name => ProviderName.SqlServer2000;

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			var table = dropTable.Table!;

			AppendIndent().Append("DROP TABLE ");
			BuildPhysicalTable(table, null);
			StringBuilder.AppendLine();
		}
	}
}
