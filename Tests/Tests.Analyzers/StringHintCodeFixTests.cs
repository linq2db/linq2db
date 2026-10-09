using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Analyzers;
using LinqToDB.Analyzers.CodeFixes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using NUnit.Framework;

namespace Tests.Analyzers
{
	[TestFixture]
	public sealed class StringHintCodeFixTests
	{
		const string SqlServerKey = StringHintAnalyzer.DiagnosticId + "|SQL Server";
		const string SqlCeKey     = StringHintAnalyzer.DiagnosticId + "|SQL Server CE";

		// The fix is chosen by its equivalence key (one per provider), so a generic receiver's provider choice is explicit.
		static Task VerifyAsync(string source, string fixedSource, string equivalenceKey)
		{
			var test = new CSharpCodeFixTest<StringHintAnalyzer, StringHintCodeFixProvider, DefaultVerifier>
			{
				TestCode                  = source,
				FixedCode                 = fixedSource,
				ReferenceAssemblies       = ReferenceAssemblies.Net.Net80,
				CodeActionEquivalenceKey  = equivalenceKey,
			};

			var reference = MetadataReference.CreateFromFile(typeof(Sql).Assembly.Location);
			test.TestState.AdditionalReferences.Add(reference);
			test.FixedState.AdditionalReferences.Add(reference);

			return test.RunAsync(CancellationToken.None);
		}

		[Test]
		public async Task GenericTableGetsAsSqlServerAndUsing()
		{
			await VerifyAsync(
				"""
				using LinqToDB;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.[|TableHint("NOLOCK")|];
					}
				}
				""",
				"""
				using LinqToDB;
				using LinqToDB.DataProvider.SqlServer;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.AsSqlServer().WithNoLock();
					}
				}
				""",
				SqlServerKey);
		}

		[Test]
		public async Task GenericTableSqlCeChoice()
		{
			await VerifyAsync(
				"""
				using LinqToDB;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.[|With("NOLOCK")|];
					}
				}
				""",
				"""
				using LinqToDB;
				using LinqToDB.DataProvider.SqlCe;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.AsSqlCe().WithNoLock();
					}
				}
				""",
				SqlCeKey);
		}

		[Test]
		public async Task ProviderReceiverKeepsChainLayout()
		{
			await VerifyAsync(
				"""
				using System.Linq;

				using LinqToDB;
				using LinqToDB.DataProvider.SqlServer;

				class Row { public int Id { get; set; } }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t
							.AsSqlServer()
							.[|TableHint("UpdLock")|]
							.Where(x => x.Id > 0);
					}
				}
				""",
				"""
				using System.Linq;

				using LinqToDB;
				using LinqToDB.DataProvider.SqlServer;

				class Row { public int Id { get; set; } }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t
							.AsSqlServer()
							.WithUpdLock()
							.Where(x => x.Id > 0);
					}
				}
				""",
				SqlServerKey);
		}

		[Test]
		public async Task QueryHintOnGenericQuery()
		{
			await VerifyAsync(
				"""
				using System.Linq;

				using LinqToDB;

				class Row { }

				class C
				{
					void M(IQueryable<Row> q)
					{
						var r = q.[|QueryHint("RECOMPILE")|].ToList();
					}
				}
				""",
				"""
				using System.Linq;

				using LinqToDB;
				using LinqToDB.DataProvider.SqlServer;

				class Row { }

				class C
				{
					void M(IQueryable<Row> q)
					{
						var r = q.AsSqlServer().OptionRecompile().ToList();
					}
				}
				""",
				SqlServerKey);
		}

		[Test]
		public async Task TablesInScopeHintOnTableAddsAsQueryableAndSystemLinq()
		{
			await VerifyAsync(
				"""
				using LinqToDB;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.[|TablesInScopeHint("NOLOCK")|];
					}
				}
				""",
				"""
				using System.Linq;
				using LinqToDB;
				using LinqToDB.DataProvider.SqlServer;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.AsQueryable().AsSqlServer().WithNoLockInScope();
					}
				}
				""",
				SqlServerKey);
		}

		[Test]
		public async Task StaticCallForm()
		{
			await VerifyAsync(
				"""
				using LinqToDB;
				using LinqToDB.DataProvider.SqlServer;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = LinqExtensions.[|TableHint(t, "NOLOCK")|];
					}
				}
				""",
				"""
				using LinqToDB;
				using LinqToDB.DataProvider.SqlServer;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.AsSqlServer().WithNoLock();
					}
				}
				""",
				SqlServerKey);
		}

		[Test]
		public async Task NamespaceImportedInsideNamespaceIsNotAddedAgain()
		{
			await VerifyAsync(
				"""
				using LinqToDB;

				namespace App
				{
					using LinqToDB.DataProvider.SqlServer;

					class Row { }

					class C
					{
						void M(ITable<Row> t)
						{
							var r = t.[|TableHint("NOLOCK")|];
						}
					}
				}
				""",
				"""
				using LinqToDB;

				namespace App
				{
					using LinqToDB.DataProvider.SqlServer;

					class Row { }

					class C
					{
						void M(ITable<Row> t)
						{
							var r = t.AsSqlServer().WithNoLock();
						}
					}
				}
				""",
				SqlServerKey);
		}
	}
}
