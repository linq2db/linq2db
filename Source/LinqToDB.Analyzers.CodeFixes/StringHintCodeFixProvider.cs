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
	/// one. That rewrite also gives the call the provider's type, so text hints chained after it bind the provider's
	/// own overloads and are restricted too; the title says so. Where the new type could break the code around the
	/// call (see <see cref="PlanRestricting"/>), the <c>var</c> local the call initializes keeps the original type, or
	/// the rewrite is not offered. The rewrite targets are read from <see cref="Diagnostic.Properties"/>, as in the
	/// other code fixes here.
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
			// All rewrites of a document are made in one pass, so a chain of hints gets one As*() call and one using.
			return FixAllProvider.Create(FixAllInDocumentAsync);
		}

		/// <inheritdoc/>
		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var root     = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (root is null)
				return;

			var diagnostic = context.Diagnostics[0];

			if (FindInvocation(root, diagnostic) is not { } invocation)
				return;

			var fixes = ReadFixes(diagnostic);
			var plan  = Plan.Keep;

			// The restricting rewrites of one call differ in the provider only; they share the plan.
			if (fixes.Count > 0 && fixes[0].GenericOverload)
			{
				var model = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

				if (model is null || PlanRestricting(invocation, model, context.CancellationToken) is not { } restricting)
					return;

				plan = restricting;
			}

			foreach (var fix in fixes)
			{
				var title = fix.GenericOverload
					? string.Format(CultureInfo.InvariantCulture, "Use {0} typed hint {1}() (applies to {0} only, as do text hints chained after it)", fix.Provider, fix.Helper)
					: string.Format(CultureInfo.InvariantCulture, "Use typed hint {0}()", fix.Helper);

				var item = new Item(invocation, fix, plan);

				context.RegisterCodeFix(
					CodeAction.Create(
						title,
						ct => RewriteAsync(document, [item], ct),
						equivalenceKey: GetEquivalenceKey(fix)),
					diagnostic);
			}
		}

		static async Task<Document?> FixAllInDocumentAsync(FixAllContext context, Document document, ImmutableArray<Diagnostic> diagnostics)
		{
			var cancellationToken = context.CancellationToken;
			var root              = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var model             = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

			if (root is null || model is null)
				return document;

			var items = new List<Item>();
			var seen  = new HashSet<InvocationExpressionSyntax>();

			foreach (var diagnostic in diagnostics.OrderBy(d => d.Location.SourceSpan.Start))
			{
				if (FindInvocation(root, diagnostic) is not { } invocation || !seen.Add(invocation))
					continue;

				var fix = ReadFixes(diagnostic).Find(f => string.Equals(GetEquivalenceKey(f), context.CodeActionEquivalenceKey, StringComparison.Ordinal));

				if (fix is null)
					continue;

				var plan = fix.GenericOverload ? PlanRestricting(invocation, model, cancellationToken) : Plan.Keep;

				if (plan is not null)
					items.Add(new Item(invocation, fix, plan));
			}

			return items.Count == 0 ? document : await RewriteAsync(document, items, cancellationToken).ConfigureAwait(false);
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

		// What a restricting rewrite changes besides the call: the `var` local whose type is written out, if any.
		sealed class Plan
		{
			public static readonly Plan Keep = new(null, null);

			public Plan(TypeSyntax? varType, TypeSyntax? explicitType)
			{
				VarType      = varType;
				ExplicitType = explicitType;
			}

			public TypeSyntax? VarType      { get; }
			public TypeSyntax? ExplicitType { get; }
		}

		sealed class Item
		{
			public Item(InvocationExpressionSyntax invocation, Fix fix, Plan plan)
			{
				Invocation = invocation;
				Fix        = fix;
				Plan       = plan;
			}

			public InvocationExpressionSyntax Invocation { get; }
			public Fix                        Fix        { get; }
			public Plan                       Plan       { get; }
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

		// Rewrites every item of one document in one pass. A restricting rewrite whose receiver is another item's call for
		// the same provider leaves out the As*() call: that receiver has the provider's type once rewritten.
		static async Task<Document> RewriteAsync(Document document, IReadOnlyList<Item> items, CancellationToken cancellationToken)
		{
			var root  = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

			if (root is null || model is null)
				return document;

			var byInvocation = new Dictionary<SyntaxNode, Item>();

			foreach (var item in items)
				byInvocation[item.Invocation] = item;

			var retypes = new Dictionary<SyntaxNode, TypeSyntax>();

			foreach (var item in items)
				if (item.Plan.VarType is { } varType && item.Plan.ExplicitType is { } explicitType)
					retypes[varType] = explicitType;

			var newRoot = root.ReplaceNodes(
				byInvocation.Keys.Concat(retypes.Keys),
				(original, rewritten) =>
				{
					if (retypes.TryGetValue(original, out var explicitType))
						return explicitType.WithTriviaFrom(original);

					var item        = byInvocation[original];
					var omitAs      = OmitsAsCall(item, byInvocation);
					var replacement = BuildReplacement(item.Invocation, (InvocationExpressionSyntax)rewritten, model, item.Fix, omitAs, cancellationToken);

					return replacement is null ? rewritten : replacement.WithTriviaFrom(rewritten);
				});

			if (newRoot is CompilationUnitSyntax unit)
			{
				var added = new HashSet<string>(StringComparer.Ordinal);

				foreach (var item in items)
				{
					var position = item.Invocation.SpanStart;

					unit = AddUsingIfMissing(unit, model, position, item.Fix.Namespace, added);

					if (item.Fix.AsQueryable)
						unit = AddUsingIfMissing(unit, model, position, SystemLinqNamespace, added);
				}

				newRoot = unit;
			}

			return document.WithSyntaxRoot(newRoot);
		}

		// t.TableHint("NOLOCK").TableHint("READPAST") fixed together: the outer call's receiver is the inner call, which
		// becomes t.AsSqlServer().WithNoLock(), so the outer one needs no second AsSqlServer(). Not when the outer call
		// needs AsQueryable() first: the inner rewrite then yields the provider's table type, not its query type.
		static bool OmitsAsCall(Item item, Dictionary<SyntaxNode, Item> byInvocation)
		{
			if (!item.Fix.GenericOverload || item.Fix.AsQueryable || item.Fix.AsMethod.Length == 0
				|| item.Invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
				return false;

			var receiver = memberAccess.Expression;

			while (receiver is ParenthesizedExpressionSyntax parenthesized)
				receiver = parenthesized.Expression;

			return receiver is InvocationExpressionSyntax inner
				&& byInvocation.TryGetValue(inner, out var innerItem)
				&& string.Equals(innerItem.Fix.Provider, item.Fix.Provider, StringComparison.Ordinal);
		}

		// A generic overload returns ITable<T> / IQueryable<T>, the typed helper the provider's own type, and so does every
		// hint chained after it. The new type converts to the old one, so it only matters where the expression's own type
		// is captured (CapturesType). If the chain initializes a `var` local whose later use needs the original type, the
		// local keeps it, written out (null when it cannot be written: an anonymous element type). If the chain itself sits
		// in such a position, the rewrite is not offered (null). The check reads the code and the symbols it binds to; it
		// does not compile the rewritten code.
		static Plan? PlanRestricting(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken)
		{
			var top = GetChainTop(invocation, model, cancellationToken);

			if (GetVarDeclaration(top) is not { } declaration)
				return CapturesType(top, model, cancellationToken) ? null : Plan.Keep;

			var (declarator, type) = declaration;

			if (model.GetDeclaredSymbol(declarator, cancellationToken) is not ILocalSymbol local)
				return null;

			if (!UseNeedsOriginalType(local, declarator, model, cancellationToken))
				return Plan.Keep;

			var original = model.GetTypeInfo(top, cancellationToken).Type;

			if (original is null || original.TypeKind == TypeKind.Error || ContainsAnonymousType(original))
				return null;

			return new Plan(type, SyntaxFactory.ParseTypeName(original.ToMinimalDisplayString(model, type.SpanStart)));
		}

		// The outermost expression whose type follows the call's: through parentheses, calls chained on it that return
		// their receiver's type (another hint, `Where` on a query) but not past one that returns another type
		// (`ToList()`), and out of `?.` when the chain is the part after it.
		static ExpressionSyntax GetChainTop(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken)
		{
			var node = expression;

			while (true)
			{
				switch (node.Parent)
				{
					case ParenthesizedExpressionSyntax parenthesized:
						node = parenthesized;
						break;
					case MemberAccessExpressionSyntax memberAccess when memberAccess.Expression == node && memberAccess.Parent is InvocationExpressionSyntax chained && chained.Expression == memberAccess:
						if (!HasSameType(chained, node, model, cancellationToken))
							return node;

						node = chained;
						break;
					case ConditionalAccessExpressionSyntax conditionalAccess when conditionalAccess.WhenNotNull == node || HasSameType(conditionalAccess, node, model, cancellationToken):
						node = conditionalAccess;
						break;
					default:
						return node;
				}
			}
		}

		static bool HasSameType(ExpressionSyntax a, ExpressionSyntax b, SemanticModel model, CancellationToken cancellationToken)
		{
			return SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(a, cancellationToken).Type, model.GetTypeInfo(b, cancellationToken).Type);
		}

		static (VariableDeclaratorSyntax declarator, TypeSyntax type)? GetVarDeclaration(ExpressionSyntax expression)
		{
			if (expression.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: true } declaration } declarator })
				return (declarator, declaration.Type);

			return null;
		}

		// Whether the expression's own type is captured where it stands, so the provider's type would end up in another
		// type or variable instead of converting to the one expected there: a branch of `?:` or `??`, a switch expression
		// arm, an element of `new[]`, a tuple or an anonymous object, a value matched by `is var x` / `is { } x`, a query's
		// `select` or `let`, the result of a lambda whose return type is inferred (also through an async lambda's Task<T>), an argument or `?.` / extension receiver bound to a parameter
		// typed as a bare type parameter of a generic method (`Wrap<T>(this T value)`). A parameter of a type built on the
		// type parameter (`IEnumerable<TInner>` of `Join`) infers the element type, which the provider's type keeps.
		static bool CapturesType(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken)
		{
			return expression.Parent switch
			{
				ConditionalExpressionSyntax conditional => conditional.Condition != expression,
				BinaryExpressionSyntax binary           => binary.IsKind(SyntaxKind.CoalesceExpression),
				SwitchExpressionArmSyntax arm           => arm.Expression == expression,

				InitializerExpressionSyntax { Parent: ImplicitArrayCreationExpressionSyntax } => true,
				AnonymousObjectMemberDeclaratorSyntax                                         => true,
				ArgumentSyntax { Parent: TupleExpressionSyntax }                              => true,

				IsPatternExpressionSyntax isPattern     => isPattern.Expression == expression && DesignatesValue(isPattern.Pattern),

				// Query syntax: `select` and `let` are the result of Select's lambda, whose TResult is inferred.
				SelectClauseSyntax or LetClauseSyntax => true,
				SwitchStatementSyntax switchStatement   => switchStatement.Sections.SelectMany(s => s.Labels).OfType<CasePatternSwitchLabelSyntax>().Any(l => DesignatesValue(l.Pattern)),
				SwitchExpressionSyntax switchExpression => switchExpression.GoverningExpression == expression && switchExpression.Arms.Any(a => DesignatesValue(a.Pattern)),

				AnonymousFunctionExpressionSyntax function => function.ExpressionBody == expression && InfersResultType(function, model, cancellationToken),
				ReturnStatementSyntax or YieldStatementSyntax =>
					expression.Parent.FirstAncestorOrSelf<SyntaxNode>(n => n is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or BaseMethodDeclarationSyntax or AccessorDeclarationSyntax) is AnonymousFunctionExpressionSyntax lambda
					&& InfersResultType(lambda, model, cancellationToken),

				ArgumentSyntax argument => BindsToBareTypeParameter(argument, model, cancellationToken),

				// A call chained on it that returns another type (GetChainTop stopped here): an extension method whose
				// receiver is a bare type parameter infers it from the provider's type.
				MemberAccessExpressionSyntax memberAccess when memberAccess.Expression == expression && memberAccess.Parent is InvocationExpressionSyntax call && call.Expression == memberAccess =>
					model.GetSymbolInfo(call, cancellationToken).Symbol is IMethodSymbol { ReducedFrom: { } definition }
					&& IsMethodTypeParameter(definition.Parameters[0].Type),

				// t.TableHint("…")?.Call() returning another type: the call is not looked into, so it counts.
				ConditionalAccessExpressionSyntax conditionalAccess => conditionalAccess.Expression == expression,

				_ => false,
			};
		}

		// `var x` or `{ } x` (a designation of the whole value with its own type), also inside `and` / `or` / `not` / parentheses.
		static bool DesignatesValue(PatternSyntax pattern)
		{
			return pattern switch
			{
				VarPatternSyntax                         => true,
				DeclarationPatternSyntax declaration     => declaration.Type.IsVar,
				ParenthesizedPatternSyntax parenthesized => DesignatesValue(parenthesized.Pattern),
				BinaryPatternSyntax binary               => DesignatesValue(binary.Left) || DesignatesValue(binary.Right),
				UnaryPatternSyntax unary                 => DesignatesValue(unary.Pattern),
				RecursivePatternSyntax recursive         => recursive.Type is null && recursive.Designation is SingleVariableDesignationSyntax,
				_                                        => false,
			};
		}

		// The lambda's return type is inferred: in a `var` local (natural type), or passed to a generic method whose
		// delegate parameter returns a bare type parameter (`Select`'s `TResult`, not `SelectMany`'s `IEnumerable<TCollection>`).
		static bool InfersResultType(AnonymousFunctionExpressionSyntax function, SemanticModel model, CancellationToken cancellationToken)
		{
			SyntaxNode node = function;

			while (node.Parent is ParenthesizedExpressionSyntax)
				node = node.Parent;

			switch (node.Parent)
			{
				case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } }:
					return declaration.Type.IsVar;
				case ArgumentSyntax argument:
					if (GetParameter(argument, model, cancellationToken) is not { } parameter)
						return true;

					var type = parameter.OriginalDefinition.Type;

					// Expression<TDelegate>
					if (type is INamedTypeSymbol { Name: "Expression", TypeArguments.Length: 1 } expressionType
						&& string.Equals(expressionType.ContainingNamespace?.ToDisplayString(), "System.Linq.Expressions", StringComparison.Ordinal))
						type = expressionType.TypeArguments[0];

					if (type is not INamedTypeSymbol { DelegateInvokeMethod: { } invoke })
						return false;

					// An async lambda's result is the argument of the delegate's Task<T> / ValueTask<T>.
					var result = invoke.ReturnType;

					if (function.AsyncKeyword != default && result is INamedTypeSymbol { Name: "Task" or "ValueTask", TypeArguments.Length: 1 } task)
						result = task.TypeArguments[0];

					return IsMethodTypeParameter(result);
				default:
					return false;
			}
		}

		// An argument bound to a parameter typed as a bare type parameter of the method (or `params T[]`); also when the
		// call does not bind, since nothing better is known.
		static bool BindsToBareTypeParameter(ArgumentSyntax argument, SemanticModel model, CancellationToken cancellationToken)
		{
			if (argument.Parent is not ArgumentListSyntax { Parent: InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax })
				return false;

			if (GetParameter(argument, model, cancellationToken) is not { } parameter)
				return true;

			var type = parameter.OriginalDefinition.Type;

			if (parameter.IsParams && type is IArrayTypeSymbol array)
				type = array.ElementType;

			return IsMethodTypeParameter(type);
		}

		static IParameterSymbol? GetParameter(ArgumentSyntax argument, SemanticModel model, CancellationToken cancellationToken)
		{
			return model.GetOperation(argument, cancellationToken) is IArgumentOperation { Parameter: { } parameter } ? parameter : null;
		}

		static bool IsMethodTypeParameter(ITypeSymbol type)
		{
			return type is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method };
		}

		// Whether a use of the local needs its original type: a write to it, a use in a type-inferring position, or one
		// that initializes another `var` local, directly or through a chain on it.
		static bool UseNeedsOriginalType(ILocalSymbol local, VariableDeclaratorSyntax declarator, SemanticModel model, CancellationToken cancellationToken)
		{
			var scope = declarator.FirstAncestorOrSelf<BlockSyntax>() ?? (SyntaxNode)declarator.SyntaxTree.GetRoot(cancellationToken);

			foreach (var identifier in scope.DescendantNodes().OfType<IdentifierNameSyntax>())
			{
				if (!string.Equals(identifier.Identifier.ValueText, local.Name, StringComparison.Ordinal)
					|| !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier, cancellationToken).Symbol, local))
					continue;

				if (IsWriteTarget(identifier))
					return true;

				var top = GetChainTop(identifier, model, cancellationToken);

				if (GetVarDeclaration(top) is not null || CapturesType(top, model, cancellationToken))
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

		// `original` is the call in the document the model belongs to (for binding); `current` is the same call with any
		// rewrites inside it already made (for the syntax the replacement is built from).
		static ExpressionSyntax? BuildReplacement(InvocationExpressionSyntax original, InvocationExpressionSyntax current, SemanticModel model, Fix fix, bool omitAs, CancellationToken cancellationToken)
		{
			var names = new List<string>();

			if (!omitAs)
			{
				if (fix.AsQueryable)
					names.Add(AsQueryableName);

				if (fix.AsMethod.Length > 0)
					names.Add(fix.AsMethod);
			}

			names.Add(fix.Helper);

			var isReducedCall = model.GetSymbolInfo(original, cancellationToken).Symbol is IMethodSymbol { ReducedFrom: not null };

			if (isReducedCall && current.Expression is MemberAccessExpressionSyntax memberAccess)
			{
				// receiver<trivia>.TableHint("…") -> receiver<trivia>.AsSqlServer().WithNoLock(): the first call reuses the
				// original dot and its trivia, so a chain broken over lines keeps its layout.
				ExpressionSyntax result = SyntaxFactory.InvocationExpression(
					memberAccess.WithName(Name(names[0]).WithTriviaFrom(memberAccess.Name)),
					EmptyArguments());

				for (var i = 1; i < names.Count; i++)
					result = Call(result, names[i]);

				return result;
			}

			if (!isReducedCall
				&& model.GetOperation(original, cancellationToken) is IInvocationOperation operation
				&& operation.Arguments.FirstOrDefault(a => a.Parameter?.Ordinal == 0)?.Syntax is ArgumentSyntax receiverArgument)
			{
				// Static form: the receiver is the argument bound to the first parameter, which a named argument can
				// put anywhere in the list (TableHint(hint: "NOLOCK", table: t)).
				var index = original.ArgumentList.Arguments.IndexOf(receiverArgument);

				if (index < 0 || index >= current.ArgumentList.Arguments.Count)
					return null;

				var result = current.ArgumentList.Arguments[index].Expression.WithoutTrivia();

				if (result is not (IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax or ElementAccessExpressionSyntax or ThisExpressionSyntax or ParenthesizedExpressionSyntax))
					result = SyntaxFactory.ParenthesizedExpression(result);

				foreach (var name in names)
					result = Call(result, name);

				return result;
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

		static CompilationUnitSyntax AddUsingIfMissing(CompilationUnitSyntax unit, SemanticModel model, int position, string ns, HashSet<string> added)
		{
			if (added.Contains(ns) || IsImported(model, position, ns))
				return unit;

			added.Add(ns);

			// Leaves the original file's line ending on the new directive.
			var endOfLine = unit.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));

			if (endOfLine == default)
				endOfLine = SyntaxFactory.CarriageReturnLineFeed;

			var directive = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns))
				.WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space))
				.WithTrailingTrivia(endOfLine);

			var usings = unit.Usings;

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

			var index = usings.Count;

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

			// Take over the leading trivia (blank-line grouping, file header) of the directive it goes in front of, unless it
			// sorts last in the previous group of usings grouped by blank lines (by first namespace segment, or System and
			// the rest): then it stays there, and the next group keeps its leading blank line.
			var group    = GetGroup(ns);
			var takeOver = index < usings.Count
				&& (index == firstLocal
					|| !string.Equals(group, GetGroup(usings[index - 1]), StringComparison.Ordinal)
					|| string.Equals(group, GetGroup(usings[index]), StringComparison.Ordinal));

			// The depth of conditional blocks (#if) before each directive's leading trivia; depth[Count] after the last one.
			var depth = new int[usings.Count + 1];

			for (var i = 0; i < usings.Count; i++)
				depth[i + 1] = depth[i] + GetConditionalDepthChange(usings[i].GetLeadingTrivia());

			// A using inside a conditional block exists in one configuration only. Where the sorted place is inside one, the
			// directive goes after the block's #endif (in front of the next directive, or of the code after the usings),
			// else in front of the block, else below the file header.
			if (depth[takeOver ? index + 1 : index] == 0)
				return InsertUsing(unit, index, directive, takeOver);

			if (index < usings.Count && depth[takeOver ? index : index + 1] == 0)
				return InsertUsing(unit, index, directive, !takeOver);

			if (index == usings.Count)
			{
				var next    = usings[usings.Count - 1].GetLastToken().GetNextToken(includeZeroWidth: true);
				var closeAt = GetBlockCloseEnd(next.LeadingTrivia, depth[usings.Count]);

				if (closeAt > 0)
				{
					unit = unit.ReplaceToken(next, next.WithLeadingTrivia(next.LeadingTrivia.Skip(closeAt)));

					return unit.WithUsings(unit.Usings.Add(directive.WithLeadingTrivia(next.LeadingTrivia.Take(closeAt))));
				}
			}

			for (var i = Math.Min(index, usings.Count) - 1; i >= Math.Max(firstLocal, 1); i--)
				if (depth[i] == 0)
					return InsertUsing(unit, i, directive, takeOver: false);

			if (firstLocal == 0)
			{
				var trivia    = usings[0].GetLeadingTrivia();
				var headerEnd = GetHeaderEnd(trivia);

				if (GetConditionalDepthChange(trivia.Take(headerEnd)) == 0)
				{
					usings = usings.Replace(usings[0], usings[0].WithLeadingTrivia(trivia.Skip(headerEnd)));

					return unit.WithUsings(usings.Insert(0, directive.WithLeadingTrivia(trivia.Take(headerEnd))));
				}
			}

			return InsertUsing(unit, index, directive, takeOver);
		}

		static CompilationUnitSyntax InsertUsing(CompilationUnitSyntax unit, int index, UsingDirectiveSyntax directive, bool takeOver)
		{
			var usings = unit.Usings;

			if (takeOver)
			{
				var next = usings[index];

				directive = directive.WithLeadingTrivia(next.GetLeadingTrivia());
				usings    = usings.Replace(next, next.WithoutLeadingTrivia());
			}

			return unit.WithUsings(usings.Insert(index, directive));
		}

		static int GetConditionalDepthChange(IEnumerable<SyntaxTrivia> trivia)
		{
			var change = 0;

			foreach (var item in trivia)
			{
				if (item.IsKind(SyntaxKind.IfDirectiveTrivia))
					change++;
				else if (item.IsKind(SyntaxKind.EndIfDirectiveTrivia))
					change--;
			}

			return change;
		}

		// The length of the trivia up to the #endif that closes the conditional blocks open at its start (`depth` of
		// them); 0 when they stay open.
		static int GetBlockCloseEnd(SyntaxTriviaList trivia, int depth)
		{
			for (var i = 0; i < trivia.Count; i++)
			{
				if (trivia[i].IsKind(SyntaxKind.IfDirectiveTrivia))
					depth++;
				else if (trivia[i].IsKind(SyntaxKind.EndIfDirectiveTrivia) && --depth == 0)
					return i + 1;
			}

			return 0;
		}

		// The file header is the leading trivia up to its last blank line or preprocessor directive. A conditional block
		// still open at the end of the trivia encloses what follows, so the header ends at the line of its #if: a using
		// inside the block would exist in one configuration only. A block closed within the header stays part of it. A
		// #define or #undef inside the open block must precede every using (CS1032), so then the header ends after it.
		static int GetHeaderEnd(SyntaxTriviaList trivia)
		{
			var end        = 0;
			var lineEmpty  = true;
			var depth      = 0;
			var openAt     = -1;
			var lastDefine = -1;

			for (var i = 0; i < trivia.Count; i++)
			{
				var item = trivia[i];

				if (item.IsKind(SyntaxKind.IfDirectiveTrivia))
				{
					if (depth++ == 0)
					{
						// The #if line starts after the indentation in front of it.
						openAt = i;

						while (openAt > 0 && trivia[openAt - 1].IsKind(SyntaxKind.WhitespaceTrivia))
							openAt--;
					}
				}
				else if (item.IsKind(SyntaxKind.EndIfDirectiveTrivia) && depth > 0)
				{
					depth--;
				}
				else if (item.IsKind(SyntaxKind.DefineDirectiveTrivia) || item.IsKind(SyntaxKind.UndefDirectiveTrivia))
				{
					lastDefine = i;
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

			return depth > 0 && lastDefine < openAt ? openAt : end;
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
