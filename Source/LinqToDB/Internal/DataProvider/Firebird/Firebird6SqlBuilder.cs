using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	public class Firebird6SqlBuilder : Firebird4SqlBuilder
	{
		public Firebird6SqlBuilder(IDataProvider provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		Firebird6SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new Firebird6SqlBuilder(this);
		}

		protected override bool SupportsNativeIfExists => true;

		public override StringBuilder BuildObjectName(
			StringBuilder sb,
			SqlObjectName name,
			ConvertType  objectType   = ConvertType.NameToQueryTable,
			bool         escape       = true,
			TableOptions tableOptions = TableOptions.NotSet,
			bool         withoutSuffix = false)
		{
			// Firebird 6 introduces SQL-standard schemas. Emit the schema qualifier when the mapping
			// specifies one; temporary tables live in an implicit module and must stay unqualified.
			var schemaName = tableOptions.HasIsTemporary() ? null : name.Schema;

			if (schemaName != null)
				(escape ? Convert(sb, schemaName, ConvertType.NameToSchema) : sb.Append(schemaName)).Append('.');

			if (name.Package != null)
				(escape ? Convert(sb, name.Package, ConvertType.NameToPackage) : sb.Append(name.Package)).Append('.');

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}
	}
}
