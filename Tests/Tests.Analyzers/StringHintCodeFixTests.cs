using System;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Analyzers;
using LinqToDB.Analyzers.CodeFixes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

		// Each occurrence adds the same using; Fix-All merges them into one.
		[Test]
		public async Task FixAllAddsTheUsingOnce()
		{
			await VerifyAsync(
				"""
				using LinqToDB;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var a = t.[|TableHint("NOLOCK")|];
						var b = t.[|With("READPAST")|];
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
						var b = t.AsSqlServer().WithReadPast();
					}
				}
				""",
				SqlServerRestrictedKey,
				static test =>
				{
					test.NumberOfIncrementalIterations = 2;
					test.NumberOfFixAllIterations      = 1;
				});
		}

		// A conditional block around the declarations is not part of the file header: a using inside it would exist in
		// one configuration only.
		[Test]
		public async Task FirstUsingGoesAboveAConditionalBlock()
		{
			await VerifyAsync(
				"""
				#if DEBUG
				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.[|TableHint("NOLOCK")|];
					}
				}
				#endif
				""",
				"""
				using LinqToDB.DataProvider.SqlServer;

				#if DEBUG
				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.AsSqlServer().WithNoLock();
					}
				}
				#endif
				""",
				SqlServerRestrictedKey,
				static test =>
				{
					test.TestState.Sources.Add(("/0/GlobalUsings.cs", "global using LinqToDB;"));
					test.FixedState.Sources.Add(("/0/GlobalUsings.cs", "global using LinqToDB;"));
					test.SolutionTransforms.Add(static (solution, projectId) =>
					{
						var options = (CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!;

						return solution.WithProjectParseOptions(projectId, options.WithPreprocessorSymbols("DEBUG"));
					});
				});
		}

		// A conditional block closed before the first declaration is part of the header; the one still open after it is not.
		[Test]
		public async Task ConditionalBlockClosedInTheHeaderStaysInIt()
		{
			await VerifyAsync(
				"""
				#if NET
				#define MODERN
				#endif

				#if DEBUG
				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.[|TableHint("NOLOCK")|];
					}
				}
				#endif
				""",
				"""
				#if NET
				#define MODERN
				#endif

				using LinqToDB.DataProvider.SqlServer;

				#if DEBUG
				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.AsSqlServer().WithNoLock();
					}
				}
				#endif
				""",
				SqlServerRestrictedKey,
				static test =>
				{
					test.TestState.Sources.Add(("/0/GlobalUsings.cs", "global using LinqToDB;"));
					test.FixedState.Sources.Add(("/0/GlobalUsings.cs", "global using LinqToDB;"));
					test.SolutionTransforms.Add(static (solution, projectId) =>
					{
						var options = (CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!;

						return solution.WithProjectParseOptions(projectId, options.WithPreprocessorSymbols("DEBUG"));
					});
				});
		}

		// Usings grouped by their first namespace segment: the new directive joins the LinqToDB group, and the group
		// after it keeps its blank line.
		[Test]
		public async Task NewUsingJoinsItsOwnFirstSegmentGroup()
		{
			await VerifyAsync(
				"""
				using LinqToDB;
				using LinqToDB.Data;

				using Microsoft.Extensions.Logging;

				class Row { }

				class C
				{
					void M(ITable<Row> t, DataConnection db, Logger logger)
					{
						var r = t.[|TableHint("NOLOCK")|];
					}
				}

				namespace Microsoft.Extensions.Logging
				{
					class Logger { }
				}
				""",
				"""
				using LinqToDB;
				using LinqToDB.Data;
				using LinqToDB.DataProvider.SqlServer;

				using Microsoft.Extensions.Logging;

				class Row { }

				class C
				{
					void M(ITable<Row> t, DataConnection db, Logger logger)
					{
						var r = t.AsSqlServer().WithNoLock();
					}
				}

				namespace Microsoft.Extensions.Logging
				{
					class Logger { }
				}
				""",
				SqlServerRestrictedKey);
		}

		// The rewrite gives the chain the provider's type, so a later text hint binds the provider's own overload and is
		// emitted on that provider only; the title says so.
		[Test]
		public async Task RestrictingRewriteRestrictsTheTextHintsChainedAfterIt()
		{
			await VerifyAsync(
				"""
				using LinqToDB;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var r = t.[|TableHint("NOLOCK")|].TableHint("INDEX(IX_PROBE)");
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
						var r = t.AsSqlServer().WithNoLock().TableHint("INDEX(IX_PROBE)");
					}
				}
				""",
				SqlServerRestrictedKey,
				static test =>
				{
					test.CodeActionVerifier = static (action, verifier) =>
						verifier.Equal("Use SQL Server typed hint WithNoLock() (applies to SQL Server only, as do text hints chained after it)", action.Title);
				});
		}

		// A conditional with no target type takes the common type of its branches; the provider's type has none with a
		// table, so the var local the conditional reads keeps the original type.
		[Test]
		public async Task VarLocalReadByAConditionalKeepsTheQueryType()
		{
			await VerifyAsync(
				"""
				using System.Linq;

				using LinqToDB;

				class Row { }

				class C
				{
					void M(IQueryable<Row> source, ITable<Row> t, bool f)
					{
						var q = source.[|QueryHint("RECOMPILE")|];
						var r = f ? q : t;
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
					void M(IQueryable<Row> source, ITable<Row> t, bool f)
					{
						IQueryable<Row> q = source.AsSqlServer().OptionRecompile();
						var r = f ? q : t;
					}
				}
				""",
				SqlServerRestrictedKey);
		}

		// The call is a branch itself: no local can keep the type, so no restricting rewrite is offered.
		[Test]
		public async Task ConditionalBranchWithNoTargetTypeGetsNoRestrictingRewrite()
		{
			const string source = """
				using System.Linq;

				using LinqToDB;

				class Row { }

				class C
				{
					void M(IQueryable<Row> source, ITable<Row> t, bool f)
					{
						var r = f ? source.[|QueryHint("RECOMPILE")|] : t;
					}
				}
				""";

			await VerifyAsync(source, source, SqlServerRestrictedKey);
		}

		// Fix-All rewrites a document's hints in one pass: the outer hint's receiver is the inner rewrite, which already has
		// the provider's type, so the chain gets one AsSqlServer(). Fixed one at a time, the second hint becomes an exact
		// rewrite (ChainedHintsFixedOneAfterAnotherShareOneAsCall).
		[TestCase("t.[|TableHint(\"NOLOCK\")|].[|TableHint(\"READPAST\")|]", "t.AsSqlServer().WithNoLock().[|TableHint(\"READPAST\")|]", "t.AsSqlServer().WithNoLock().WithReadPast()")]
		[TestCase("t\n\t\t\t.[|TableHint(\"NOLOCK\")|]\n\t\t\t.[|TableHint(\"READPAST\")|]", "t\n\t\t\t.AsSqlServer().WithNoLock()\n\t\t\t.[|TableHint(\"READPAST\")|]", "t\n\t\t\t.AsSqlServer().WithNoLock()\n\t\t\t.WithReadPast()")]
		public async Task FixAllOnChainedHintsSharesOneAsCall(string chain, string fixedOnce, string fixedAll)
		{
			static string Source(string expression, bool withUsing)
			{
				return (withUsing ? "using LinqToDB;\nusing LinqToDB.DataProvider.SqlServer;\n" : "using LinqToDB;\n")
					+ "\nclass Row { }\n\nclass C\n{\n\tvoid M(ITable<Row> t)\n\t{\n\t\tvar r = " + expression + ";\n\t}\n}\n";
			}

			await VerifyAsync(
				Source(chain, withUsing: false),
				Source(fixedOnce, withUsing: true),
				SqlServerRestrictedKey,
				test =>
				{
					// One restricting fix; the second hint then binds the provider's own overload (an exact rewrite).
					test.NumberOfIncrementalIterations = 1;
					test.FixedState.MarkupHandling     = MarkupMode.Allow;
					test.BatchFixedCode                = Source(fixedAll, withUsing: true);
					test.NumberOfFixAllIterations      = 1;
				});
		}

		// The chain initializes a var local that is written later: the local keeps the chain's original type.
		[Test]
		public async Task ChainedInitializerWrittenLaterKeepsTheTableType()
		{
			await VerifyAsync(
				"""
				using LinqToDB;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var q = t.[|TableHint("NOLOCK")|].TableHint("INDEX(IX_1)");
						q = t;
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
						ITable<Row> q = t.AsSqlServer().WithNoLock().TableHint("INDEX(IX_1)");
						q = t;
					}
				}
				""",
				SqlServerRestrictedKey);
		}

		// A generic method infers its type argument from the call: no restricting rewrite. A chain that ends in another
		// type (ToList()) passes the same type either way, so the rewrite is offered there.
		[Test]
		public async Task GenericArgumentGetsNoRestrictingRewriteUnlessTheChainChangesType()
		{
			await VerifyAsync(
				"""
				using System.Collections.Generic;
				using System.Linq;

				using LinqToDB;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var a = Pick(t.[|TableHint("NOLOCK")|]);
						var b = Pick(t.[|TableHint("READPAST")|].ToList());
					}

					static T Pick<T>(T value)
					{
						return value;
					}
				}
				""",
				"""
				using System.Collections.Generic;
				using System.Linq;

				using LinqToDB;
				using LinqToDB.DataProvider.SqlServer;

				class Row { }

				class C
				{
					void M(ITable<Row> t)
					{
						var a = Pick(t.[|TableHint("NOLOCK")|]);
						var b = Pick(t.AsSqlServer().WithReadPast().ToList());
					}

					static T Pick<T>(T value)
					{
						return value;
					}
				}
				""",
				SqlServerRestrictedKey,
				static test =>
				{
					test.FixedState.MarkupHandling = MarkupMode.Allow;
				});
		}

		// A #define inside the conditional block that encloses the declarations must precede every using (CS1032), so the
		// using goes after it, inside the block.
		[Test]
		public async Task DefineInsideAnOpenConditionalBlockStaysAboveTheUsing()
		{
			await VerifyAsync(
				"""
				#if DEBUG
				#define MODERN
				class Row { }

				class C
				{
					void M(LinqToDB.ITable<Row> t)
					{
						var r = t.[|TableHint("NOLOCK")|];
					}
				}
				#endif
				""",
				"""
				#if DEBUG
				#define MODERN
				using LinqToDB.DataProvider.SqlServer;

				class Row { }

				class C
				{
					void M(LinqToDB.ITable<Row> t)
					{
						var r = t.AsSqlServer().WithNoLock();
					}
				}
				#endif
				""",
				SqlServerRestrictedKey,
				static test =>
				{
					DefineDebug(test);
					AddGlobalLinqToDBUsing(test);
				});
		}

		// Existing usings inside a conditional block: the new one goes after the block's #endif.
		[Test]
		public async Task NewUsingGoesAfterAConditionalBlockOfUsings()
		{
			await VerifyAsync(
				"""
				using System;
				#if DEBUG
				using LinqToDB;
				#endif

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
				using System;
				#if DEBUG
				using LinqToDB;
				#endif
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
				DefineDebug);
		}

		// The sorted place is inside a conditional block in the middle of the usings: the new using goes in front of it.
		[Test]
		public async Task NewUsingStaysOutOfAConditionalBlockInTheMiddle()
		{
			await VerifyAsync(
				"""
				using System;
				#if DEBUG
				using LinqToDB;
				using LinqToDB.Linq;
				#endif
				using LinqToDB.Mapping;

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
				using System;
				using LinqToDB.DataProvider.SqlServer;
				#if DEBUG
				using LinqToDB;
				using LinqToDB.Linq;
				#endif
				using LinqToDB.Mapping;

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
				DefineDebug);
		}

		// A comment right above the #if that encloses the declarations stays with the header, above the new using.
		[Test]
		public async Task CommentAboveAnOpenConditionalBlockStaysInTheHeader()
		{
			await VerifyAsync(
				"""
				// header
				#if NOPE
				class Other { }
				#else
				class Row { }

				class C
				{
					void M(LinqToDB.ITable<Row> t)
					{
						var r = t.[|TableHint("NOLOCK")|];
					}
				}
				#endif
				""",
				"""
				// header
				using LinqToDB.DataProvider.SqlServer;

				#if NOPE
				class Other { }
				#else
				class Row { }

				class C
				{
					void M(LinqToDB.ITable<Row> t)
					{
						var r = t.AsSqlServer().WithNoLock();
					}
				}
				#endif
				""",
				SqlServerRestrictedKey,
				AddGlobalLinqToDBUsing);
		}

		// The chain after `?.` initializes a var local written later: the local keeps the original type.
		[Test]
		public async Task ConditionalAccessVarLocalWrittenLaterKeepsTheQueryType()
		{
			await VerifyAsync(
				"""
				using System.Linq;

				using LinqToDB;

				class Row { public int Id; }

				class C
				{
					void M(IQueryable<Row> src)
					{
						var q = src?.Where(r => r.Id > 0).[|QueryHint("RECOMPILE")|];
						q = src;
					}
				}
				""",
				"""
				using System.Linq;

				using LinqToDB;
				using LinqToDB.DataProvider.SqlServer;

				class Row { public int Id; }

				class C
				{
					void M(IQueryable<Row> src)
					{
						IQueryable<Row> q = src?.Where(r => r.Id > 0).AsSqlServer().OptionRecompile();
						q = src;
					}
				}
				""",
				SqlServerRestrictedKey);
		}

		// The receiver of an extension method typed as a bare type parameter, and a value matched by `is var x`, take
		// the provider's type: no restricting rewrite.
		[Test]
		public async Task GenericReceiverAndVarPatternGetNoRestrictingRewrite()
		{
			const string source = """
				using System.Collections.Generic;

				using LinqToDB;

				class Row { }

				static class Ext
				{
					public static List<T> Wrap<T>(this T value)
					{
						return new List<T> { value };
					}
				}

				class C
				{
					void M(ITable<Row> t)
					{
						var l = t.[|TableHint("NOLOCK")|].Wrap();
						l.Add(t);

						if (t.[|TableHint("READPAST")|] is var x)
							x = t;
					}
				}
				""";

			await VerifyAsync(source, source, SqlServerRestrictedKey);
		}

		// Join's inner sequence and SelectMany's collection selector infer the element type from IEnumerable<T>, which the
		// provider's type keeps: the rewrite is offered.
		[Test]
		public async Task LinqOperatorArgumentsAreRewritten()
		{
			await VerifyAsync(
				"""
				using System.Linq;

				using LinqToDB;

				class Row { public int Id; }

				class C
				{
					void M(IQueryable<Row> src, ITable<Row> t)
					{
						var j = src.Join(t.[|TableHint("NOLOCK")|], a => a.Id, b => b.Id, (a, b) => a);
						var s = src.SelectMany(a => t.[|TableHint("READPAST")|], (a, b) => b);
					}
				}
				""",
				"""
				using System.Linq;

				using LinqToDB;
				using LinqToDB.DataProvider.SqlServer;

				class Row { public int Id; }

				class C
				{
					void M(IQueryable<Row> src, ITable<Row> t)
					{
						var j = src.Join(t.AsSqlServer().WithNoLock(), a => a.Id, b => b.Id, (a, b) => a);
						var s = src.SelectMany(a => t.AsSqlServer().WithReadPast(), (a, b) => b);
					}
				}
				""",
				SqlServerRestrictedKey,
				static test =>
				{
					test.NumberOfIncrementalIterations = 2;
				});
		}

		static void AddGlobalLinqToDBUsing(CSharpCodeFixTest<StringHintAnalyzer, StringHintCodeFixProvider, DefaultVerifier> test)
		{
			test.TestState.Sources.Add(("/0/GlobalUsings.cs", "global using LinqToDB;"));
			test.FixedState.Sources.Add(("/0/GlobalUsings.cs", "global using LinqToDB;"));
		}

		static void DefineDebug(CSharpCodeFixTest<StringHintAnalyzer, StringHintCodeFixProvider, DefaultVerifier> test)
		{
			test.SolutionTransforms.Add(static (solution, projectId) =>
			{
				var options = (CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!;

				return solution.WithProjectParseOptions(projectId, options.WithPreprocessorSymbols("DEBUG"));
			});
		}
	}
}
