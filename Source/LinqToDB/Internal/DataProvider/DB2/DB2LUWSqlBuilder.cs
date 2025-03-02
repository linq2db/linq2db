using System.Globalization;
using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.DataProvider.DB2;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.Internal.DataProvider.DB2
{
	sealed class DB2LUWSqlBuilder : DB2SqlBuilderBase
	{
		public DB2LUWSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		DB2LUWSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new DB2LUWSqlBuilder(this);
		}

		protected override DB2Version Version => DB2Version.LUW;

		protected override string GetPhysicalTableName(ISqlTableSource table, string? alias,
			bool ignoreTableExpression = false, string? defaultDatabaseName = null, bool withoutSuffix = false)
		{
			var name = base.GetPhysicalTableName(table, alias, ignoreTableExpression: ignoreTableExpression, defaultDatabaseName: defaultDatabaseName, withoutSuffix: withoutSuffix);

			if (table.SqlTableType == SqlTableType.Function)
				return $"TABLE({name})";

			return name;
		}

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions, bool withoutSuffix)
		{
			if (objectType == ConvertType.NameToProcedure && name.Database != null)
				throw new LinqToDBException("DB2 LUW cannot address functions/procedures with database name specified.");

			var schemaName = name.Schema;
			if (schemaName == null && tableOptions.IsTemporaryOptionSet())
				schemaName = "SESSION";

			// "db..table" syntax not supported
			if (name.Database != null && schemaName == null)
				throw new LinqToDBException("DB2 requires schema name if database name provided.");

			if (name.Database != null)
			{
				(escape ? Convert(sb, name.Database, ConvertType.NameToDatabase) : sb.Append(name.Database))
					.Append('.');
				if (schemaName == null)
					sb.Append('.');
			}

			if (schemaName != null)
			{
				(escape ? Convert(sb, schemaName, ConvertType.NameToSchema) : sb.Append(schemaName))
					.Append('.');
			}

			if (name.Package != null)
			{
				(escape ? Convert(sb, name.Package, ConvertType.NameToPackage) : sb.Append(name.Package))
					.Append('.');
			}

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.VarBinary:
					// https://www.ibm.com/docs/en/db2/11.5?topic=list-binary-strings
					var length = type.Length == null || type.Length > 32672 || type.Length < 1 ? 32672 : type.Length;
					StringBuilder.Append(CultureInfo.InvariantCulture, $"VARBINARY({length})");
					return;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
		}
	}
}
