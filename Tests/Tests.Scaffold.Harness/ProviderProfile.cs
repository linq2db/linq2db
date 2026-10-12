using System;

using LinqToDB;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Schema;

namespace Tests.Scaffold.Harness
{
	/// <summary>
	/// Target database for code generation: SQL dialect used by generator, scaffold database options, default schema and
	/// type mappings. Provider ADO.NET assemblies are resolved from the test host.
	/// </summary>
	public sealed class ProviderProfile
	{
		ProviderProfile(string name, Func<IDataProvider> dataProvider, DatabaseOptions databaseOptions, string defaultSchema, string connectionString, Func<TestTypeMappings> typeMappings)
		{
			Name                   = name;
			_dataProvider          = new Lazy<IDataProvider>(dataProvider);
			DatabaseOptions        = databaseOptions;
			DefaultSchema          = defaultSchema;
			DummyConnectionString  = connectionString;
			_typeMappings          = typeMappings;
		}

		readonly Lazy<IDataProvider>   _dataProvider;
		readonly Func<TestTypeMappings> _typeMappings;

		public string          Name                  { get; }
		public IDataProvider   DataProvider          => _dataProvider.Value;
		public DatabaseOptions DatabaseOptions       { get; }
		public string          DefaultSchema         { get; }

		/// <summary>
		/// Connection string for SQL generation without database access. Never opened: <c>ToSqlQuery()</c> doesn't open connection.
		/// </summary>
		public string DummyConnectionString { get; }

		public TestTypeMappings CreateTypeMappings() => _typeMappings();

		public TestSchema CreateSchema() => new(DatabaseOptions, DefaultSchema);

		/// <summary>
		/// Options for SQL generation without database access.
		/// </summary>
		public DataOptions CreateOfflineOptions() => new DataOptions().UseConnectionString(DataProvider, DummyConnectionString);

		public override string ToString() => Name;

		public static readonly ProviderProfile SQLite = new(
			"SQLite",
			static () => SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft),
			DatabaseOptions.Default,
			"main",
			"Data Source=:memory:",
			static () => new TestTypeMappings());

		public static readonly ProviderProfile SqlServer = new(
			"SqlServer",
			static () => SqlServerTools.GetDataProvider(SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient),
			SqlServerDatabaseOptions.Instance,
			"dbo",
			"Server=localhost;Database=Offline;TrustServerCertificate=true",
			static () => new TestTypeMappings()
				.Map("hierarchyid", "Microsoft.SqlServer.Types.SqlHierarchyId", true , DataType.Udt)
				.Map("geography"  , "Microsoft.SqlServer.Types.SqlGeography"  , false, DataType.Udt)
				.Map("geometry"   , "Microsoft.SqlServer.Types.SqlGeometry"   , false, DataType.Udt));

		public static readonly ProviderProfile PostgreSQL = new(
			"PostgreSQL",
			static () => PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v15),
			DatabaseOptions.Default,
			"public",
			"Host=localhost;Database=offline",
			static () => new TestTypeMappings()
				.Map("interval", "NpgsqlTypes.NpgsqlInterval", true, DataType.Interval)
				.Map("inet"    , "NpgsqlTypes.NpgsqlInet"    , true, DataType.Udt)
				.Map("point"   , "NpgsqlTypes.NpgsqlPoint"   , true, DataType.Udt)
				.Map("polygon" , "NpgsqlTypes.NpgsqlPolygon" , true, DataType.Udt));
	}
}
