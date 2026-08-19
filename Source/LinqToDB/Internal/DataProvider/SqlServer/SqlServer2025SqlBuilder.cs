using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServer2025SqlBuilder : SqlServer2022SqlBuilder
	{
		public SqlServer2025SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected SqlServer2025SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2025SqlBuilder(this);
		}

		public override string Name => ProviderName.SqlServer2025;

		// SQL Server 2025 adds `||` as an ANSI-SQL string-concat operator (strict null propagation,
		// auto-coerce). Pre-2025 versions inherit the default `+` form.
		// https://learn.microsoft.com/en-us/sql/t-sql/language-elements/string-concatenation-pipes-transact-sql
		protected override ConcatBuildStyle ConcatStyle => ConcatBuildStyle.Pipes;

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			if (type.DataType == DataType.Json)
			{
				StringBuilder.Append("JSON");
				return;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
		}
	}
}
