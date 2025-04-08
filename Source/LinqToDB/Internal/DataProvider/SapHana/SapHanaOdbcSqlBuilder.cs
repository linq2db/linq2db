using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	sealed class SapHanaOdbcSqlBuilder : SapHanaSqlBuilder
	{
		public SapHanaOdbcSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SapHanaOdbcSqlBuilder(this);
		}

		private SapHanaOdbcSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			return convertType switch
			{
				ConvertType.NameToQueryParameter => sb.Append('?'),
				_                                => base.Convert(sb, value, convertType),
			};
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.Money:
					StringBuilder.Append("Decimal(19, 4)");
					break;
				case DataType.SmallMoney:
					StringBuilder.Append("Decimal(10, 4)");
					break;
				default:
					base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
					break;
			}
		}
	}
}
