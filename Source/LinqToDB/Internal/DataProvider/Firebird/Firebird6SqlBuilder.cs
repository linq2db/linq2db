using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

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
	}
}
