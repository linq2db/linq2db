using LinqToDB;
using LinqToDB.Data;

namespace Default.SqlServer
{
	interface IIdentifiable
	{
		int ID { get; }
	}

	partial class TestDataDB
	{
		static DataOptions? _sqlServerDataOptions;

		static DataOptions GetSqlServerDataOptions(string? configuration = null)
		{
			return _sqlServerDataOptions ??= new DataOptions()
				.UseConfiguration(configuration ?? "SqlServerConfig")
				.WithOptions<BulkCopyOptions>  (o => o with { BulkCopyTimeout = 60 * 100 })
				;
		}
	}
}
