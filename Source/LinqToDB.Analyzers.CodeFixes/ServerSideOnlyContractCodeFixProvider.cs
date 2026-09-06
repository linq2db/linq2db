using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace LinqToDB.Analyzers.CodeFixes
{
	/// <summary>
	/// Code fixes for <see cref="ServerSideOnlyContractAnalyzer"/>: adds a server-side-only marker
	/// (<c>L2DB1003</c>) or replaces a stub's exception with <c>ServerSideOnlyException</c> (<c>L2DB1004</c>).
	/// </summary>
	/// <remarks>
	/// The remedy is read from <see cref="Diagnostic.Properties"/> rather than re-derived: the analyzer's
	/// detection core is <c>internal</c> to the sibling assembly, and opening it up with
	/// <c>InternalsVisibleTo</c> to serve a code fix would widen a shipped analyzer's surface for no gain.
	/// <para>
	/// The missing-marker rule's other remedy - implement the body - is named in the diagnostic message and
	/// deliberately never auto-applied; synthesising an implementation is not a mechanical rewrite.
	/// </para>
	/// </remarks>
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ServerSideOnlyContractCodeFixProvider))]
	[Shared]
	public sealed class ServerSideOnlyContractCodeFixProvider : CodeFixProvider
	{
		const string AddMarkerTitle        = "Declare server-side only";
		const string ReplaceExceptionTitle = "Throw ServerSideOnlyException";

		const string ServerSideOnlyAttributeName = "LinqToDB.Mapping.ServerSideOnly";
		const string ServerSideOnlyExceptionName = "LinqToDB.ServerSideOnlyException";
		const string ExpressionAttributeMetadataName = "LinqToDB.Sql+ExpressionAttribute";

		/// <inheritdoc/>
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
			ServerSideOnlyContractAnalyzer.MissingMarkerDiagnosticId,
			ServerSideOnlyContractAnalyzer.WrongExceptionDiagnosticId);

		// A custom solution-scoped Fix-All - see ContractFixAllProvider for why neither
		// WellKnownFixAllProviders.BatchFixer nor DocumentBasedFixAllProvider can express this fix.
		/// <inheritdoc/>
		public override FixAllProvider GetFixAllProvider() => ContractFixAllProvider.Instance;

		/// <inheritdoc/>
		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root  = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

			if (root is null || model is null)
				return;

			var diagnostic = context.Diagnostics[0];

			var title = string.Equals(diagnostic.Id, ServerSideOnlyContractAnalyzer.WrongExceptionDiagnosticId, System.StringComparison.Ordinal)
				? ReplaceExceptionTitle
				: AddMarkerTitle;

			if (!TryRewrite(root, model, diagnostic, out var original, out var replacement) || original is null || replacement is null)
			{
				// What the fix has to write can be declared on an implemented interface member in ANOTHER file,
				// which no single-tree rewrite can reach. Offer a solution-level fix for that case rather than
				// declining: marking the reported member would not help a call bound to the interface, since the
				// runtime reads attributes up the interface chain and not back down.
				var targetLocation = GetFixTargetLocation(diagnostic);

				if (targetLocation?.SourceTree is not null && targetLocation.SourceTree != root.SyntaxTree)
				{
					diagnostic.Properties.TryGetValue(ServerSideOnlyContractAnalyzer.RemedyPropertyKey, out var crossFileRemedy);

					context.RegisterCodeFix(
						CodeAction.Create(
							title,
							ct => ApplyToDeclaringDocumentAsync(context.Document, targetLocation, crossFileRemedy, ct),
							equivalenceKey: diagnostic.Id),
						diagnostic);
				}

				return;
			}

			context.RegisterCodeFix(
				CodeAction.Create(
					title,
					ct => ApplyAsync(context.Document, root, original, replacement, ct),
					equivalenceKey: diagnostic.Id),
				diagnostic);
		}

		// Carries the marker-capable attribute for the set-named-argument remedy and the implemented interface
		// member's declaration for add-attribute; the remedy in Diagnostic.Properties says which.
		static Location? GetFixTargetLocation(Diagnostic diagnostic)
			=> diagnostic.AdditionalLocations.Count > 0 ? diagnostic.AdditionalLocations[0] : null;

		static async Task<Solution> ApplyToDeclaringDocumentAsync(Document document, Location location, string? remedy, CancellationToken cancellationToken)
		{
			var solution = document.Project.Solution;
			var target   = solution.GetDocument(location.SourceTree);

			if (target is null)
				return solution;

			var root = await target.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			if (root is null || !TryRewriteAt(root, location, remedy, out var original, out var replacement) || original is null || replacement is null)
				return solution;

			var updated   = target.WithSyntaxRoot(root.ReplaceNode(original, replacement));
			var processed = await PostProcessAsync(updated, cancellationToken).ConfigureAwait(false);

			return processed.Project.Solution;
		}

		/// <summary>
		/// The rewrite at a location the analyzer supplied, in whichever tree that location belongs to. Split
		/// out of <see cref="ApplyToDeclaringDocumentAsync"/> so Fix-All can compute the same edit without
		/// applying it one document at a time.
		/// </summary>
		static bool TryRewriteAt(SyntaxNode root, Location location, string? remedy, out SyntaxNode? original, out SyntaxNode? replacement)
		{
			original    = null;
			replacement = null;

			if (string.Equals(remedy, ServerSideOnlyContractAnalyzer.RemedyAddAttribute, System.StringComparison.Ordinal))
			{
				if (FindMemberAt(root, location) is not { } member)
					return false;

				original    = member;
				replacement = AddMarkerAttribute(member);
				return true;
			}

			if (FindAttributeAt(root, location) is not { } attribute)
				return false;

			original    = attribute;
			replacement = SetServerSideOnlyTrue(attribute);
			return true;
		}

		static AttributeSyntax? FindAttributeAt(SyntaxNode? root, Location location)
			=> root?.FindNode(location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<AttributeSyntax>();

		static MemberDeclarationSyntax? FindMemberAt(SyntaxNode? root, Location location)
			=> root?.FindNode(location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<MemberDeclarationSyntax>();

		// Emitted type names are fully qualified so the result compiles even when the file imports neither
		// LinqToDB nor LinqToDB.Mapping. Reducing the annotated nodes shortens them again wherever the using
		// IS present; without this the fix is correct but writes LinqToDB.ServerSideOnlyException in a file
		// that already has `using LinqToDB;`.
		static async Task<Document> ApplyAsync(Document document, SyntaxNode root, SyntaxNode original, SyntaxNode replacement, CancellationToken cancellationToken)
			=> await PostProcessAsync(document.WithSyntaxRoot(root.ReplaceNode(original, replacement)), cancellationToken).ConfigureAwait(false);

		static async Task<Document> PostProcessAsync(Document document, CancellationToken cancellationToken)
		{
			var reduced = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

			return await Formatter.FormatAsync(reduced, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		static bool TryRewrite(SyntaxNode root, SemanticModel model, Diagnostic diagnostic, out SyntaxNode? original, out SyntaxNode? replacement)
		{
			original    = null;
			replacement = null;

			var node   = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
			var member = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();

			if (member is null)
				return false;

			diagnostic.Properties.TryGetValue(ServerSideOnlyContractAnalyzer.RemedyPropertyKey, out var remedy);

			switch (remedy)
			{
				case ServerSideOnlyContractAnalyzer.RemedyAddAttribute:
					{
						// No location means the member implements nothing, so it is its own marker target. A
						// location means the marker belongs on the implemented interface member instead:
						// marking the implementation silences the rule while a call bound to the interface
						// still client-evaluates, which is the failure the rule exists to catch. Same tree
						// only here, so this stays a single-tree rewrite and Fix-All keeps working; another
						// file is handled by the solution-level action in RegisterCodeFixesAsync.
						var location = GetFixTargetLocation(diagnostic);

						if (location is null)
						{
							original    = member;
							replacement = AddMarkerAttribute(member);
							return true;
						}

						if (location.SourceTree != root.SyntaxTree || FindMemberAt(root, location) is not { } target)
							return false;

						original    = target;
						replacement = AddMarkerAttribute(target);
						return true;
					}

				case ServerSideOnlyContractAnalyzer.RemedySetNamedArgument:
					{
						// Own attribute lists first; failing that, the location the analyzer passed through - the
						// attribute may be declared on an implemented interface member. Same tree only here, so
						// this stays a single-tree rewrite and Fix-All keeps working; another file is handled by
						// the solution-level action in RegisterCodeFixesAsync.
						var attribute = FindMarkerCapableAttribute(member, model)
							?? FindSameTreeMarkerCapableAttribute(root, diagnostic);

						if (attribute is null)
							return false;

						original    = attribute;
						replacement = SetServerSideOnlyTrue(attribute);
						return true;
					}

				case ServerSideOnlyContractAnalyzer.RemedyReplaceException:
					{
						var creation = FindStubObjectCreation(member);

						if (creation is null)
							return false;

						var memberName = GetNameOfArgument(member);

						if (memberName is null)
							return false;

						original    = creation;
						replacement = ServerSideOnlyExceptionCreation(memberName);
						return true;
					}

				default:
					return false;
			}
		}

		// Insert the attribute ahead of any existing ones, carrying the member's leading trivia across so doc
		// comments survive. Line breaks and indentation are left elastic and annotated for the formatter
		// rather than synthesised here: it reads end_of_line and indent_style from the consumer's
		// .editorconfig, which hand-built trivia cannot, and a fix asked only to add an attribute must not
		// introduce a line whose endings or indentation differ from its neighbours'.
		static MemberDeclarationSyntax AddMarkerAttribute(MemberDeclarationSyntax member)
		{
			var attribute = SyntaxFactory.AttributeList(
				SyntaxFactory.SingletonSeparatedList(
					SyntaxFactory.Attribute(
						SyntaxFactory.ParseName(ServerSideOnlyAttributeName)
							.WithAdditionalAnnotations(Simplifier.Annotation))));

			if (member.AttributeLists.Count > 0)
			{
				var first = member.AttributeLists[0];

				return member
					.WithAttributeLists(
						member.AttributeLists
							.Replace(first, first.WithLeadingTrivia(SyntaxFactory.ElasticMarker))
							// Leading trivia only: WithTriviaFrom would copy first's TRAILING trivia across while
							// first keeps its own, duplicating a same-line comment onto the inserted list.
							.Insert(0, attribute
								.WithLeadingTrivia(first.GetLeadingTrivia())
								.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)))
					.WithAdditionalAnnotations(Formatter.Annotation);
			}

			var leading = member.GetLeadingTrivia();

			return member
				.WithLeadingTrivia(SyntaxFactory.ElasticMarker)
				.WithAttributeLists(SyntaxFactory.SingletonList(
					attribute
						.WithLeadingTrivia(leading)
						.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)))
				.WithAdditionalAnnotations(Formatter.Annotation);
		}

		static AttributeSyntax? FindSameTreeMarkerCapableAttribute(SyntaxNode root, Diagnostic diagnostic)
		{
			var location = GetFixTargetLocation(diagnostic);

			return location?.SourceTree == root.SyntaxTree ? FindAttributeAt(root, location!) : null;
		}

		static AttributeSyntax? FindMarkerCapableAttribute(MemberDeclarationSyntax member, SemanticModel model)
		{
			var expressionAttribute = model.Compilation.GetTypeByMetadataName(ExpressionAttributeMetadataName);

			if (expressionAttribute is null)
				return null;

			foreach (var list in member.AttributeLists)
			{
				foreach (var attribute in list.Attributes)
				{
					var type = model.GetSymbolInfo(attribute).Symbol?.ContainingType;

					for (var current = type; current is not null; current = current.BaseType)
						if (SymbolEqualityComparer.Default.Equals(current, expressionAttribute))
							return attribute;
				}
			}

			return null;
		}

		static AttributeSyntax SetServerSideOnlyTrue(AttributeSyntax attribute)
		{
			var trueLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
			var arguments   = attribute.ArgumentList ?? SyntaxFactory.AttributeArgumentList();

			// The member may already declare ServerSideOnly = false - that is one of the shapes the rule
			// reports. Update the existing argument instead of appending a second one, which is CS0643.
			foreach (var existing in arguments.Arguments)
			{
				if (existing.NameEquals is { } nameEquals
					&& string.Equals(nameEquals.Name.Identifier.ValueText, "ServerSideOnly", System.StringComparison.Ordinal))
				{
					return attribute.WithArgumentList(
						arguments.WithArguments(arguments.Arguments.Replace(existing, existing.WithExpression(trueLiteral))));
				}
			}

			var argument = SyntaxFactory.AttributeArgument(
				SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("ServerSideOnly")),
				null,
				trueLiteral);

			return attribute.WithArgumentList(arguments.AddArguments(argument));
		}

		static ObjectCreationExpressionSyntax? FindStubObjectCreation(MemberDeclarationSyntax member)
		{
			var body = GetClassifiedBody(member);

			if (body is null)
				return null;

			foreach (var node in body.DescendantNodes())
				if (node is ThrowStatementSyntax { Expression: ObjectCreationExpressionSyntax statementCreation })
					return statementCreation;
				else if (node is ThrowExpressionSyntax { Expression: ObjectCreationExpressionSyntax expressionCreation })
					return expressionCreation;

			return null;
		}

		// The analyzer classifies a property's GETTER but reports on the property, so the node handed to the
		// fix is the whole declaration - and a scan over that reaches a setter's throw first whenever `set` is
		// written before `get`, rewriting the accessor nobody complained about. Search only the body that was
		// actually classified. Returning null declines the fix, which is the right failure: no rewrite beats
		// the wrong one.
		static SyntaxNode? GetClassifiedBody(MemberDeclarationSyntax member)
		{
			switch (member)
			{
				case MethodDeclarationSyntax method:
					return method.Body ?? (SyntaxNode?)method.ExpressionBody;

				case PropertyDeclarationSyntax property:
					if (property.ExpressionBody is not null)
						return property.ExpressionBody;

					if (property.AccessorList is { } accessors)
						foreach (var accessor in accessors.Accessors)
							if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
								return accessor.Body ?? (SyntaxNode?)accessor.ExpressionBody;

					return null;

				default:
					return null;
			}
		}

		static ObjectCreationExpressionSyntax ServerSideOnlyExceptionCreation(ExpressionSyntax memberName)
		{
			var nameOf = SyntaxFactory.InvocationExpression(
				SyntaxFactory.IdentifierName("nameof"),
				SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
					SyntaxFactory.Argument(memberName))));

			return SyntaxFactory.ObjectCreationExpression(
					SyntaxFactory.ParseTypeName(ServerSideOnlyExceptionName)
						.WithLeadingTrivia(SyntaxFactory.Space)
						.WithAdditionalAnnotations(Simplifier.Annotation))
				.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(nameOf))));
		}

		// An explicit interface implementation is not in its containing type's declaration space, so a bare
		// nameof(M) there is CS0103. Qualify it with the interface the declaration already names - nameof(I.M)
		// still evaluates to "M", so the exception message is unchanged. Any other member kind declines.
		static ExpressionSyntax? GetNameOfArgument(MemberDeclarationSyntax member)
		{
			return member switch
			{
				MethodDeclarationSyntax method     => Qualify(method.ExplicitInterfaceSpecifier, method.Identifier),
				PropertyDeclarationSyntax property => Qualify(property.ExplicitInterfaceSpecifier, property.Identifier),
				_                                  => null,
			};

			static ExpressionSyntax Qualify(ExplicitInterfaceSpecifierSyntax? specifier, SyntaxToken identifier)
			{
				var name = SyntaxFactory.IdentifierName(identifier.ValueText);

				return specifier is null
					? name
					: SyntaxFactory.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						specifier.Name.WithoutTrivia(),
						name);
			}
		}

		// Solution-scoped rather than a DocumentBasedFixAllProvider, whose FixAllAsync returns a Document and so
		// cannot reach a fix target in another file - it would skip those sites while reporting success, and
		// `dotnet format --diagnostics` goes through Fix-All. Also not WellKnownFixAllProviders.BatchFixer:
		// BatchFixer computes each fix against the ORIGINAL tree and merges, so edits after the first go stale
		// when several diagnostics sit physically close - ten adjacent Overlaps overloads being exactly that
		// shape. Grouping by target document and rewriting each in one ReplaceNodes pass keeps both properties.
		sealed class ContractFixAllProvider : FixAllProvider
		{
			public static readonly ContractFixAllProvider Instance = new();

			public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
			{
				var diagnostics = await GetDiagnosticsAsync(fixAllContext).ConfigureAwait(false);

				if (diagnostics.IsEmpty)
					return null;

				// The provider fixes both rules, so the Fix-All menu text has to follow the diagnostic being
				// fixed rather than defaulting to the marker wording.
				var title = fixAllContext.DiagnosticIds.Contains(ServerSideOnlyContractAnalyzer.WrongExceptionDiagnosticId)
					? ReplaceExceptionTitle
					: AddMarkerTitle;

				return CodeAction.Create(
					title,
					ct => FixAllAsync(fixAllContext.Solution, diagnostics, ct),
					equivalenceKey: nameof(ContractFixAllProvider));
			}

			static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(FixAllContext fixAllContext)
			{
				switch (fixAllContext.Scope)
				{
					case FixAllScope.Document when fixAllContext.Document is not null:
						return await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false);

					case FixAllScope.Project:
						return await fixAllContext.GetAllDiagnosticsAsync(fixAllContext.Project).ConfigureAwait(false);

					case FixAllScope.Solution:
					{
						var builder = ImmutableArray.CreateBuilder<Diagnostic>();

						foreach (var project in fixAllContext.Solution.Projects)
							builder.AddRange(await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false));

						return builder.ToImmutable();
					}

					default:
						return ImmutableArray<Diagnostic>.Empty;
				}
			}

			static async Task<Solution> FixAllAsync(Solution solution, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
			{
				// Every rewrite is computed against the ORIGINAL tree of whichever document it lands in, and
				// applied one document at a time in a single ReplaceNodes pass, so no edit ever sees a tree
				// another edit mutated. Two implementations sharing one interface attribute collapse to one
				// entry rather than colliding.
				var byDocument = new Dictionary<DocumentId, Dictionary<SyntaxNode, SyntaxNode>>();

				foreach (var diagnostic in diagnostics)
				{
					var (documentId, original, replacement) = await ResolveAsync(solution, diagnostic, cancellationToken).ConfigureAwait(false);

					if (documentId is null || original is null || replacement is null)
						continue;

					if (!byDocument.TryGetValue(documentId, out var replacements))
					{
						replacements = new Dictionary<SyntaxNode, SyntaxNode>();
						byDocument.Add(documentId, replacements);
					}

					if (!replacements.ContainsKey(original))
						replacements.Add(original, replacement);
				}

				var updatedSolution = solution;

				foreach (var entry in byDocument)
				{
					var document = solution.GetDocument(entry.Key);

					if (document is null)
						continue;

					var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

					if (root is null)
						continue;

					var replacements = entry.Value;
					var updated      = document.WithSyntaxRoot(root.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]));
					var processed    = await PostProcessAsync(updated, cancellationToken).ConfigureAwait(false);
					var processedRoot = await processed.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

					if (processedRoot is not null)
						updatedSolution = updatedSolution.WithDocumentSyntaxRoot(entry.Key, processedRoot);
				}

				return updatedSolution;
			}

			// Which document the edit lands in, which is the diagnostic's own unless the analyzer pointed at a
			// declaration in another file.
			static async Task<(DocumentId? DocumentId, SyntaxNode? Original, SyntaxNode? Replacement)> ResolveAsync(
				Solution          solution,
				Diagnostic        diagnostic,
				CancellationToken cancellationToken)
			{
				var diagnosticTree = diagnostic.Location.SourceTree;

				if (diagnosticTree is null || solution.GetDocument(diagnosticTree) is not { } document)
					return default;

				diagnostic.Properties.TryGetValue(ServerSideOnlyContractAnalyzer.RemedyPropertyKey, out var remedy);

				var targetLocation = GetFixTargetLocation(diagnostic);

				if (targetLocation?.SourceTree is { } targetTree && targetTree != diagnosticTree)
				{
					if (solution.GetDocument(targetTree) is not { } targetDocument)
						return default;

					var targetRoot = await targetDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

					return targetRoot is not null && TryRewriteAt(targetRoot, targetLocation, remedy, out var crossOriginal, out var crossReplacement)
						? (targetDocument.Id, crossOriginal, crossReplacement)
						: default;
				}

				var root  = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
				var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

				return root is not null && model is not null && TryRewrite(root, model, diagnostic, out var original, out var replacement)
					? (document.Id, original, replacement)
					: default;
			}
		}
	}
}
