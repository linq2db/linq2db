namespace LinqToDB.DataProvider.SapHana
{
	using LinqToDB.Mapping;
	using LinqToDB.SqlQuery;
	using SqlProvider;

	class SapHanaOdbcSqlBuilder : SapHanaSqlBuilder
	{
		public SapHanaOdbcSqlBuilder(MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}
		
		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SapHanaOdbcSqlBuilder(MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		public override string Convert(string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return "?";
				default:
					return base.Convert(value, convertType);
			}
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.Money:
					StringBuilder.Append("Decimal(19,4)");
					break;
				case DataType.SmallMoney:
					StringBuilder.Append("Decimal(10,4)");
					break;
				default:
					base.BuildDataTypeFromDataType(type, forCreateTable);
					break;
			}
		}
	}
}
