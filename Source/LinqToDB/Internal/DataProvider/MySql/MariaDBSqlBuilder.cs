using System.Globalization;
using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.MySql
{
	public class MariaDBSqlBuilder : MySqlSqlBuilder
	{
		public MariaDBSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override bool SupportsColumnAliasesInSource => true;

		MariaDBSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new MariaDBSqlBuilder(this) { HintBuilder = HintBuilder };
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			if (type.DataType is DataType.Vector32)
			{
				// MariaDB doesn't have default size, let it fail if user didn't specify size
				if (type.Length != null)
				{
					StringBuilder.AppendFormat(CultureInfo.InvariantCulture, "VECTOR({0})", type.Length);
					return;
				}
			}

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
		}
	}
}
