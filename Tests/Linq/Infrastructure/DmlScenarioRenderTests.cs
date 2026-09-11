using System.Text;

using LinqToDB;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.DataProvider.Oracle;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Infrastructure
{
	/// <summary>
	/// Offline rendering of the auxiliary statements an <see cref="IDmlService"/> adds to a scenario. No connection is
	/// opened - the provider is asked for its builder directly - so these run on every leg and pin the rendered text of
	/// shapes that are otherwise only observable against a live server.
	/// </summary>
	[TestFixture]
	public class DmlScenarioRenderTests : TestBase
	{
		[Table("O'Brien")]
		sealed class ApostropheTable
		{
			[PrimaryKey, Identity] public int Id { get; set; }
		}

		[Table("Plain")]
		sealed class PlainTable
		{
			[PrimaryKey, Identity] public int Id { get; set; }
		}

		// The reset block embeds the sequence name inside PL/SQL string literals (EXECUTE IMMEDIATE '...'), so the
		// quoted identifier has to be escaped for the LITERAL, not just for identifier position: an apostrophe in the
		// table name would otherwise close the literal early and the remainder would be parsed as PL/SQL.
		[Test]
		public void Oracle_TruncateWithIdentityReset_EscapesApostropheInSequenceName()
		{
			var sql = RenderOracleTruncateReset<ApostropheTable>();

			sql.ShouldContain("EXECUTE IMMEDIATE 'SELECT \"SIDENTITY_O''Brien\".NEXTVAL FROM dual'");
			sql.ShouldContain("EXECUTE IMMEDIATE 'ALTER SEQUENCE \"SIDENTITY_O''Brien\" INCREMENT BY -'");
			sql.ShouldContain("EXECUTE IMMEDIATE 'ALTER SEQUENCE \"SIDENTITY_O''Brien\" INCREMENT BY 1 MINVALUE 0'");
		}

		// The control: a name that needs no literal escaping must render exactly as before, so the escaping above
		// cannot be satisfied by doubling apostrophes unconditionally.
		[Test]
		public void Oracle_TruncateWithIdentityReset_LeavesAnOrdinaryNameAlone()
		{
			var sql = RenderOracleTruncateReset<PlainTable>();

			sql.ShouldContain("SIDENTITY_Plain");
			sql.ShouldNotContain("''");
		}

		static string RenderOracleTruncateReset<T>()
		{
			var provider    = OracleTools.GetDataProvider(OracleVersion.v12, OracleProvider.Managed);
			var dataOptions = new DataOptions();
			var ms          = provider.MappingSchema;

			var truncate = new SqlTruncateTableStatement
			{
				Table         = new SqlTable(ms.GetEntityDescriptor(typeof(T))),
				ResetIdentity = true,
			};

			var sqlOptimizer = provider.GetSqlOptimizer(dataOptions);
			var factory      = sqlOptimizer.CreateSqlExpressionFactory(ms, dataOptions);

			var scenario = new OracleDmlService().BuildCommandScenario(truncate, provider.SqlProviderFlags, factory);

			scenario.ShouldNotBeNull();
			scenario.Steps.Count.ShouldBe(2);

			// Step 0 is the TRUNCATE itself; step 1 is the PL/SQL block that walks the sequence back to zero.
			return Render(provider, dataOptions, scenario.Steps[1].Statement!);
		}

		static string Render(IDataProvider provider, DataOptions dataOptions, SqlStatement statement)
		{
			var sqlOptimizer   = provider.GetSqlOptimizer(dataOptions);
			var factory        = sqlOptimizer.CreateSqlExpressionFactory(provider.MappingSchema, dataOptions);
			var convertVisitor = sqlOptimizer.CreateConvertVisitor(false);

			var optimizationContext = new OptimizationContext(
				new EvaluationContext(),
				dataOptions,
				provider.SqlProviderFlags,
				provider.MappingSchema,
				new SqlExpressionOptimizerVisitor(false),
				convertVisitor,
				factory,
				false,
				static () => NoopQueryParametersNormalizer.Instance);

			var sb = new StringBuilder();

			provider
				.CreateSqlBuilder(provider.MappingSchema, dataOptions)
				.BuildSql(statement, sb, optimizationContext, new AliasesContext(), null, 0);

			return sb.ToString();
		}
	}
}
