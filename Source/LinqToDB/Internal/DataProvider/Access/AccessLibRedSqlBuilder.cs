using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Access
{
	public class AccessLibRedSqlBuilder : AccessSqlBuilderBase
	{
		public AccessLibRedSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		AccessLibRedSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new AccessLibRedSqlBuilder(this);
		}

		// LibRed's grammar has no qualified-name production, so unlike the Microsoft flavours it cannot take
		// the database component AccessSqlBuilderBase emits: every spelling gives "mismatched input '.'".
		// Same shape as SqlCeSqlBuilder - a file database addresses one database and never names it.
		public override StringBuilder BuildObjectName(
			StringBuilder sb,
			SqlObjectName name,
			ConvertType objectType = ConvertType.NameToQueryTable,
			bool escape = true,
			TableOptions tableOptions = TableOptions.NotSet,
			bool withoutSuffix = false
		)
		{
			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}
	}
}
