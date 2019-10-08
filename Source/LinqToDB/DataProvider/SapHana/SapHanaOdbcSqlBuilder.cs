#nullable disable

namespace LinqToDB.DataProvider.SapHana
{
	using LinqToDB.SqlQuery;
	using SqlProvider;

	class SapHanaOdbcSqlBuilder : SapHanaSqlBuilder
	{
		public SapHanaOdbcSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}
		
		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SapHanaOdbcSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		public override object Convert(object value, ConvertType convertType)
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
			switch (type.DataType)
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
