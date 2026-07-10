using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LinqToDB.Analyzers
{
	/// <summary>
	/// Reports use of the legacy analytic / window-function API
	/// (<c>Sql.Ext.&lt;Fn&gt;()...Over()...ToValue()</c>, class <c>LinqToDB.AnalyticFunctions</c>) which is
	/// superseded by the <c>Sql.Window</c> API and slated for removal in a future major release. A companion
	/// code fix migrates mechanically-convertible chains to <c>Sql.Window</c>.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class WindowFunctionApiAnalyzer : DiagnosticAnalyzer
	{
		/// <summary>Diagnostic id for the legacy window-function API usage rule.</summary>
		public const string DiagnosticId = "LINQ2DB1001";

		const string AnalyticFunctionsMetadataName = "LinqToDB.AnalyticFunctions";
		const string ToValueMethodName             = "ToValue";

		internal static readonly DiagnosticDescriptor Rule = new(
			id:                 DiagnosticId,
			title:              "Use the Sql.Window API instead of the legacy Sql.Ext window functions",
			messageFormat:      "Legacy window-function API 'Sql.Ext.{0}(...)' is superseded by 'Sql.Window'; migrate to the Sql.Window API",
			category:           "Usage",
			defaultSeverity:    DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description:        "The Sql.Ext analytic / window-function API is superseded by the Sql.Window API and will be removed in a future major release. Where the call chain is mechanically convertible a code fix rewrites it to the equivalent Sql.Window call.",
			helpLinkUri:        "https://github.com/linq2db/linq2db/wiki/LINQ2DB1001");

		/// <inheritdoc/>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		/// <inheritdoc/>
		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(static startContext =>
			{
				// Resolve the anchor type once per compilation; skip entirely when linq2db isn't referenced.
				var analyticFunctions = startContext.Compilation.GetTypeByMetadataName(AnalyticFunctionsMetadataName);
				if (analyticFunctions is null)
					return;

				startContext.RegisterOperationAction(opContext =>
				{
					var invocation = (IInvocationOperation)opContext.Operation;

					// Cheap name gate before any symbol comparison — the vast majority of invocations bail here.
					if (!string.Equals(invocation.TargetMethod.Name, ToValueMethodName, StringComparison.Ordinal))
						return;

					// ToValue() is an instance member of AnalyticFunctions' nested IReadyToFunction<> builder,
					// so the terminal call's containing type is nested directly in AnalyticFunctions.
					if (!SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType.ContainingType, analyticFunctions))
						return;

					var rootName = GetRootFunctionName(invocation) ?? "window";
					opContext.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), rootName));
				}, OperationKind.Invocation);
			});
		}

		// Walk the fluent receiver chain back to the Sql.Ext.<Fn>(...) root and return <Fn>.
		// Handles both instance builder links (Instance set) and the extension-method root
		// (receiver in Instance or, in reduced form, in the first argument).
		static string? GetRootFunctionName(IInvocationOperation invocation)
		{
			var current = invocation;

			while (true)
			{
				var receiver = current.Instance
					?? (current.TargetMethod.IsExtensionMethod && current.Arguments.Length > 0 ? current.Arguments[0].Value : null);

				if (receiver is IInvocationOperation inner)
					current = inner;
				else
					return current.TargetMethod.Name;
			}
		}
	}
}
