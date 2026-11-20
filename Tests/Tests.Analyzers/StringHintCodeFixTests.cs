using System;
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
		// An exact rewrite (a provider's own string overload) and a restricting one (a generic overload) have different
		// keys, so Fix-All started from one never applies the other.
		const string SqlServerKey           = StringHintAnalyzer.DiagnosticId + "|SQL Server";
		const string SqlServerRestrictedKey = StringHintAnalyzer.DiagnosticId + "|SQL Server|restricted";
		const string SqlCeRestrictedKey     = StringHintAnalyzer.DiagnosticId + "|SQL Server CE|restricted";

		// The fix is chosen by its equivalence key (one per provider), so a generic receiver's provider choice is explicit.
		static Task VerifyAsync(string source, string fixedSource, string equivalenceKey, Action<CSharpCodeFixTest<StringHintAnalyzer, StringHintCodeFixProvider, DefaultVerifier>>? configure = null)
		{
			var test = new CSharpCodeFixTest<StringHintAnalyzer, StringHintCodeFixProvider, DefaultVerifier>
			{
				TestCode                  = source,
				FixedCode                 = fixedSource,
				ReferenceAssemblies       = TestReferenceAssemblies.Default,
				CodeActionEquivalenceKey  = equivalenceKey,
			};

			var reference = MetadataReference.CreateFromFile(typeof(Sql).Assembly.Location);
			test.TestState.AdditionalReferences.Add(reference);
			test.FixedState.AdditionalReferences.Add(reference);
			test.BatchFixedState.AdditionalReferences.Add(reference);

			configure?.Invoke(test);

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
				SqlServerRestrictedKey);
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
				SqlCeRestrictedKey);
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
				SqlServerRestrictedKey);
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
				SqlServerRestrictedKey);
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
				SqlServerRestrictedKey);
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
				SqlServerRestrictedKey);
		}
	

		[Test]
		public async Task NamedReorderedArgumentsUseTheBoundReceiver()
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
						var r = LinqExtensions.[|TableHint(hint: "NOLOCK", table: t)|];
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
				SqlServerRestrictedKey);
		}

		[Test]
		public async Task GenericWithOnSqlServerReceiverOffersSqlCeToo()
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
						var r = t.AsSqlServer().[|With("NOLOCK")|];
					}
				}
				""",
				"""
				using LinqToDB;
				using LinqToDB.DataProvider.SqlCe;
				using LinqToDB.DataProvider.SqlServer;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.AsSqlServer().AsSqlCe().WithNoLock();
					}
				}
				""",
				SqlCeRestrictedKey);
		}

		[Test]
		public async Task GenericWithOnSqlServerReceiverSqlServerChoice()
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
						var r = t.AsSqlServer().[|With("NOLOCK")|];
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
				SqlServerRestrictedKey);
		}

		[Test]
		public async Task NewUsingGoesAfterGlobalUsings()
		{
			await VerifyAsync(
				"""
				global using System.Collections.Generic;
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
				global using System.Collections.Generic;
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
				SqlServerRestrictedKey);
		}
	
		[Test]
		public async Task FixAllFromAnExactRewriteLeavesGenericCallsAlone()
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
						var a = t.AsSqlServer().[|TableHint("NOLOCK")|];
						var b = t.[|With("NOLOCK")|];
						var c = t.AsSqlServer().[|TableHint("NOLOCK")|];
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
						var a = t.AsSqlServer().WithNoLock();
						var b = t.[|With("NOLOCK")|];
						var c = t.AsSqlServer().WithNoLock();
					}
				}
				""",
				SqlServerKey,
				static test =>
				{
					test.NumberOfIncrementalIterations = 2;
					test.NumberOfFixAllIterations      = 1;

					// The call the Fix-All leaves alone is still reported.
					test.FixedState.MarkupHandling = MarkupMode.Allow;
				});
		}

		[Test]
		public async Task FixAllFromARestrictingRewriteLeavesExactCallsAlone()
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
						var a = t.[|TableHint("NOLOCK")|];
						var b = t.AsSqlServer().[|TableHint("NOLOCK")|];
						var c = t.[|With("NOLOCK")|];
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
						var a = t.AsSqlServer().WithNoLock();
						var b = t.AsSqlServer().[|TableHint("NOLOCK")|];
						var c = t.AsSqlServer().WithNoLock();
					}
				}
				""",
				SqlServerRestrictedKey,
				static test =>
				{
					test.NumberOfIncrementalIterations = 2;
					test.NumberOfFixAllIterations      = 1;

					// The call the Fix-All leaves alone is still reported.
					test.FixedState.MarkupHandling = MarkupMode.Allow;
				});
		}
	}
}
