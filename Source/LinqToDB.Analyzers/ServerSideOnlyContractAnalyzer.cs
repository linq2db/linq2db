using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LinqToDB.Analyzers
{
	/// <summary>
	/// Checks that a member's server-side-only <i>declaration</i> agrees with its <i>implementation</i>.
	/// A member is declared server-side-only by <c>[ServerSideOnly]</c>, by an <c>Sql.Expression</c>-derived
	/// attribute whose <c>ServerSideOnly</c> is effectively true (every <c>Sql.Extension</c> constructor sets
	/// it), by an <c>Sql.TableFunction</c>-derived attribute, or by <c>[ExpressionMethod]</c>; and by
	/// convention its body is <c>=&gt; throw new ServerSideOnlyException(nameof(X))</c>.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class ServerSideOnlyContractAnalyzer : DiagnosticAnalyzer
	{
		/// <summary>Diagnostic id for a throw-only stub that declares no server-side-only marker.</summary>
		public const string MissingMarkerDiagnosticId = "L2DB1003";

		/// <summary>Diagnostic id for a server-side-only stub throwing the wrong exception type.</summary>
		public const string WrongExceptionDiagnosticId = "L2DB1004";

		// Public rather than internal because the code fix lives in the sibling LinqToDB.Analyzers.CodeFixes
		// assembly and reads the remedy off Diagnostic.Properties; DiagnosticId is public for the same reason.

		/// <summary>Property key carrying the remedy a code fix should apply.</summary>
		public const string RemedyPropertyKey = "remedy";

		/// <summary>Remedy: add a <c>[ServerSideOnly]</c> attribute to the member.</summary>
		public const string RemedyAddAttribute = "add-attribute";

		/// <summary>Remedy: set <c>ServerSideOnly = true</c> on the member's existing Sql.* attribute.</summary>
		public const string RemedySetNamedArgument = "set-named-argument";

		/// <summary>Remedy: replace the thrown exception with <c>ServerSideOnlyException</c>.</summary>
		public const string RemedyReplaceException = "replace-exception";

		// Roslyn lower-cases .editorconfig keys on parse, so the lookup key must be lower-cased even though
		// the user-facing form keeps the readable rule id.
		static readonly string UnmarkedStubExceptionTypesKey =
			("linq2db." + MissingMarkerDiagnosticId + ".unmarked_stub_exception_types").ToLowerInvariant();

		static readonly string AllowedExceptionTypesKey =
			("linq2db." + WrongExceptionDiagnosticId + ".allowed_exception_types").ToLowerInvariant();

		internal static readonly DiagnosticDescriptor MissingMarkerRule = new(
			id:                 MissingMarkerDiagnosticId,
			title:              "Declare a server-side-only stub, or implement it",
			messageFormat:      "'{0}' is a throw-only stub but nothing declares it server-side-only: add [ServerSideOnly], set ServerSideOnly = true on its Sql.* attribute, or give it a real implementation",
			category:           "LinqToDB",
			defaultSeverity:    DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description:        "A member whose whole body throws can still be picked for client-side evaluation unless something declares it server-side-only, in which case the call fails at runtime instead of translating.",
			helpLinkUri:        "https://github.com/linq2db/linq2db/wiki/" + MissingMarkerDiagnosticId);

		internal static readonly DiagnosticDescriptor WrongExceptionRule = new(
			id:                 WrongExceptionDiagnosticId,
			title:              "A server-side-only stub should throw ServerSideOnlyException",
			messageFormat:      "'{0}' is declared server-side-only but its stub throws {1}: throw new ServerSideOnlyException(nameof({0})) so a client-side call names the API",
			category:           "LinqToDB",
			defaultSeverity:    DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description:        "ServerSideOnlyException reports which API was called on the client. Any other exception - NotImplementedException in particular - tells the caller nothing about why the call could not run.",
			helpLinkUri:        "https://github.com/linq2db/linq2db/wiki/" + WrongExceptionDiagnosticId);

		/// <inheritdoc/>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MissingMarkerRule, WrongExceptionRule);

		/// <inheritdoc/>
		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();

			// Skip generated code, unlike the internal LINQ2DB0002/0003 twin: a consumer cannot edit theirs,
			// so a diagnostic there is noise they can only suppress.
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(static startContext =>
			{
				var symbols = ServerSideOnlyContract.Symbols.TryCreate(startContext.Compilation);

				if (symbols is null)
					return;

				var compilation = startContext.Compilation;

				startContext.RegisterOperationBlockAction(blockContext =>
				{
					// The stub test is cheap and almost always false; option parsing happens only past it.
					if (!ServerSideOnlyContract.TryGetStub(blockContext.OwningSymbol, blockContext.OperationBlocks, out var member, out var thrownType)
						|| member is null
						|| thrownType is null)
					{
						return;
					}

					var options   = ReadOptions(blockContext, compilation, symbols, member);
					var violation = ServerSideOnlyContract.Classify(member, thrownType, symbols, options);

					if (violation == ServerSideOnlyContract.Violation.None)
						return;

					var location = member.Locations.Length > 0 ? member.Locations[0] : Location.None;

					if (violation == ServerSideOnlyContract.Violation.MissingMarker)
					{
						var remedy = ServerSideOnlyContract.HasMarkerCapableAttribute(member, symbols)
							? RemedySetNamedArgument
							: RemedyAddAttribute;

						blockContext.ReportDiagnostic(Diagnostic.Create(
							MissingMarkerRule,
							location,
							ImmutableDictionary<string, string?>.Empty.Add(RemedyPropertyKey, remedy),
							member.Name));
					}
					else
					{
						blockContext.ReportDiagnostic(Diagnostic.Create(
							WrongExceptionRule,
							location,
							ImmutableDictionary<string, string?>.Empty.Add(RemedyPropertyKey, RemedyReplaceException),
							member.Name,
							thrownType.Name));
					}
				});
			});
		}

		static ServerSideOnlyContract.Options ReadOptions(
			OperationBlockAnalysisContext    blockContext,
			Compilation                      compilation,
			ServerSideOnlyContract.Symbols   symbols,
			ISymbol                          member)
		{
			var tree = member.Locations.Length > 0 ? member.Locations[0].SourceTree : null;

			var configured = tree is null
				? null
				: blockContext.Options.AnalyzerConfigOptionsProvider.GetOptions(tree);

			// Both options are ADDITIVE. Replacing the defaults would let a consumer listing only their own
			// type silently drop ServerSideOnlyException, the one type that discriminates.
			return new ServerSideOnlyContract.Options(
				Resolve(configured, UnmarkedStubExceptionTypesKey, compilation, symbols.ServerSideOnlyException),
				Resolve(configured, AllowedExceptionTypesKey,      compilation, symbols.ServerSideOnlyException));
		}

		static ImmutableArray<INamedTypeSymbol> Resolve(
			AnalyzerConfigOptions? options,
			string                 key,
			Compilation            compilation,
			INamedTypeSymbol       @default)
		{
			if (options is null || !options.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
				return ImmutableArray.Create(@default);

			var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

			builder.Add(@default);

			foreach (var name in value.Split(','))
			{
				var trimmed = name.Trim();

				if (trimmed.Length == 0)
					continue;

				// An unresolvable name degrades to silence for that entry rather than throwing, so a typo
				// or a type from an unreferenced assembly cannot break the build.
				var type = compilation.GetTypeByMetadataName(trimmed);

				if (type is not null && !Contains(builder, type))
					builder.Add(type);
			}

			return builder.ToImmutable();
		}

		static bool Contains(IReadOnlyList<INamedTypeSymbol> types, INamedTypeSymbol type)
		{
			for (var i = 0; i < types.Count; i++)
				if (SymbolEqualityComparer.Default.Equals(types[i], type))
					return true;

			return false;
		}
	}
}
