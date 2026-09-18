using System.Collections.Immutable;

using LinqToDB.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeGenerators
{
	/// <summary>
	/// Checks that a member's server-side-only <i>declaration</i> agrees with its <i>implementation</i>.
	/// A member is declared server-side-only by <c>[ServerSideOnly]</c>, by an <c>Sql.Expression</c>-derived
	/// attribute whose <c>ServerSideOnly</c> is effectively true (every <c>Sql.Extension</c> constructor sets
	/// it), by an <c>Sql.TableFunction</c>-derived attribute, or by <c>[ExpressionMethod]</c>; and by
	/// convention its body is <c>=&gt; throw new ServerSideOnlyException(nameof(X))</c>. Two rules:
	/// <list type="bullet">
	/// <item><c>LINQ2DB0002</c> - a throw-only stub that declares none of those.</item>
	/// <item><c>LINQ2DB0003</c> - a declared member whose stub throws something else.</item>
	/// </list>
	/// The detection itself lives in <see cref="ServerSideOnlyContract"/>, linked in from
	/// <c>Source/LinqToDB.Analyzers</c> so the shipped <c>L2DB1003</c>/<c>L2DB1004</c> decide identical cases.
	/// </summary>
	/// <remarks>
	/// The exception-type sets are hardcoded here and configurable on the shipped twin: an internal rule's
	/// audience is this repo, so a knob would only let a later change silence a deliberate decision.
	/// </remarks>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class ServerSideOnlyContractAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor MissingMarkerRule = new(
			id:                 "LINQ2DB0002",
			title:              "Declare a server-side-only stub, or implement it",
			messageFormat:      "'{0}' is a throw-only stub but nothing declares it server-side-only: add [ServerSideOnly], set ServerSideOnly = true on its Sql.* attribute, or give it a real implementation",
			category:           "Usage",
			defaultSeverity:    DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description:        "A member whose whole body throws can still be picked for client-side evaluation unless something declares it server-side-only, in which case the call fails at runtime instead of translating.");

		static readonly DiagnosticDescriptor WrongExceptionRule = new(
			id:                 "LINQ2DB0003",
			title:              "A server-side-only stub should throw ServerSideOnlyException",
			messageFormat:      "'{0}' is declared server-side-only but its stub throws {1}: throw new ServerSideOnlyException(nameof({0})) so a client-side call names the API",
			category:           "Usage",
			defaultSeverity:    DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description:        "ServerSideOnlyException reports which API was called on the client. Any other exception - NotImplementedException in particular - tells the caller nothing about why the call could not run.");

		/// <inheritdoc/>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [MissingMarkerRule, WrongExceptionRule];

		/// <inheritdoc/>
		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();

			// Analyze generated code, unlike the shipped twin: Sql.Row.generated.cs carries ten of the
			// defects these rules exist for, and linq2db's generated files are checked in and maintained
			// through their .tt. A consumer's are not, so LinqToDB.Analyzers passes None.
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction(static startContext =>
			{
				var symbols = ServerSideOnlyContract.Symbols.TryCreate(startContext.Compilation);

				if (symbols is null)
					return;

				// Hardcoded, unlike the shipped twin: ServerSideOnlyException is the only type that both
				// evidences intent on an unmarked stub and is acceptable inside a marked one.
				var serverSideOnly = ImmutableArray.Create(symbols.ServerSideOnlyException);
				var options        = new ServerSideOnlyContract.Options(serverSideOnly, serverSideOnly);

				startContext.RegisterOperationBlockAction(blockContext =>
				{
					if (!ServerSideOnlyContract.TryGetStub(blockContext.OwningSymbol, blockContext.OperationBlocks, out var member, out var thrownType)
						|| member is null
						|| thrownType is null)
					{
						return;
					}

					var violation = ServerSideOnlyContract.Classify(member, thrownType, symbols, options);

					if (violation == ServerSideOnlyContract.Violation.None)
						return;

					var location = member.Locations.Length > 0 ? member.Locations[0] : Location.None;

					if (violation == ServerSideOnlyContract.Violation.MissingMarker)
						blockContext.ReportDiagnostic(Diagnostic.Create(MissingMarkerRule, location, member.Name));
					else
						blockContext.ReportDiagnostic(Diagnostic.Create(WrongExceptionRule, location, member.Name, thrownType.Name));
				});
			});
		}
	}
}
