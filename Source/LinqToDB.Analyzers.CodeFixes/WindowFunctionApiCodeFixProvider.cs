using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

using LinqToDB.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LinqToDB.Analyzers.CodeFixes
{
	/// <summary>
	/// Code fix for <see cref="WindowFunctionApiAnalyzer"/> (L2DB1001): rewrites a mechanically-convertible
	/// legacy <c>Sql.Ext.&lt;Fn&gt;()...Over()...ToValue()</c> chain to the equivalent <c>Sql.Window.&lt;Fn&gt;(...)</c>
	/// call, preserving comments and formatting. Chains with no direct <c>Sql.Window</c> equivalent are left
	/// unchanged (the diagnostic remains, but no fix is offered).
	/// </summary>
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WindowFunctionApiCodeFixProvider))]
	[Shared]
	public sealed class WindowFunctionApiCodeFixProvider : CodeFixProvider
	{
		const string Title = "Convert to Sql.Window API";

		/// <inheritdoc/>
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(WindowFunctionApiAnalyzer.DiagnosticId);

		/// <inheritdoc/>
		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		/// <inheritdoc/>
		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (root is null)
				return;

			var diagnostic = context.Diagnostics[0];
			var node       = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
			var invocation = node as InvocationExpressionSyntax ?? node.FirstAncestorOrSelf<InvocationExpressionSyntax>();

			if (invocation is null)
				return;

			var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

			if (model is null)
				return;

			var rewritten = LegacyWindowChainRewriter.TryRewrite(invocation, model, context.CancellationToken);

			// Not mechanically convertible — leave the diagnostic in place with no fix.
			if (rewritten is null)
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Title,
					_ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(invocation, rewritten))),
					equivalenceKey: WindowFunctionApiAnalyzer.DiagnosticId),
				diagnostic);
		}
	}
}
