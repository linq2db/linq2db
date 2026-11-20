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
		static Task VerifyAsync(string source, string fixedSource, string? equivalenceKey, Action<CSharpCodeFixTest<StringHintAnalyzer, StringHintCodeFixProvider, DefaultVerifier>>? configure = null)
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
	
		// A generic overload returns ITable<T> / IQueryable<T>, the typed helper the provider's type. A var local written
		// again after its declaration keeps the original type, or the later assignment would no longer compile.
		[Test]
		public async Task ReassignedVarLocalKeepsTheQueryType()
		{
			await VerifyAsync(
				"""
				using System.Linq;

				using LinqToDB;

				class Row { public int Id { get; set; } }

				class C
				{
					void M(IQueryable<Row> source, bool f)
					{
						var q = source.[|QueryHint("RECOMPILE")|];

						if (f)
							q = q.Where(x => x.Id > 0);
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
					void M(IQueryable<Row> source, bool f)
					{
						IQueryable<Row> q = source.AsSqlServer().OptionRecompile();

						if (f)
							q = q.Where(x => x.Id > 0);
					}
				}
				""",
				SqlServerRestrictedKey);
		}

		[Test]
		public async Task ReassignedVarLocalKeepsTheTableType()
		{
			await VerifyAsync(
				"""
				using LinqToDB;

				class Row { }

				class C
				{
					void M(ITable<Row> t, ITable<Row> other)
					{
						var r = (t.[|TableHint("NOLOCK")|]);
						Use(ref r);
						r = other;
					}

					static void Use(ref ITable<Row> table)
					{
					}
				}
				""",
				"""
				using LinqToDB;
				using LinqToDB.DataProvider.SqlServer;

				class Row { }

				class C
				{
					void M(ITable<Row> t, ITable<Row> other)
					{
						ITable<Row> r = (t.AsSqlServer().WithNoLock());
						Use(ref r);
						r = other;
					}

					static void Use(ref ITable<Row> table)
					{
					}
				}
				""",
				SqlServerRestrictedKey);
		}

		// The exact rewrite keeps the provider's type, so nothing changes around it.
		[Test]
		public async Task ReassignedVarLocalOfAnExactRewriteStaysVar()
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
						var r = t.AsSqlServer().[|TableHint("NOLOCK")|];
						r = t.AsSqlServer();
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
						r = t.AsSqlServer();
					}
				}
				""",
				SqlServerKey);
		}

		// An anonymous element type cannot be written out, so no restricting rewrite is offered for a reassigned local.
		[Test]
		public async Task ReassignedVarLocalOfAnAnonymousTypeGetsNoRestrictingRewrite()
		{
			const string source = """
				using System.Linq;

				using LinqToDB;

				class Row { public int Id { get; set; } }

				class C
				{
					void M(IQueryable<Row> source)
					{
						var q = source.Select(x => new { x.Id }).[|QueryHint("RECOMPILE")|];
						q = q.Where(x => x.Id > 0);
					}
				}
				""";

			await VerifyAsync(source, source, SqlServerRestrictedKey);
		}
	
		// The SDK writes implicit and <Using> imports to GlobalUsings.g.cs as `global using global::X;`.
		[Test]
		public async Task SdkGlobalUsingsWithTheGlobalAliasAreNotAddedAgain()
		{
			const string globalUsings = """
				global using global::System.Linq;
				global using global::LinqToDB.DataProvider.SqlServer;
				""";

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
				using LinqToDB;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.AsQueryable().AsSqlServer().WithNoLockInScope();
					}
				}
				""",
				SqlServerRestrictedKey,
				test =>
				{
					test.TestState.Sources.Add(("/0/GlobalUsings.g.cs", globalUsings));
					test.FixedState.Sources.Add(("/0/GlobalUsings.g.cs", globalUsings));
				});
		}

		[Test]
		public async Task LocalUsingWithTheGlobalAliasIsNotAddedAgain()
		{
			await VerifyAsync(
				"""
				using global::System.Linq;

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
				using global::System.Linq;

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
	
		// With no using in the file, the new directive goes below the file header: a #define after it would not compile.
		[Test]
		public async Task FirstUsingGoesBelowTheFileHeader()
		{
			await VerifyAsync(
				"""
				// File header.
				#define LOCAL_SYMBOL

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
				// File header.
				#define LOCAL_SYMBOL

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
				SqlServerRestrictedKey,
				static test =>
				{
					test.TestState.Sources.Add(("/0/GlobalUsings.cs", "global using LinqToDB;"));
					test.FixedState.Sources.Add(("/0/GlobalUsings.cs", "global using LinqToDB;"));
				});
		}

		// Usings grouped by blank lines: a System namespace joins the System group, not the group after it.
		[Test]
		public async Task NewUsingJoinsItsOwnGroup()
		{
			await VerifyAsync(
				"""
				using System;

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
				using System;
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
	
		// A doc comment on the first declaration stays on it.
		[Test]
		public async Task FirstUsingGoesAboveTheFirstDeclarationsDocComment()
		{
			await VerifyAsync(
				"""
				/// <summary>A row.</summary>
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
				using LinqToDB.DataProvider.SqlServer;

				/// <summary>A row.</summary>
				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.AsSqlServer().WithNoLock();
					}
				}
				""",
				SqlServerRestrictedKey,
				static test =>
				{
					test.TestState.Sources.Add(("/0/GlobalUsings.cs", "global using LinqToDB;"));
					test.FixedState.Sources.Add(("/0/GlobalUsings.cs", "global using LinqToDB;"));
				});
		}
	
		// Fixed one after the other, the second hint already sits on the provider's receiver after the first rewrite, so
		// it binds the provider's own overload and the exact rewrite adds no second AsSqlServer().
		[Test]
		public async Task ChainedHintsFixedOneAfterAnotherShareOneAsCall()
		{
			await VerifyAsync(
				"""
				using LinqToDB;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.[|TableHint("NOLOCK")|].[|TableHint("READPAST")|];
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
						var r = t.AsSqlServer().WithNoLock().WithReadPast();
					}
				}
				""",
				null,
				static test =>
				{
					// The first action of each step: SQL Server's restricting rewrite, then the exact one.
					test.CodeActionIndex               = 0;
					test.NumberOfIncrementalIterations = 2;
					test.CodeFixTestBehaviors         |= CodeFixTestBehaviors.SkipFixAllCheck;
				});
		}
	}
}
