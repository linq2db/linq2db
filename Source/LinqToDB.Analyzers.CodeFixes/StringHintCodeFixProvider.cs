using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace LinqToDB.Analyzers.CodeFixes
{
	/// <summary>
	/// Code fix for <see cref="StringHintAnalyzer"/> (<c>L2DB2001</c>): replaces a string hint call with the typed
	/// helper the diagnostic names, inserting the provider's <c>As*()</c> call where the receiver is not already of
	/// the provider's type, and importing the provider namespace.
	/// </summary>
	/// <remarks>
	/// On a generic receiver every provider that has the hint gets its own action, titled with the provider it
	/// restricts the hint to: the string overload emitted the hint for every provider, the typed one emits it for
	/// one. The rewrite targets are read from <see cref="Diagnostic.Properties"/>, as in the other code fixes here.
	/// </remarks>
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StringHintCodeFixProvider))]
	[Shared]
	public sealed class StringHintCodeFixProvider : CodeFixProvider
	{
		const string SystemLinqNamespace = "System.Linq";
		const string AsQueryableName     = "AsQueryable";

		/// <inheritdoc/>
		public override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(StringHintAnalyzer.DiagnosticId); }
		}

		/// <inheritdoc/>
		public override FixAllProvider GetFixAllProvider()
		{
			// Fix-All applies the action the user picked (one provider, via its equivalence key) to every occurrence.
			return WellKnownFixAllProviders.BatchFixer;
		}

		/// <inheritdoc/>
		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (root is null)
				return;

			var diagnostic = context.Diagnostics[0];

			if (FindInvocation(root, diagnostic) is null)
				return;

			foreach (var fix in ReadFixes(diagnostic))
			{
				var title = fix.GenericOverload
					? string.Format(CultureInfo.InvariantCulture, "Use {0} typed hint {1}() (applies to {0} only)", fix.Provider, fix.Helper)
					: string.Format(CultureInfo.InvariantCulture, "Use typed hint {0}()", fix.Helper);

				var captured = fix;

				context.RegisterCodeFix(
					CodeAction.Create(
						title,
						ct => ApplyAsync(context.Document, diagnostic, captured, ct),
						equivalenceKey: StringHintAnalyzer.DiagnosticId + "|" + fix.Provider),
					diagnostic);
			}
		}

		sealed class Fix
		{
			public Fix(string provider, string ns, string asMethod, bool asQueryable, string helper, bool genericOverload)
			{
				Provider        = provider;
				Namespace       = ns;
				AsMethod        = asMethod;
				AsQueryable     = asQueryable;
				Helper          = helper;
				GenericOverload = genericOverload;
			}

			public string Provider        { get; }
			public string Namespace       { get; }
			public string AsMethod        { get; }
			public bool   AsQueryable     { get; }
			public string Helper          { get; }
			public bool   GenericOverload { get; }
		}

		static List<Fix> ReadFixes(Diagnostic diagnostic)
		{
			var fixes      = new List<Fix>();
			var properties = diagnostic.Properties;

			if (!properties.TryGetValue(StringHintAnalyzer.FixCountKey, out var countText)
				|| !int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
				return fixes;

			properties.TryGetValue(StringHintAnalyzer.GenericOverloadKey, out var genericText);

			var generic = string.Equals(genericText, "true", StringComparison.Ordinal);

			for (var i = 0; i < count; i++)
			{
				var prefix = "Fix" + i.ToString(CultureInfo.InvariantCulture);

				if (!properties.TryGetValue(prefix + StringHintAnalyzer.FixProviderKey,    out var provider)    || provider    is null
					|| !properties.TryGetValue(prefix + StringHintAnalyzer.FixNamespaceKey,   out var ns)          || ns          is null
					|| !properties.TryGetValue(prefix + StringHintAnalyzer.FixAsMethodKey,    out var asMethod)    || asMethod    is null
					|| !properties.TryGetValue(prefix + StringHintAnalyzer.FixAsQueryableKey, out var asQueryable) || asQueryable is null
					|| !properties.TryGetValue(prefix + StringHintAnalyzer.FixHelperKey,      out var helper)      || helper      is null)
					continue;

				fixes.Add(new Fix(provider, ns, asMethod, string.Equals(asQueryable, "true", StringComparison.Ordinal), helper, generic));
			}

			return fixes;
		}

		// The diagnostic spans the hint call's name and argument list; the invocation ends where the diagnostic does.
		static InvocationExpressionSyntax? FindInvocation(SyntaxNode root, Diagnostic diagnostic)
		{
			var span = diagnostic.Location.SourceSpan;
			var node = root.FindNode(span, getInnermostNodeForTie: true);

			foreach (var invocation in node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>())
			{
				if (invocation.Span.End != span.End)
					continue;

				// Extension form (receiver.TableHint("…")) or static form (LinqExtensions.TableHint(receiver, "…")).
				if (invocation.Expression is MemberAccessExpressionSyntax or IdentifierNameSyntax or GenericNameSyntax)
					return invocation;
			}

			return null;
		}

		static async Task<Document> ApplyAsync(Document document, Diagnostic diagnostic, Fix fix, CancellationToken cancellationToken)
		{
			var root  = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

			if (root is null || model is null || FindInvocation(root, diagnostic) is not { } invocation)
				return document;

			var replacement = BuildReplacement(invocation, model, fix, cancellationToken);

			if (replacement is null)
				return document;

			var newRoot = root.ReplaceNode(invocation, replacement.WithTriviaFrom(invocation));

			if (newRoot is CompilationUnitSyntax unit)
			{
				var position = invocation.SpanStart;

				unit = AddUsingIfMissing(unit, model, position, fix.Namespace);

				if (fix.AsQueryable)
					unit = AddUsingIfMissing(unit, model, position, SystemLinqNamespace);

				newRoot = unit;
			}

			return document.WithSyntaxRoot(newRoot);
		}

		static ExpressionSyntax? BuildReplacement(InvocationExpressionSyntax invocation, SemanticModel model, Fix fix, CancellationToken cancellationToken)
		{
			var names = new List<string>();

			if (fix.AsQueryable)
				names.Add(AsQueryableName);

			if (fix.AsMethod.Length > 0)
				names.Add(fix.AsMethod);

			names.Add(fix.Helper);

			var isReducedCall = model.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol { ReducedFrom: not null };

			if (isReducedCall && invocation.Expression is MemberAccessExpressionSyntax memberAccess)
			{
				// receiver<trivia>.TableHint("…") -> receiver<trivia>.AsSqlServer().WithNoLock(): the first call reuses the
				// original dot and its trivia, so a chain broken over lines keeps its layout.
				ExpressionSyntax current = SyntaxFactory.InvocationExpression(
					memberAccess.WithName(Name(names[0]).WithTriviaFrom(memberAccess.Name)),
					EmptyArguments());

				for (var i = 1; i < names.Count; i++)
					current = Call(current, names[i]);

				return current;
			}

			if (!isReducedCall
				&& model.GetOperation(invocation, cancellationToken) is IInvocationOperation operation
				&& operation.Arguments.FirstOrDefault(a => a.Parameter?.Ordinal == 0)?.Syntax is ArgumentSyntax receiverArgument)
			{
				// Static form: the receiver is the argument bound to the first parameter, which a named argument can
				// put anywhere in the list (TableHint(hint: "NOLOCK", table: t)).
				ExpressionSyntax current = receiverArgument.Expression.WithoutTrivia();

				if (current is not (IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax or ElementAccessExpressionSyntax or ThisExpressionSyntax or ParenthesizedExpressionSyntax))
					current = SyntaxFactory.ParenthesizedExpression(current);

				foreach (var name in names)
					current = Call(current, name);

				return current;
			}

			return null;
		}

		// The new tokens carry plain (not elastic) empty trivia: elastic trivia would let the formatter that runs after
		// a code action re-indent the user's chain around the rewritten call.
		static InvocationExpressionSyntax Call(ExpressionSyntax receiver, string name)
		{
			return SyntaxFactory.InvocationExpression(
				SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					receiver,
					SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.DotToken, SyntaxTriviaList.Empty),
					Name(name)),
				EmptyArguments());
		}

		static IdentifierNameSyntax Name(string name)
		{
			return SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(SyntaxTriviaList.Empty, name, SyntaxTriviaList.Empty));
		}

		static ArgumentListSyntax EmptyArguments()
		{
			return SyntaxFactory.ArgumentList(
				SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.OpenParenToken, SyntaxTriviaList.Empty),
				default,
				SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.CloseParenToken, SyntaxTriviaList.Empty));
		}

		static CompilationUnitSyntax AddUsingIfMissing(CompilationUnitSyntax unit, SemanticModel model, int position, string ns)
		{
			if (IsImported(model, position, ns))
				return unit;

			// Leaves the original file's line ending on the new directive.
			var endOfLine = unit.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));

			if (endOfLine == default)
				endOfLine = SyntaxFactory.CarriageReturnLineFeed;

			var directive = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns))
				.WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space))
				.WithTrailingTrivia(endOfLine);

			var usings = unit.Usings;
			var index  = usings.Count;

			// Global usings must precede every other using (CS8915), so the new directive never goes in front of one.
			var firstLocal = 0;

			while (firstLocal < usings.Count && usings[firstLocal].GlobalKeyword != default)
				firstLocal++;

			// Sorted position among the plain usings, when they are sorted; otherwise after the last one.
			if (IsSorted(usings))
			{
				for (var i = firstLocal; i < usings.Count; i++)
				{
					if (usings[i].Alias is null && usings[i].StaticKeyword == default && usings[i].Name is { } name
						&& string.CompareOrdinal(Normalize(name.ToString()), Normalize(ns)) > 0)
					{
						index = i;
						break;
					}
				}
			}

			if (index < usings.Count)
			{
				// Take over the leading trivia (blank-line grouping) of the directive it is inserted in front of.
				var next = usings[index];

				directive = directive.WithLeadingTrivia(next.GetLeadingTrivia());
				usings    = usings.Replace(next, next.WithoutLeadingTrivia());
			}

			return unit.WithUsings(usings.Insert(index, directive));
		}

		// System first, then ordinal: the order the repository and `dotnet format` keep.
		static string Normalize(string name)
		{
			return string.Equals(name, "System", StringComparison.Ordinal) || name.StartsWith("System.", StringComparison.Ordinal) ? "0" + name : "1" + name;
		}

		static bool IsSorted(SyntaxList<UsingDirectiveSyntax> usings)
		{
			string? previous = null;

			foreach (var directive in usings)
			{
				if (directive.GlobalKeyword != default || directive.Alias is not null || directive.StaticKeyword != default || directive.Name is null)
					continue;

				var current = Normalize(directive.Name.ToString());

				if (previous is not null && string.CompareOrdinal(previous, current) > 0)
					return false;

				previous = current;
			}

			return true;
		}

		// Imported if a using directive in scope at the position - in this file, an enclosing namespace, or a global
		// using anywhere in the compilation - names the namespace.
		static bool IsImported(SemanticModel model, int position, string ns)
		{
			var token = model.SyntaxTree.GetRoot().FindToken(position);

			foreach (var node in token.Parent?.AncestorsAndSelf() ?? Enumerable.Empty<SyntaxNode>())
			{
				var usings = node switch
				{
					CompilationUnitSyntax unit                  => unit.Usings,
					BaseNamespaceDeclarationSyntax declaration  => declaration.Usings,
					_                                           => default,
				};

				foreach (var directive in usings)
					if (directive.Alias is null && directive.StaticKeyword == default && string.Equals(directive.Name?.ToString(), ns, StringComparison.Ordinal))
						return true;
			}

			foreach (var tree in model.Compilation.SyntaxTrees)
			{
				if (tree.GetRoot() is not CompilationUnitSyntax unit)
					continue;

				foreach (var directive in unit.Usings)
					if (directive.GlobalKeyword != default && directive.Alias is null && directive.StaticKeyword == default
						&& string.Equals(directive.Name?.ToString(), ns, StringComparison.Ordinal))
						return true;
			}

			return false;
		}
	}
}
