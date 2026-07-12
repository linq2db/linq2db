using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

using LinqToDB.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

		// Opt-in (default off): apply the rewrite even when the Sql.Window return type diverges from the legacy
		// ToValue<TR>() slot, leaving the user to resolve the resulting type error. Off by default so the fix never
		// turns compiling code into a type error unasked. User-facing form is `linq2db.L2DB1001.apply_fix_on_return_type_mismatch`;
		// the lookup key is lower-cased because Roslyn lower-cases .editorconfig keys on parse (they are case-insensitive).
		static readonly string ApplyOnReturnTypeMismatchOptionKey =
			("linq2db." + WindowFunctionApiAnalyzer.DiagnosticId + ".apply_fix_on_return_type_mismatch").ToLowerInvariant();

		/// <inheritdoc/>
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(WindowFunctionApiAnalyzer.DiagnosticId);

		// A custom document-based Fix-All rather than WellKnownFixAllProviders.BatchFixer: BatchFixer computes each
		// fix against the *original* tree and merges the results, so when several diagnostics sit physically close
		// (e.g. multiple window columns in one `select new { ... }`), the edits after the first go stale and are
		// silently dropped. WindowChainFixAllProvider instead rewrites every flagged chain in a single ReplaceNodes
		// pass, so all occurrences in a document are converted at once.
		/// <inheritdoc/>
		public override FixAllProvider GetFixAllProvider() => WindowChainFixAllProvider.Instance;

		/// <inheritdoc/>
		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (root is null)
				return;

			var diagnostic = context.Diagnostics[0];
			var invocation = FindConvertibleInvocation(root, diagnostic);

			if (invocation is null)
				return;

			var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

			if (model is null)
				return;

			var ignoreReturnTypeMismatch = ReadApplyOnReturnTypeMismatch(context.Document, root.SyntaxTree);
			var rewritten                = LegacyWindowChainRewriter.TryRewrite(invocation, model, context.CancellationToken, ignoreReturnTypeMismatch);

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

		// The invocation node the diagnostic anchors to (the terminal ToValue() call of the chain), or null.
		static InvocationExpressionSyntax? FindConvertibleInvocation(SyntaxNode root, Diagnostic diagnostic)
		{
			var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
			return node as InvocationExpressionSyntax ?? node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
		}

		// Read the per-file opt-in that applies the rewrite despite a return-type mismatch (default off).
		static bool ReadApplyOnReturnTypeMismatch(Document document, SyntaxTree tree)
		{
			var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);

			return options.TryGetValue(ApplyOnReturnTypeMismatchOptionKey, out var value)
				&& bool.TryParse(value, out var enabled)
				&& enabled;
		}

		sealed class WindowChainFixAllProvider : DocumentBasedFixAllProvider
		{
			public static readonly WindowChainFixAllProvider Instance = new();

			protected override string GetFixAllTitle(FixAllContext fixAllContext) => Title;

			protected override async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
			{
				var root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);

				if (root is null)
					return document;

				var model = await document.GetSemanticModelAsync(fixAllContext.CancellationToken).ConfigureAwait(false);

				if (model is null)
					return document;

				var ignoreReturnTypeMismatch = ReadApplyOnReturnTypeMismatch(document, root.SyntaxTree);

				// Compute every rewrite against the ORIGINAL tree/model, then apply them together via a single
				// ReplaceNodes pass — no edit ever sees a tree mutated by another, so none goes stale.
				var replacements = new Dictionary<SyntaxNode, ExpressionSyntax>();

				foreach (var diagnostic in diagnostics)
				{
					var invocation = FindConvertibleInvocation(root, diagnostic);

					if (invocation is null || replacements.ContainsKey(invocation))
						continue;

					var rewritten = LegacyWindowChainRewriter.TryRewrite(invocation, model, fixAllContext.CancellationToken, ignoreReturnTypeMismatch);

					if (rewritten is not null)
						replacements[invocation] = rewritten;
				}

				if (replacements.Count == 0)
					return document;

				var newRoot = root.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]);

				return document.WithSyntaxRoot(newRoot);
			}
		}
	}
}
