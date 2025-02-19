using System.Globalization;


#if NETFRAMEWORK || NETSTANDARD2_0
using System.Text;

using LinqToDB;

#endif

using LinqToDB.Common;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.DB2;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.Internal.DataProvider.DB2
{
	sealed class DB2zOSSqlBuilder : DB2SqlBuilderBase
	{
		public DB2zOSSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		DB2zOSSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new DB2zOSSqlBuilder(this);
		}

		protected override DB2Version Version => DB2Version.zOS;

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.VarBinary:
					// https://www.ibm.com/docs/en/db2-for-zos/12?topic=strings-varying-length-binary
					var length = type.Length == null || type.Length > 32704 || type.Length < 1 ? 32704 : type.Length;
					StringBuilder.Append(CultureInfo.InvariantCulture, $"VARBINARY({length})");
					return;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
		}
	}
}
