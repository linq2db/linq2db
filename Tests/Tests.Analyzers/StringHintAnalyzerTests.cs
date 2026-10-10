using System.Threading.Tasks;

using LinqToDB.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using NUnit.Framework;

using Verify = Tests.Analyzers.AnalyzerVerifier<LinqToDB.Analyzers.StringHintAnalyzer>;

namespace Tests.Analyzers
{
	[TestFixture]
	public sealed class StringHintAnalyzerTests
	{
		const string Header = """
			using System.Linq;

			using LinqToDB;
			using LinqToDB.DataProvider.Oracle;
			using LinqToDB.DataProvider.SqlCe;
			using LinqToDB.DataProvider.SqlServer;

			class Row
			{
				public int Id { get; set; }
			}

			""";

		// Every snippet shares one host method, so a test body is just the statements under analysis.
		static string Source(string statements, string members = "")
		{
			return Header
				+ "class C\n{\n" + members + "\n\tvoid M(ITable<Row> t, IQueryable<Row> q, string runtime)\n\t{\n"
				+ statements
				+ "\n\t}\n}\n";
		}

		static DiagnosticResult Expected(string hint, string equivalents, int location = 0)
		{
			return new DiagnosticResult(StringHintAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
				.WithLocation(location)
				.WithArguments(hint, equivalents);
		}

		const string NoLockOnGenericTable = "AsSqlServer().WithNoLock() for SQL Server, AsSqlCe().WithNoLock() for SQL Server CE";

		#region Positives

		[Test]
		public async Task TableHintOnGenericTable()
		{
			await Verify.VerifyAsync(
				Source("""t.{|#0:TableHint("NOLOCK")|};"""),
				Expected("NOLOCK", NoLockOnGenericTable));
		}

		[Test]
		public async Task WithOnGenericTable()
		{
			await Verify.VerifyAsync(
				Source("""t.{|#0:With("NOLOCK")|};"""),
				Expected("NOLOCK", NoLockOnGenericTable));
		}

		[Test]
		public async Task MatchIgnoresCaseAndSurroundingBlanks()
		{
			await Verify.VerifyAsync(
				Source("""t.{|#0:TableHint(" nolock ")|};"""),
				Expected("nolock", NoLockOnGenericTable));
		}

		[Test]
		public async Task ConstantFieldAndConcatenation()
		{
			await Verify.VerifyAsync(
				Source(
					"""
					t.{|#0:TableHint(Hint)|};
					t.{|#1:TableHint("UPD" + "LOCK")|};
					""",
					"\tconst string Hint = \"NOLOCK\";\n"),
				Expected("NOLOCK", NoLockOnGenericTable, 0),
				Expected("UPDLOCK", "AsSqlServer().WithUpdLock() for SQL Server, AsSqlCe().WithUpdLock() for SQL Server CE", 1));
		}

		[Test]
		public async Task TypedConstantPassedAsText()
		{
			// SqlServerHints.Table.NoLock is still text to the generic overload, which emits it on every provider.
			await Verify.VerifyAsync(
				Source("""t.{|#0:TableHint(SqlServerHints.Table.NoLock)|};"""),
				Expected("NoLock", NoLockOnGenericTable));
		}

		[Test]
		public async Task SqlServerOverloadIsExact()
		{
			await Verify.VerifyAsync(
				Source(
					"""
					t.AsSqlServer().{|#0:TableHint("NOLOCK")|};
					q.AsSqlServer().{|#1:TablesInScopeHint("NOLOCK")|};
					q.AsSqlServer().{|#2:QueryHint("RECOMPILE")|};
					"""),
				Expected("NOLOCK",    "WithNoLock()",        0),
				Expected("NOLOCK",    "WithNoLockInScope()", 1),
				Expected("RECOMPILE", "OptionRecompile()",   2));
		}

		[Test]
		public async Task GenericOverloadOnSqlServerReceiverKeepsEveryProvider()
		{
			// SqlServerHints has no With: the call binds the generic LinqExtensions.With, which emits for every provider.
			await Verify.VerifyAsync(
				Source("""t.AsSqlServer().{|#0:With("NOLOCK")|};"""),
				Expected("NOLOCK", "WithNoLock() for SQL Server, AsSqlCe().WithNoLock() for SQL Server CE"));
		}

		[Test]
		public async Task SqlCeReceiverOffersSqlCeOnly()
		{
			await Verify.VerifyAsync(
				Source("""t.AsSqlCe().{|#0:TableHint("NOLOCK")|};"""),
				Expected("NOLOCK", "WithNoLock()"));
		}

		[Test]
		public async Task TablesInScopeHintOnGenericQuery()
		{
			await Verify.VerifyAsync(
				Source("""q.{|#0:TablesInScopeHint("NOLOCK")|};"""),
				Expected("NOLOCK", "AsSqlServer().WithNoLockInScope() for SQL Server, AsSqlCe().WithNoLockInScope() for SQL Server CE"));
		}

		[Test]
		public async Task TablesInScopeHintOnTableGoesThroughAsQueryable()
		{
			// AsSqlServer() on a table yields the table type, which the scope helper does not take; the second call
			// therefore binds the generic overload too.
			await Verify.VerifyAsync(
				Source(
					"""
					t.{|#0:TablesInScopeHint("NOLOCK")|};
					t.AsSqlServer().{|#1:TablesInScopeHint("NOLOCK")|};
					"""),
				Expected("NOLOCK", "AsQueryable().AsSqlServer().WithNoLockInScope() for SQL Server, AsQueryable().AsSqlCe().WithNoLockInScope() for SQL Server CE", 0),
				Expected("NOLOCK", "AsQueryable().AsSqlServer().WithNoLockInScope() for SQL Server, AsQueryable().AsSqlCe().WithNoLockInScope() for SQL Server CE", 1));
		}

		[Test]
		public async Task QueryHintOnGenericQuery()
		{
			await Verify.VerifyAsync(
				Source("""q.{|#0:QueryHint("RECOMPILE")|};"""),
				Expected("RECOMPILE", "AsSqlServer().OptionRecompile() for SQL Server"));
		}

		[Test]
		public async Task OracleHintClassServesTableAndQuery()
		{
			await Verify.VerifyAsync(
				Source(
					"""
					t.{|#0:TableHint("FULL")|};
					q.{|#1:QueryHint("ALL_ROWS")|};
					"""),
				Expected("FULL",     "AsOracle().FullHint() for Oracle",    0),
				Expected("ALL_ROWS", "AsOracle().AllRowsHint() for Oracle", 1));
		}

		[Test]
		public async Task StaticCallForm()
		{
			await Verify.VerifyAsync(
				Source("""LinqExtensions.{|#0:TableHint(t, "NOLOCK")|};"""),
				Expected("NOLOCK", NoLockOnGenericTable));
		}

		[Test]
		public async Task InsideAQueryExpression()
		{
			await Verify.VerifyAsync(
				Source("""var r = q.Where(x => t.{|#0:With("NOLOCK")|}.Any(y => y.Id == x.Id));"""),
				Expected("NOLOCK", NoLockOnGenericTable));
		}

		#endregion

		#region Negatives

		[Test]
		public async Task RuntimeStringIsIgnored()
		{
			await Verify.VerifyAsync(Source("""t.TableHint(runtime);"""));
		}

		[Test]
		public async Task TextWithoutTypedEquivalentIsIgnored()
		{
			await Verify.VerifyAsync(Source(
				"""
				t.TableHint("NOLOCK, INDEX(0)");
				q.QueryHint("FOR UPDATE");
				"""));
		}

		[Test]
		public async Task ParameterisedHintIsIgnored()
		{
			// Index has typed helpers only with an index name; the overloads with a parameter are not triggers.
			await Verify.VerifyAsync(Source(
				"""
				t.TableHint("INDEX");
				t.TableHint("INDEX", "IX_Row");
				t.TableHint("NOLOCK", 1);
				"""));
		}

		[Test]
		public async Task VersionGatedHelperIsNotOffered()
		{
			// The typed helpers emit only from SQL Server 2012 / 2014 / 2019 on; the string emits on every version.
			await Verify.VerifyAsync(Source(
				"""
				t.TableHint("FORCESCAN");
				q.TablesInScopeHint("SNAPSHOT");
				q.QueryHint("NO_PERFORMANCE_SPOOL");
				q.AsSqlServer().QueryHint("OPTIMIZE FOR UNKNOWN");
				"""));
		}

		[Test]
		public async Task TableTextInQueryScopeIsIgnored()
		{
			// A table constant is matched only by the table and tables-in-scope methods.
			await Verify.VerifyAsync(Source("""q.QueryHint("NOLOCK");"""));
		}

		[Test]
		public async Task OtherProvidersReceiverIsIgnored()
		{
			// An Oracle receiver is already scoped to Oracle, which has no typed NOLOCK.
			await Verify.VerifyAsync(Source("""t.AsOracle().TableHint("NOLOCK");"""));
		}

		[Test]
		public async Task TypedHelperIsNotReported()
		{
			await Verify.VerifyAsync(Source(
				"""
				t.AsSqlServer().WithNoLock();
				q.AsSqlServer().WithNoLockInScope().OptionRecompile();
				"""));
		}

		[Test]
		public async Task UserMethodWithTheSameNameIsIgnored()
		{
			await Verify.VerifyAsync(Source(
				"""Ext.TableHint(t, "NOLOCK");""",
				"\tstatic class Ext { public static ITable<Row> TableHint(ITable<Row> t, string hint) { return t; } }\n"));
		}

		[Test]
		public async Task SilentWithoutLinqToDB()
		{
			await Verify.VerifyWithoutLinqToDBAsync("""
				static class Ext { public static object TableHint(this object t, string hint) { return t; } }
				class C { void M(object t) { t.TableHint("NOLOCK"); } }
				""");
		}

		[Test]
		public async Task EditorConfigCanSilenceTheRule()
		{
			await Verify.VerifyAsync(
				Source("""t.TableHint("NOLOCK");"""),
				"root = true\n\n[*]\ndotnet_diagnostic.L2DB2001.severity = none\n");
		}

		[Test]
		public async Task PragmaSilencesTheRule()
		{
			await Verify.VerifyAsync(Source(
				"""
				#pragma warning disable L2DB2001
				t.TableHint("NOLOCK");
				#pragma warning restore L2DB2001
				"""));
		}

		#endregion

		[Test]
		public void MessagePutsTheCodeFactFirstAndThePackagePathLast()
		{
			var descriptor = new StringHintAnalyzer().SupportedDiagnostics[0];
			var message    = descriptor.MessageFormat.ToString(System.Globalization.CultureInfo.InvariantCulture);

			Assert.That(message, Does.StartWith("Hint '{0}' has a typed equivalent: {1}."));
			Assert.That(message, Does.EndWith(StringHintAnalyzer.GuidePath));
			Assert.That(descriptor.DefaultSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
		}
	}
}
