using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.CodeModel;
using LinqToDB.DataModel;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Metadata;
using LinqToDB.Naming;
using LinqToDB.Scaffold;
using LinqToDB.Schema;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Scaffold
{
	[TestFixture]
	public sealed class SqlServerDecimalOverflowProtectionTests : TestBase
	{
		[Test]
		public void MarksSqlServerDecimalColumnsOutsideClrLimits()
		{
			var options = ScaffoldOptions.Default();
			options.DataModel.GenerateSqlServerDecimalOverflowProtection = true;

			var model = LoadModel(
				options,
				SqlServerDatabaseOptions.Instance,
				new Column("SafeDecimal"     ,       null, new DatabaseType("decimal", null, 28,  0), true, true, true, 0),
				new Column("PrecisionBoundary",      null, new DatabaseType("decimal", null, 29,  0), true, true, true, 1),
				new Column("LargePrecision"  ,       null, new DatabaseType("decimal", null, 30,  0), true, true, true, 2),
				new Column("LargeScale"      ,       null, new DatabaseType("decimal", null, 29, 29), true, true, true, 3),
				new Column("LargePrecisionAndScale", null, new DatabaseType("decimal", null, 38, 35), true, true, true, 4));

			var columns = model.DataContext.Entities[0].Columns;

			// decimal(28, 0) always fits CLR decimal; decimal(29, 0) can exceed decimal.MaxValue and must be protected.
			Assert.That(columns[0].Metadata.UseGetSqlDecimal, Is.False);
			Assert.That(columns[1].Metadata.UseGetSqlDecimal, Is.True);
			Assert.That(columns[2].Metadata.UseGetSqlDecimal, Is.True);
			Assert.That(columns[3].Metadata.UseGetSqlDecimal, Is.True);
			Assert.That(columns[4].Metadata.UseGetSqlDecimal, Is.True);
		}

		[Test]
		public void DoesNotMarkNonSqlServerDecimalColumns()
		{
			var options = ScaffoldOptions.Default();
			options.DataModel.GenerateSqlServerDecimalOverflowProtection = true;

			var model = LoadModel(
				options,
				DatabaseOptions.Default,
				new Column("Value", null, new DatabaseType("decimal", null, 38, 35), true, true, true, 0));

			Assert.That(model.DataContext.Entities[0].Columns[0].Metadata.UseGetSqlDecimal, Is.False);
		}

		[Test]
		public void EmitsGetSqlDecimalAttributeInGeneratedCode([Values(MetadataSource.Attributes, MetadataSource.FluentMapping)] MetadataSource metadata)
		{
			var options = ScaffoldOptions.Default();
			options.DataModel.GenerateSqlServerDecimalOverflowProtection = true;
			options.DataModel.Metadata                                   = metadata;

			var source = GenerateSource(
				options,
				SqlServerDatabaseOptions.Instance,
				new Column("SafeDecimal"   , null, new DatabaseType("decimal", null, 28,  0), true, true, true, 0),
				new Column("LargePrecision", null, new DatabaseType("decimal", null, 38, 35), true, true, true, 1));

			Assert.That(source, Does.Contain("GetSqlDecimal"));
		}

		[Test]
		public void DoesNotEmitGetSqlDecimalAttributeWhenDisabled([Values(MetadataSource.Attributes, MetadataSource.FluentMapping)] MetadataSource metadata)
		{
			var options = ScaffoldOptions.Default();
			options.DataModel.Metadata = metadata;

			var source = GenerateSource(
				options,
				SqlServerDatabaseOptions.Instance,
				new Column("LargePrecision", null, new DatabaseType("decimal", null, 38, 35), true, true, true, 0));

			Assert.That(source, Does.Not.Contain("GetSqlDecimal"));
		}

		static DatabaseModel LoadModel(ScaffoldOptions options, DatabaseOptions databaseOptions, params Column[] columns)
		{
			var scaffolder = new Scaffolder(LanguageProviders.CSharp, HumanizerNameConverter.Instance, options, null);
			var schema    = new TestSchemaProvider(databaseOptions, columns);

			return scaffolder.LoadDataModel(schema, TestTypeMappingProvider.Instance);
		}

		static string GenerateSource(ScaffoldOptions options, DatabaseOptions databaseOptions, params Column[] columns)
		{
			var scaffolder = new Scaffolder(LanguageProviders.CSharp, HumanizerNameConverter.Instance, options, null);
			var schema     = new TestSchemaProvider(databaseOptions, columns);
			var dataModel  = scaffolder.LoadDataModel(schema, TestTypeMappingProvider.Instance);

			var provider   = SqlServerTools.GetDataProvider(SqlServerVersion.v2012, SqlServerProvider.MicrosoftDataSqlClient);
			var sqlBuilder = provider.CreateSqlBuilder(provider.MappingSchema, new DataOptions());

			var files      = scaffolder.GenerateCodeModel(
				sqlBuilder,
				dataModel,
				MetadataBuilders.GetMetadataBuilder(scaffolder.Language, options.DataModel.Metadata),
				new ProviderSpecificStructsEqualityFixer(scaffolder.Language));

			return string.Join("\n", scaffolder.GenerateSourceCode(dataModel, files).Select(f => f.Code));
		}

		sealed class TestSchemaProvider(DatabaseOptions databaseOptions, IReadOnlyCollection<Column> columns) : ISchemaProvider
		{
			IEnumerable<Table> ISchemaProvider.GetTables()
			{
				yield return new Table(
					new SqlObjectName("TestTable", Schema: "dbo"),
					null,
					columns,
					null,
					null);
			}

			IEnumerable<View>              ISchemaProvider.GetViews()              => [];
			IEnumerable<ForeignKey>        ISchemaProvider.GetForeignKeys()        => [];
			IEnumerable<StoredProcedure>   ISchemaProvider.GetProcedures(bool withSchema, bool safeSchemaOnly) => [];
			IEnumerable<TableFunction>     ISchemaProvider.GetTableFunctions()     => [];
			IEnumerable<ScalarFunction>    ISchemaProvider.GetScalarFunctions()    => [];
			IEnumerable<AggregateFunction> ISchemaProvider.GetAggregateFunctions() => [];
			ISet<string>                   ISchemaProvider.GetDefaultSchemas()     => new HashSet<string>(["dbo"], System.StringComparer.Ordinal);
			string?                        ISchemaProvider.DatabaseName            => null;
			string?                        ISchemaProvider.ServerVersion           => null;
			string?                        ISchemaProvider.DataSource              => null;
			DatabaseOptions                ISchemaProvider.DatabaseOptions         => databaseOptions;
		}

		sealed class TestTypeMappingProvider : ITypeMappingProvider
		{
			public static readonly TestTypeMappingProvider Instance = new();

			LinqToDB.Schema.TypeMapping? ITypeMappingProvider.GetTypeMapping(DatabaseType databaseType)
			{
				return new LinqToDB.Schema.TypeMapping(WellKnownTypes.System.Decimal, LinqToDB.DataType.Decimal);
			}
		}
	}
}
