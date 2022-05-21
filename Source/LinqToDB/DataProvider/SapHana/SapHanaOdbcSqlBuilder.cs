using System.Text;

namespace LinqToDB.DataProvider.SapHana
{
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	class SapHanaOdbcSqlBuilder : SapHanaSqlBuilder
	{
		public SapHanaOdbcSqlBuilder(SapHanaOdbcDataProvider provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{ }

		protected SapHanaOdbcSqlBuilder(SapHanaOdbcSqlBuilder parentBuilder) : base(parentBuilder)
		{ }

		protected override BasicSqlBuilder<IDataProvider> CreateSqlBuilder()
			=> new SapHanaOdbcSqlBuilder(this);

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			return convertType switch
			{
				ConvertType.NameToQueryParameter => sb.Append('?'),
				_                                => base.Convert(sb, value, convertType),
			};
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.Money:
					StringBuilder.Append("Decimal(19, 4)");
					break;
				case DataType.SmallMoney:
					StringBuilder.Append("Decimal(10, 4)");
					break;
				default:
					base.BuildDataTypeFromDataType(type, forCreateTable);
					break;
			}
		}
	}
}
