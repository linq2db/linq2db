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
	/// Code fix for <see cref="StringHintAnalyzer"/> (<c>L2DB1005</c>): replaces a string hint call with the typed
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
			get { return [StringHintAnalyzer.DiagnosticId]; }
		}

		/// <inheritdoc/>
		public override FixAllProvider GetFixAllProvider()
		{
			// Fix-All applies the action the user picked (one provider, via its equivalence key) to every occurrence of the
			// same kind: an exact rewrite never pulls in a restricting one, nor the other way round (GetEquivalenceKey).
			return WellKnownFixAllProviders.BatchFixer;
		}

		/// <inheritdoc/>
		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (root is null)
				return;

			var diagnostic = context.Diagnostics[0];

			if (FindInvocation(root, diagnostic) is not { } invocation)
				return;

			var fixes = ReadFixes(diagnostic);

			// A restricting rewrite changes the call's type; when that type is not expressible, it is not offered.
			if (fixes.Count > 0 && fixes[0].GenericOverload)
			{
				var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

				if (model is null || GetVarLocalRetype(invocation, model, context.CancellationToken) is { Blocked: true })
					return;
			}

			foreach (var fix in fixes)
			{
				var title = fix.GenericOverload
					? string.Format(CultureInfo.InvariantCulture, "Use {0} typed hint {1}() (applies to {0} only)", fix.Provider, fix.Helper)
					: string.Format(CultureInfo.InvariantCulture, "Use typed hint {0}()", fix.Helper);

				var captured = fix;

				context.RegisterCodeFix(
					CodeAction.Create(
						title,
						ct => ApplyAsync(context.Document, diagnostic, captured, ct),
						equivalenceKey: GetEquivalenceKey(fix)),
					diagnostic);
			}
		}

		// An exact rewrite (a provider's own string overload) and a restricting one (a generic overload, whose rewrite stops
		// the hint on every other provider) get different keys, so Fix-All started from an exact rewrite, whose title names
		// no restriction, never restricts a generic call, and a restricting Fix-All stays among generic calls.
		static string GetEquivalenceKey(Fix fix)
		{
			var key = StringHintAnalyzer.DiagnosticId + "|" + fix.Provider;

			return fix.GenericOverload ? key + "|restricted" : key;
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

			replacement = replacement.WithTriviaFrom(invocation);

			SyntaxNode newRoot;

			if (fix.GenericOverload && GetVarLocalRetype(invocation, model, cancellationToken) is { } retype)
			{
				if (retype.Blocked)
					return document;

				newRoot = root.ReplaceNodes(
					new SyntaxNode[] { invocation, retype.VarType },
					(original, _) => original == invocation ? replacement : retype.ExplicitType.WithTriviaFrom(retype.VarType));
			}
			else
			{
				newRoot = root.ReplaceNode(invocation, replacement);
			}

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

		sealed class VarLocalRetype
		{
			public VarLocalRetype(TypeSyntax varType, TypeSyntax explicitType, bool blocked)
			{
				VarType      = varType;
				ExplicitType = explicitType;
				Blocked      = blocked;
			}

			public TypeSyntax VarType      { get; }
			public TypeSyntax ExplicitType { get; }
			public bool       Blocked      { get; }
		}

		// A generic overload returns ITable<T> / IQueryable<T>, the typed helper the provider's own type. When the call
		// initializes a `var` local that is written again after its declaration (`q = q.Where(...)`), the new type would
		// break that write, so the local keeps the original type, written out. Null when nothing has to change; blocked
		// when the type cannot be written (an anonymous element type).
		static VarLocalRetype? GetVarLocalRetype(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken)
		{
			SyntaxNode value = invocation;

			while (value.Parent is ParenthesizedExpressionSyntax parenthesized)
				value = parenthesized;

			if (value.Parent is not EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: true } declaration } declarator })
				return null;

			if (model.GetDeclaredSymbol(declarator, cancellationToken) is not ILocalSymbol local
				|| !IsWrittenAfterDeclaration(local, declarator, model, cancellationToken))
				return null;

			var type = model.GetTypeInfo(invocation, cancellationToken).Type;

			if (type is null || type.TypeKind == TypeKind.Error || ContainsAnonymousType(type))
				return new VarLocalRetype(declaration.Type, declaration.Type, blocked: true);

			var explicitType = SyntaxFactory.ParseTypeName(type.ToMinimalDisplayString(model, declaration.Type.SpanStart));

			return new VarLocalRetype(declaration.Type, explicitType, blocked: false);
		}

		static bool IsWrittenAfterDeclaration(ILocalSymbol local, VariableDeclaratorSyntax declarator, SemanticModel model, CancellationToken cancellationToken)
		{
			var scope = declarator.FirstAncestorOrSelf<BlockSyntax>() ?? (SyntaxNode)declarator.SyntaxTree.GetRoot(cancellationToken);

			foreach (var identifier in scope.DescendantNodes().OfType<IdentifierNameSyntax>())
			{
				if (!string.Equals(identifier.Identifier.ValueText, local.Name, StringComparison.Ordinal) || !IsWriteTarget(identifier))
					continue;

				if (SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier, cancellationToken).Symbol, local))
					return true;
			}

			return false;
		}

		// An assignment target (simple, compound, `??=`, or an element of a deconstruction), a ref / out argument, a
		// `ref` alias, or an increment.
		static bool IsWriteTarget(ExpressionSyntax identifier)
		{
			SyntaxNode node = identifier;

			while (node.Parent is ParenthesizedExpressionSyntax)
				node = node.Parent;

			switch (node.Parent)
			{
				case AssignmentExpressionSyntax assignment when assignment.Left == node:
				case RefExpressionSyntax:
				case PrefixUnaryExpressionSyntax  { RawKind: (int)SyntaxKind.PreIncrementExpression or (int)SyntaxKind.PreDecrementExpression }:
				case PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.PostIncrementExpression or (int)SyntaxKind.PostDecrementExpression }:
					return true;
				case ArgumentSyntax argument when !argument.RefKindKeyword.IsKind(SyntaxKind.None) && !argument.RefKindKeyword.IsKind(SyntaxKind.InKeyword):
					return true;
				case ArgumentSyntax { Parent: TupleExpressionSyntax tuple }:
					// (q, x) = ...: climb the nested tuples to the assignment.
					SyntaxNode outer = tuple;

					while (outer.Parent is ArgumentSyntax { Parent: TupleExpressionSyntax parentTuple })
						outer = parentTuple;

					return outer.Parent is AssignmentExpressionSyntax deconstruction && deconstruction.Left == outer;
				default:
					return false;
			}
		}

		static bool ContainsAnonymousType(ITypeSymbol type)
		{
			if (type.IsAnonymousType)
				return true;

			switch (type)
			{
				case INamedTypeSymbol named:
					foreach (var argument in named.TypeArguments)
						if (ContainsAnonymousType(argument))
							return true;

					return false;
				case IArrayTypeSymbol array:
					return ContainsAnonymousType(array.ElementType);
				default:
					return false;
			}
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
						&& string.CompareOrdinal(Normalize(GetNamespaceText(name)), Normalize(ns)) > 0)
					{
						index = i;
						break;
					}
				}
			}

			if (usings.Count == 0 && unit.Externs.Count == 0)
			{
				// The first using of the file goes below the file header (comments, #nullable, #define), with a blank line
				// before the code that follows; trivia that belongs to the first declaration (a doc comment) stays on it.
				var firstToken = unit.GetFirstToken(includeZeroWidth: true);
				var trivia     = firstToken.LeadingTrivia;
				var headerEnd  = GetHeaderEnd(trivia);

				directive = directive.WithLeadingTrivia(trivia.Take(headerEnd));
				unit      = unit.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(new[] { endOfLine }.Concat(trivia.Skip(headerEnd))));

				return unit.WithUsings(usings.Add(directive));
			}

			if (index < usings.Count)
			{
				var next = usings[index];

				// Usings grouped by blank lines (by first namespace segment, or System and the rest): a directive that sorts
				// last in the previous group stays there, after that group's last directive, and the next group keeps its
				// leading blank line.
				var group              = GetGroup(ns);
				var joinsPreviousGroup = index > firstLocal
					&& string.Equals(group, GetGroup(usings[index - 1]), StringComparison.Ordinal)
					&& !string.Equals(group, GetGroup(next), StringComparison.Ordinal);

				if (!joinsPreviousGroup)
				{
					// Take over the leading trivia (blank-line grouping, file header) of the directive it goes in front of.
					directive = directive.WithLeadingTrivia(next.GetLeadingTrivia());
					usings    = usings.Replace(next, next.WithoutLeadingTrivia());
				}
			}

			return unit.WithUsings(usings.Insert(index, directive));
		}

		// The file header is the leading trivia up to its last blank line or preprocessor directive. A conditional block
		// that is still open at the first token encloses the declarations, so the header ends before it: a using inside
		// it would exist in one configuration only. A block closed within the header stays part of it.
		static int GetHeaderEnd(SyntaxTriviaList trivia)
		{
			var end       = 0;
			var lineEmpty = true;
			var depth     = 0;
			var endAtOpen = -1;

			for (var i = 0; i < trivia.Count; i++)
			{
				var item = trivia[i];

				if (item.IsKind(SyntaxKind.IfDirectiveTrivia))
				{
					if (depth++ == 0)
						endAtOpen = end;
				}
				else if (item.IsKind(SyntaxKind.EndIfDirectiveTrivia) && depth > 0)
				{
					depth--;
				}

				if (item.IsDirective)
				{
					// A directive's trivia includes its end of line.
					end       = i + 1;
					lineEmpty = true;
				}
				else if (item.IsKind(SyntaxKind.EndOfLineTrivia))
				{
					if (lineEmpty)
						end = i + 1;

					lineEmpty = true;
				}
				else if (!item.IsKind(SyntaxKind.WhitespaceTrivia))
				{
					lineEmpty = false;
				}
			}

			return depth > 0 ? endAtOpen : end;
		}

		// The first segment of the namespace a directive names: usings grouped by it, or by System and the rest, have
		// their group boundaries between different first segments.
		static string? GetGroup(UsingDirectiveSyntax directive)
		{
			return directive.Name is { } name ? GetGroup(GetNamespaceText(name)) : null;
		}

		static string GetGroup(string ns)
		{
			var dot = ns.IndexOf('.');

			return dot < 0 ? ns : ns.Substring(0, dot);
		}

		static bool IsSystemNamespace(string name)
		{
			return string.Equals(name, "System", StringComparison.Ordinal) || name.StartsWith("System.", StringComparison.Ordinal);
		}

		// System first, then ordinal: the order the repository and `dotnet format` keep.
		static string Normalize(string name)
		{
			return IsSystemNamespace(name) ? "0" + name : "1" + name;
		}

		static bool IsSorted(SyntaxList<UsingDirectiveSyntax> usings)
		{
			string? previous = null;

			foreach (var directive in usings)
			{
				if (directive.GlobalKeyword != default || directive.Alias is not null || directive.StaticKeyword != default || directive.Name is null)
					continue;

				var current = Normalize(GetNamespaceText(directive.Name));

				if (previous is not null && string.CompareOrdinal(previous, current) > 0)
					return false;

				previous = current;
			}

			return true;
		}

		// Imported if a using directive in scope at the position - in this file, an enclosing namespace, or a global
		// using anywhere in the compilation - names the namespace, with or without a leading `global::`.
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
					if (directive.Alias is null && directive.StaticKeyword == default && NamesNamespace(directive, ns))
						return true;
			}

			foreach (var tree in model.Compilation.SyntaxTrees)
			{
				if (tree.GetRoot() is not CompilationUnitSyntax unit)
					continue;

				foreach (var directive in unit.Usings)
					if (directive.GlobalKeyword != default && directive.Alias is null && directive.StaticKeyword == default
						&& NamesNamespace(directive, ns))
						return true;
			}

			return false;
		}

		static bool NamesNamespace(UsingDirectiveSyntax directive, string ns)
		{
			return directive.Name is { } name && string.Equals(GetNamespaceText(name), ns, StringComparison.Ordinal);
		}

		// The namespace a using directive names, without the `global::` alias qualifier the SDK writes in front of
		// implicit and <Using> imports (GlobalUsings.g.cs: `global using global::System.Linq;`).
		static string GetNamespaceText(NameSyntax name)
		{
			const string GlobalPrefix = "global::";

			var text = name.ToString();

			return text.StartsWith(GlobalPrefix, StringComparison.Ordinal) ? text.Substring(GlobalPrefix.Length) : text;
		}
	}
}
