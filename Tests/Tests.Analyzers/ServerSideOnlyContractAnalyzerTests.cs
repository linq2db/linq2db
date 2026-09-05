using System.Threading.Tasks;

using LinqToDB.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using NUnit.Framework;

using Verify = Tests.Analyzers.AnalyzerVerifier<LinqToDB.Analyzers.ServerSideOnlyContractAnalyzer>;

namespace Tests.Analyzers
{
	[TestFixture]
	public sealed class ServerSideOnlyContractAnalyzerTests
	{
		const string Usings = """
			using System;
			using LinqToDB;
			using LinqToDB.Mapping;

			""";

		static DiagnosticResult MissingMarker(string member) =>
			new DiagnosticResult(ServerSideOnlyContractAnalyzer.MissingMarkerDiagnosticId, DiagnosticSeverity.Info)
				.WithLocation(0)
				.WithArguments(member);

		// Arm A carries the marker-capable attribute's location as an additional location, so the code fix can
		// reach it when it is declared on an implemented interface member rather than on the reported one. That
		// makes it part of the diagnostic's observable shape, which an explicit DiagnosticResult must declare.
		static DiagnosticResult MissingMarkerOnAttributed(string member) =>
			new DiagnosticResult(ServerSideOnlyContractAnalyzer.MissingMarkerDiagnosticId, DiagnosticSeverity.Info)
				.WithLocation(0)
				.WithLocation(1)
				.WithArguments(member);

		static DiagnosticResult WrongException(string member, string thrown) =>
			new DiagnosticResult(ServerSideOnlyContractAnalyzer.WrongExceptionDiagnosticId, DiagnosticSeverity.Info)
				.WithLocation(0)
				.WithArguments(member, thrown);

		#region Body shapes

		// Roslyn models `=> throw` as Block -> Return -> Conversion -> Throw, and `{ throw; }` as
		// Block -> Throw. An implementation that handled only one of them reported 1 of 19 real defects in
		// Source/LinqToDB, every miss being expression-bodied. These two must stay in lockstep.
		[Test]
		public Task ReportsExpressionBodiedStub()
		{
			var source = Usings + """
				static class C
				{
					public static int {|#0:M|}() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Verify.VerifyAsync(source, MissingMarker("M"));
		}

		[Test]
		public Task ReportsBlockBodiedStub()
		{
			var source = Usings + """
				static class C
				{
					public static int {|#0:M|}()
					{
						throw new ServerSideOnlyException(nameof(M));
					}
				}
				""";

			return Verify.VerifyAsync(source, MissingMarker("M"));
		}

		#endregion

		#region Arm B - no marker-capable attribute, so the thrown type is the only evidence

		[Test]
		public Task DoesNotReportUnmarkedStubThrowingNotImplementedException()
		{
			// The default IDE stub. Tests/Linq alone carries ~176 of these, so treating them as
			// server-side-only intent would make the rule unusable on any real project.
			var source = Usings + """
				static class C
				{
					public static int M() => throw new NotImplementedException();
				}
				""";

			return Verify.VerifyAsync(source);
		}

		[Test]
		public Task DoesNotReportSwitchArmThrow()
		{
			var source = Usings + """
				static class C
				{
					public static int M(int x) => x switch
					{
						1 => 1,
						_ => throw new ServerSideOnlyException(nameof(M)),
					};
				}
				""";

			return Verify.VerifyAsync(source);
		}

		#endregion

		#region Arm A - an attribute declares the member translatable, so any exception counts

		[Test]
		public Task ReportsAttributedStubWithServerSideOnlyUnset()
		{
			var source = Usings + """
				static class C
				{
					[{|#1:Sql.Function("F")|}]
					public static int {|#0:M|}() => throw new NotImplementedException();
				}
				""";

			return Verify.VerifyAsync(source, MissingMarkerOnAttributed("M"));
		}

		[Test]
		public Task ReportsAttributedStubWithServerSideOnlyExplicitlyFalse()
		{
			var source = Usings + """
				static class C
				{
					[{|#1:Sql.Function("F", ServerSideOnly = false)|}]
					public static int {|#0:M|}() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Verify.VerifyAsync(source, MissingMarkerOnAttributed("M"));
		}

		#endregion

		#region Marker forms

		[Test]
		public Task DoesNotReportBareSqlExtensionStub()
		{
			// Every Sql.ExtensionAttribute ctor sets ServerSideOnly = true, so this is marked with nothing
			// written. Reading only the named arguments would report correct user code here.
			var source = Usings + """
				static class C
				{
					[Sql.Extension("F")]
					public static int M() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Verify.VerifyAsync(source);
		}

		[Test]
		public Task DoesNotReportTableExpressionStub()
		{
			// Marker form 3, and the only one on its own branch: TableFunctionAttribute derives from
			// MappingAttribute, not from Sql.ExpressionAttribute, so no other fixture reaches that clause.
			// SqlFn's two OpenJson overloads are the in-repo shape a forms-1-2-only model would report.
			var source = Usings + """
				using System.Collections.Generic;

				static class C
				{
					[Sql.TableExpression("F")]
					public static IEnumerable<int> M() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Verify.VerifyAsync(source);
		}

		[Test]
		public Task DoesNotReportServerSideOnlyAttributeStub()
		{
			var source = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int M() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Verify.VerifyAsync(source);
		}

		[Test]
		public Task DoesNotReportExpressionMethodStub()
		{
			var source = Usings + """
				using System.Linq.Expressions;

				static class C
				{
					[ExpressionMethod(nameof(MImpl))]
					public static int M(int x) => throw new ServerSideOnlyException(nameof(M));

					static Expression<Func<int, int>> MImpl() => x => x;
				}
				""";

			return Verify.VerifyAsync(source);
		}

		#endregion

		#region Symbol kinds

		[Test]
		public Task ReportsThrowOnlyPropertyGetter()
		{
			var source = Usings + """
				static class C
				{
					public static int {|#0:P|} => throw new ServerSideOnlyException(nameof(P));
				}
				""";

			return Verify.VerifyAsync(source, MissingMarker("P"));
		}

		[Test]
		public Task DoesNotReportSetterOnlyThrow()
		{
			// A set-only throw beside a real getter is not a stub member.
			var source = Usings + """
				class C
				{
					int _p;

					public int P
					{
						get => _p;
						set => throw new ServerSideOnlyException(nameof(P));
					}
				}
				""";

			return Verify.VerifyAsync(source);
		}

		[Test]
		public Task DoesNotReportThrowingConstructor()
		{
			// [ServerSideOnly] is AttributeTargets.Property | Method, so a fix here could not compile.
			var source = Usings + """
				class C
				{
					public C() => throw new ServerSideOnlyException("C");
				}
				""";

			return Verify.VerifyAsync(source);
		}

		#endregion

		#region Implemented-interface walk

		[Test]
		public Task DoesNotReportImplementationWhenInterfaceCarriesMarker()
		{
			// The Sql.GroupBy shape: the marker lives on the interface because calls bind the interface
			// method, and the implementation inherits it.
			var source = Usings + """
				interface I
				{
					[ServerSideOnly] int M();
				}

				class C : I
				{
					public int M() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Verify.VerifyAsync(source);
		}

		[Test]
		public Task ReportsImplementationWhenInterfaceUnmarked()
		{
			var source = Usings + """
				interface I
				{
					int M();
				}

				class C : I
				{
					public int {|#0:M|}() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Verify.VerifyAsync(source, MissingMarker("M"));
		}

		// The runtime reads mapping attributes up the BASE-CLASS chain as well as the interface chain
		// (MappingAttributesCache.GetMappingAttributesTreeInternal), and it does so by walking types explicitly
		// rather than through reflection inheritance - so ServerSideOnlyAttribute's Inherited = false does not
		// stop it. Verified against the built assembly: GetAttribute<ServerSideOnlyAttribute> on an override
		// whose base carries the marker returns it. The walk here has to match, or correct code is reported.
		[Test]
		public Task DoesNotReportOverrideWhenBaseCarriesMarker()
		{
			var source = Usings + """
				class B
				{
					[ServerSideOnly] public virtual int M() => 0;
				}

				class D : B
				{
					public override int M() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Verify.VerifyAsync(source);
		}

		// An explicit implementation's ISymbol.Name is the dotted `I.M`, so a walk that prefilters on name
		// equality reaches nothing for it - and this symbol kind is deliberately in the rule's scope. These two
		// pin both halves: the marker on the interface must still suppress, and its absence must still report.
		[Test]
		public Task DoesNotReportExplicitImplementationWhenInterfaceCarriesMarker()
		{
			var source = Usings + """
				interface I
				{
					[ServerSideOnly] int M();
				}

				class C : I
				{
					int I.M() => throw new ServerSideOnlyException(nameof(I.M));
				}
				""";

			return Verify.VerifyAsync(source);
		}

		[Test]
		public Task ReportsExplicitImplementationWhenInterfaceUnmarked()
		{
			var source = Usings + """
				interface I
				{
					int M();
				}

				class C : I
				{
					int I.{|#0:M|}() => throw new ServerSideOnlyException(nameof(I.M));
				}
				""";

			return Verify.VerifyAsync(source, MissingMarker("I.M"));
		}

		#endregion

		#region Wrong exception in a marked stub

		[Test]
		public Task ReportsMarkedStubThrowingNotImplementedException()
		{
			var source = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int {|#0:M|}() => throw new NotImplementedException();
				}
				""";

			return Verify.VerifyAsync(source, WrongException("M", "NotImplementedException"));
		}

		[Test]
		public Task ReportsExpressionMethodStubThrowingWrongException()
		{
			// Form 4 imposes the convention on [ExpressionMethod] stubs too - measured, all 35 such stubs
			// in Tests/Linq throw something other than ServerSideOnlyException.
			var source = Usings + """
				using System.Linq.Expressions;

				static class C
				{
					[ExpressionMethod(nameof(MImpl))]
					public static int {|#0:M|}(int x) => throw new NotImplementedException();

					static Expression<Func<int, int>> MImpl() => x => x;
				}
				""";

			return Verify.VerifyAsync(source, WrongException("M", "NotImplementedException"));
		}

		[Test]
		public Task DoesNotReportMarkedStubThrowingServerSideOnlyException()
		{
			var source = Usings + """
				static class C
				{
					[Sql.Extension("F")]
					public static int M() => throw new ServerSideOnlyException(nameof(M));
				}
				""";

			return Verify.VerifyAsync(source);
		}

		#endregion

		#region Options

		[Test]
		public Task UnmarkedStubExceptionTypesIsAdditive()
		{
			// Extending arm B must not drop the default. Under a "replaces" implementation this snippet
			// would report only the NotImplementedException stub, and M2 would go silent.
			const string editorConfig = """
				root = true

				[*.cs]
				linq2db.L2DB1003.unmarked_stub_exception_types = System.NotImplementedException
				""";

			var source = Usings + """
				static class C
				{
					public static int {|#0:M1|}() => throw new NotImplementedException();
					public static int {|#1:M2|}() => throw new ServerSideOnlyException(nameof(M2));
				}
				""";

			var expected = new[]
			{
				new DiagnosticResult(ServerSideOnlyContractAnalyzer.MissingMarkerDiagnosticId, DiagnosticSeverity.Info).WithLocation(0).WithArguments("M1"),
				new DiagnosticResult(ServerSideOnlyContractAnalyzer.MissingMarkerDiagnosticId, DiagnosticSeverity.Info).WithLocation(1).WithArguments("M2"),
			};

			return Verify.VerifyAsync(source, editorConfig, expected);
		}

		[Test]
		public Task AllowedExceptionTypesIsAdditiveAndDoesNotAffectTheOtherRule()
		{
			const string editorConfig = """
				root = true

				[*.cs]
				linq2db.L2DB1004.allowed_exception_types = System.NotImplementedException
				""";

			// M1 - the nominated type is accepted. M2 - the ServerSideOnlyException default is still accepted,
			// so a "replaces" implementation is caught rather than passing quietly. M3 - this list must not
			// widen L2DB1003's arm B, which would report every unattributed placeholder in a consumer project.
			var source = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int M1() => throw new NotImplementedException();

					[ServerSideOnly]
					public static int M2() => throw new ServerSideOnlyException(nameof(M2));

					public static int M3() => throw new NotImplementedException();
				}
				""";

			return Verify.VerifyAsync(source, editorConfig);
		}

		[Test]
		public Task ExceptionTypesMatchExactlyAndNotBySubclass()
		{
			// Listing a base type must not re-admit its whole hierarchy - Exception is one keystroke away.
			const string editorConfig = """
				root = true

				[*.cs]
				linq2db.L2DB1004.allowed_exception_types = System.SystemException
				""";

			var source = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int {|#0:M|}() => throw new NotImplementedException();
				}
				""";

			return Verify.VerifyAsync(source, editorConfig, WrongException("M", "NotImplementedException"));
		}

		[Test]
		public Task UnresolvableOptionEntryDegradesToTheDefault()
		{
			const string editorConfig = """
				root = true

				[*.cs]
				linq2db.L2DB1004.allowed_exception_types = Not.A.Real.Type, , System.NotImplementedException
				""";

			var source = Usings + """
				static class C
				{
					[ServerSideOnly]
					public static int M() => throw new NotImplementedException();
				}
				""";

			return Verify.VerifyAsync(source, editorConfig);
		}

		#endregion
	}
}
