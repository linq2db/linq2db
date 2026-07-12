using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace LinqToDB.Analyzers.CodeFixes
{
	/// <summary>
	/// Rewrites a legacy <c>Sql.Ext.&lt;Fn&gt;(...)...Over()...ToValue()</c> chain into the equivalent
	/// <c>Sql.Window.&lt;Fn&gt;(&lt;args&gt;, f =&gt; f....)</c> call. Mirrors the internal
	/// <c>LegacyMemberConverterBase.TryConvertAnalyticFunction</c> mapping, but at the syntax level so the fix
	/// can preserve the user's argument expressions (and their trivia) verbatim. Returns <see langword="null"/> when the
	/// chain has no direct mechanical <c>Sql.Window</c> equivalent (the diagnostic then stays without a fix).
	/// </summary>
	internal static class LegacyWindowChainRewriter
	{
		const string AnalyticFunctionsTypeName = "AnalyticFunctions";

		// Functions that may carry a ROWS/RANGE frame in the new pipeline (ranking / LEAD / LAG cannot).
		static readonly HashSet<string> FrameableFunctions = new(StringComparer.Ordinal)
		{
			"Sum", "Average", "Min", "Max", "Count", "LongCount", "FirstValue", "LastValue", "NthValue",
			"StdDev", "StdDevPop", "StdDevSamp", "Variance", "VarPop", "VarSamp",
			"CovarPop", "CovarSamp", "Corr",
			"RegrSlope", "RegrIntercept", "RegrCount", "RegrR2", "RegrAvgX", "RegrAvgY", "RegrSXX", "RegrSYY", "RegrSXY",
		};

		// Aggregates that may take an Oracle KEEP (KeepFirst/KeepLast) modifier.
		static readonly HashSet<string> KeepableFunctions = new(StringComparer.Ordinal)
		{
			"Sum", "Average", "Min", "Max", "Count", "LongCount",
			"StdDev", "StdDevPop", "StdDevSamp", "Variance", "VarPop", "VarSamp",
		};

		// Functions whose OVER carries PARTITION BY only (order / frame are ignored).
		static readonly HashSet<string> PartitionOnlyFunctions = new(StringComparer.Ordinal) { "Median", "RatioToReport" };

		// Windowed ordered-set aggregates: WITHIN GROUP (ORDER BY ...) OVER (PARTITION BY ...).
		static readonly HashSet<string> WindowedOrderedSetFunctions = new(StringComparer.Ordinal) { "PercentileCont", "PercentileDisc" };

		// Every function this rewriter can convert (union of the uniform families and the special-shape ones).
		static readonly HashSet<string> ConvertibleFunctions = new(StringComparer.Ordinal)
		{
			"RowNumber", "Rank", "DenseRank", "PercentRank", "CumeDist", "NTile",
			"Sum", "Average", "Min", "Max", "Count", "LongCount",
			"StdDev", "StdDevPop", "StdDevSamp", "Variance", "VarPop", "VarSamp",
			"CovarPop", "CovarSamp", "Corr",
			"RegrSlope", "RegrIntercept", "RegrCount", "RegrR2", "RegrAvgX", "RegrAvgY", "RegrSXX", "RegrSYY", "RegrSXY",
			"Lead", "Lag",
			"FirstValue", "LastValue", "NthValue",
			"Median", "RatioToReport", "PercentileCont", "PercentileDisc",
		};

		sealed class OrderClause
		{
			public bool               IsDescending;
			public ArgumentListSyntax Args = null!;
		}

		// One fluent step in the rebuilt builder lambda: a method call (Args != null) or a property access (Args == null).
		readonly struct Step
		{
			public Step(string name, ArgumentListSyntax? args) { Name = name; Args = args; }
			public string              Name { get; }
			public ArgumentListSyntax? Args { get; }
		}

		public static ExpressionSyntax? TryRewrite(InvocationExpressionSyntax toValueInvocation, SemanticModel model, CancellationToken cancellationToken, bool ignoreReturnTypeMismatch = false)
		{
			if (toValueInvocation.Expression is not MemberAccessExpressionSyntax toValueAccess)
				return null;

			// The package carries no linq2db dependency, so it can be installed alongside a linq2db too old to
			// have the Sql.Window API. WindowFunctionBuilder holds every extension method the rewrite emits; when
			// it's absent, don't rewrite compiling code into a call to an API that doesn't exist in this compilation.
			if (model.Compilation.GetTypeByMetadataName("LinqToDB.WindowFunctionBuilder") is null)
				return null;

			// Collected walking ToValue -> root; ordering lists are reversed to natural order before building.
			var orderClauses    = new List<OrderClause>();
			List<OrderClause>? keepOrderClauses       = null;
			List<OrderClause>? withinGroupOrderClauses = null;

			ArgumentListSyntax? partitionArgs = null;

			var    sawOver     = false;
			var    keepSeen    = false;
			var    isKeepFirst = false;
			var    frameSeen   = false;
			var    frameIsRange = false;
			string? frameStart = null;
			string? frameEnd   = null;
			ArgumentSyntax? frameStartValue = null;
			ArgumentSyntax? frameEndValue   = null;

			ExpressionSyntax? current = toValueAccess.Expression;

			while (current is not null)
			{
				switch (current)
				{
					case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax ma } inv:
					{
						if (model.GetSymbolInfo(inv, cancellationToken).Symbol is not IMethodSymbol method)
							return null;

						var name = ma.Name.Identifier.Text;

						// KeepFirst/KeepLast and the root analytic function are all declared on the AnalyticFunctions class.
						if (IsAnalyticFunctionsClass(method.ContainingType))
						{
							if (name is "KeepFirst" or "KeepLast")
							{
								keepSeen        = true;
								isKeepFirst     = string.Equals(name, "KeepFirst", StringComparison.Ordinal);
								keepOrderClauses = orderClauses;              // orders collected so far belong to KEEP
								orderClauses     = new List<OrderClause>();
								current          = ma.Expression;
								continue;
							}

							return BuildFromRoot(
								toValueInvocation, model, inv, ma, sawOver,
								partitionArgs, orderClauses, keepSeen, isKeepFirst, keepOrderClauses, withinGroupOrderClauses,
								frameSeen, frameIsRange, frameStart, frameStartValue, frameEnd, frameEndValue, ignoreReturnTypeMismatch);
						}

						if (!IsAnalyticNestedType(method.ContainingType))
							return null;

						switch (name)
						{
							case "Over":
								sawOver = true;
								break;
							case "PartitionBy":
								partitionArgs = inv.ArgumentList;
								break;
							case "OrderBy" or "OrderByDesc" or "ThenBy" or "ThenByDesc":
								orderClauses.Add(new OrderClause { IsDescending = name.EndsWith("Desc", StringComparison.Ordinal), Args = inv.ArgumentList });
								break;
							case "ValuePreceding" or "ValueFollowing":
							{
								frameSeen = true;
								var argCount = inv.ArgumentList.Arguments.Count;
								var value    = argCount > 0 ? inv.ArgumentList.Arguments[argCount - 1] : null;
								ApplyFrameBoundary(method.ContainingType, name, value, ref frameStart, ref frameStartValue, ref frameEnd, ref frameEndValue);
								break;
							}
							default:
								return null; // Filter, ListAgg, and anything unrecognized: no mechanical conversion
						}

						current = ma.Expression;
						continue;
					}

					case MemberAccessExpressionSyntax ma:
					{
						if (model.GetSymbolInfo(ma, cancellationToken).Symbol is not { } symbol || !IsAnalyticNestedType(symbol.ContainingType))
							return null;

						var name = ma.Name.Identifier.Text;

						switch (name)
						{
							case "Rows":
								frameSeen = true;
								break;
							case "Range":
								frameSeen    = true;
								frameIsRange = true;
								break;
							case "Between" or "And":
								break; // markers
							case "WithinGroup":
								// Ordered-set: the OrderBy collected so far is the WITHIN GROUP order, not the OVER order.
								withinGroupOrderClauses = orderClauses;
								orderClauses            = new List<OrderClause>();
								break;
							case "UnboundedPreceding" or "UnboundedFollowing" or "CurrentRow":
								frameSeen = true;
								ApplyFrameBoundary(symbol.ContainingType, name, null, ref frameStart, ref frameStartValue, ref frameEnd, ref frameEndValue);
								break;
							default:
								return null;
						}

						current = ma.Expression;
						continue;
					}

					default:
						return null;
				}
			}

			return null;
		}

		static void ApplyFrameBoundary(
			INamedTypeSymbol containingType, string legacyName, ArgumentSyntax? value,
			ref string? frameStart, ref ArgumentSyntax? frameStartValue,
			ref string? frameEnd,   ref ArgumentSyntax? frameEndValue)
		{
			var mapped = legacyName switch
			{
				"UnboundedPreceding" or "UnboundedFollowing" => "Unbounded",
				"CurrentRow"                                  => "CurrentRow",
				"ValuePreceding"                              => "ValuePreceding",
				"ValueFollowing"                              => "ValueFollowing",
				_                                             => (string?)null,
			};

			if (mapped is null)
				return;

			// ISecondBoundaryExpected declares the *end* boundary; every other boundary interface is the *start*.
			if (string.Equals(containingType.Name, "ISecondBoundaryExpected", StringComparison.Ordinal))
			{
				frameEnd      = mapped;
				frameEndValue = value;
			}
			else
			{
				frameStart      = mapped;
				frameStartValue = value;
			}
		}

		static ExpressionSyntax? BuildFromRoot(
			InvocationExpressionSyntax toValueInvocation,
			SemanticModel model,
			InvocationExpressionSyntax rootInvocation,
			MemberAccessExpressionSyntax rootAccess,
			bool sawOver,
			ArgumentListSyntax? partitionArgs,
			List<OrderClause> orderClauses,
			bool keepSeen, bool isKeepFirst, List<OrderClause>? keepOrderClauses, List<OrderClause>? withinGroupOrderClauses,
			bool frameSeen, bool frameIsRange, string? frameStart, ArgumentSyntax? frameStartValue, string? frameEnd, ArgumentSyntax? frameEndValue,
			bool ignoreReturnTypeMismatch)
		{
			var functionName = rootAccess.Name.Identifier.Text;

			// Plain aggregate without OVER has no Sql.Window equivalent; unknown/irregular functions bail.
			if (!sawOver || !ConvertibleFunctions.Contains(functionName))
				return null;

			// Split the root call's arguments into positional value args and the special modifier args
			// (AggregateModifier / Nulls / From / NullsPosition) that become builder calls. Map each written
			// argument to its parameter through the semantic model rather than by source position: C# named
			// arguments can be reordered, so a positional match would misclassify a modifier as a value arg (or
			// vice-versa). Each argument maps to its parameter, so value args are collected with their parameter
			// ordinal and sorted into declaration order — the order Sql.Window expects.
			var valueArgsByOrdinal = new List<(int Ordinal, ArgumentSyntax Arg)>();
			string? distinct = null, nullTreatment = null, fromPosition = null;

			if (model.GetOperation(rootInvocation) is not IInvocationOperation rootOperation)
				return null;

			foreach (var argument in rootOperation.Arguments)
			{
				// Only arguments the caller actually wrote map to a value slot / builder step; skip compiler-
				// supplied defaults and params-array synthesis (and anything without a resolved parameter).
				if (argument.ArgumentKind != ArgumentKind.Explicit
					|| argument.Parameter is null
					|| argument.Syntax is not ArgumentSyntax argSyntax)
					continue;

				switch (argument.Parameter.Type.Name)
				{
					case "AggregateModifier":
						var mod = EnumMemberName(argSyntax.Expression, argument.Parameter.Type, model);
						if (mod is null) return null;
						if (string.Equals(mod, "Distinct", StringComparison.Ordinal)) distinct = "Distinct";
						break;
					case "Nulls":
						var nulls = EnumMemberName(argSyntax.Expression, argument.Parameter.Type, model);
						if (nulls is null) return null;
						if (string.Equals(nulls, "Ignore",  StringComparison.Ordinal)) nullTreatment = "IgnoreNulls";
						if (string.Equals(nulls, "Respect", StringComparison.Ordinal)) nullTreatment = "RespectNulls";
						break;
					case "From":
						var from = EnumMemberName(argSyntax.Expression, argument.Parameter.Type, model);
						if (from is null) return null;
						if (string.Equals(from, "First", StringComparison.Ordinal)) fromPosition = "FromFirst";
						if (string.Equals(from, "Last",  StringComparison.Ordinal)) fromPosition = "FromLast";
						break;
					case "NullsPosition":
						break;
					default:
						// Emitted positionally in Sql.Window arg order — drop any name-colon so a reordered
						// named call doesn't carry a now-wrong parameter name into the rewritten call.
						valueArgsByOrdinal.Add((argument.Parameter.Ordinal, argSyntax.NameColon is null ? argSyntax : argSyntax.WithNameColon(null)));
						break;
				}
			}

			var valueArgs = valueArgsByOrdinal.OrderBy(t => t.Ordinal).Select(t => t.Arg).ToList();

			// Assemble the ordered builder steps for whichever function shape this is.
			var steps = new List<Step>();

			if (keepSeen)
			{
				// KEEP: f.KeepFirst()/KeepLast().OrderBy(...)[.ThenBy...].PartitionBy(...)   (order is mandatory).
				// A DISTINCT modifier has no equivalent here — the Sql.Window KEEP builder exposes no .Distinct()
				// (Distinct and Keep are mutually-exclusive states) — so bail rather than drop it and change results.
				if (!KeepableFunctions.Contains(functionName) || keepOrderClauses is not { Count: > 0 } || distinct is not null)
					return null;

				steps.Add(new Step(isKeepFirst ? "KeepFirst" : "KeepLast", SyntaxFactory.ArgumentList()));
				AddOrderSteps(steps, keepOrderClauses);
				if (partitionArgs is not null)
					steps.Add(new Step("PartitionBy", partitionArgs));
			}
			else if (PartitionOnlyFunctions.Contains(functionName))
			{
				// Median / RatioToReport: OVER carries PARTITION BY only (order / frame ignored).
				if (valueArgs.Count == 0)
					return null;
				if (partitionArgs is not null)
					steps.Add(new Step("PartitionBy", partitionArgs));
			}
			else if (WindowedOrderedSetFunctions.Contains(functionName))
			{
				// PercentileCont/Disc: f.OrderBy(k)[.ThenBy...].PartitionBy(...). The within-group order is mandatory;
				// the group form (no OVER) already bailed above via !sawOver.
				if (valueArgs.Count == 0 || withinGroupOrderClauses is not { Count: > 0 })
					return null;

				// PercentileCont takes a single ordering key; only PercentileDisc allows several.
				if (string.Equals(functionName, "PercentileCont", StringComparison.Ordinal) && withinGroupOrderClauses.Count > 1)
					return null;

				AddOrderSteps(steps, withinGroupOrderClauses);
				if (partitionArgs is not null)
					steps.Add(new Step("PartitionBy", partitionArgs));
			}
			else
			{
				// Uniform families: [Distinct][From][Nulls][PartitionBy][order...][frame].
				if (frameSeen && (frameStart is null || !FrameableFunctions.Contains(functionName)))
					return null;

				if (distinct      is not null) steps.Add(new Step("Distinct",      SyntaxFactory.ArgumentList()));
				if (fromPosition  is not null) steps.Add(new Step(fromPosition,    SyntaxFactory.ArgumentList()));
				if (nullTreatment is not null) steps.Add(new Step(nullTreatment,   SyntaxFactory.ArgumentList()));
				if (partitionArgs is not null) steps.Add(new Step("PartitionBy",   partitionArgs));

				AddOrderSteps(steps, orderClauses);
			}

			// --- Emit: build the scaffold with placeholder args, format it, then splice the originals back in. ---
			var placeholders = new Dictionary<string, SyntaxNode>(StringComparer.Ordinal);
			var counter      = 0;

			string NextId() => "__l2db_" + counter++.ToString(CultureInfo.InvariantCulture);

			ArgumentListSyntax Placeholderize(ArgumentListSyntax original)
			{
				var args = original.Arguments.Select(a =>
				{
					var id = NextId();
					placeholders[id] = a.Expression;
					return a.WithExpression(SyntaxFactory.IdentifierName(id));
				});
				return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(args));
			}

			ArgumentSyntax PlaceholderizeArg(ArgumentSyntax original)
			{
				var id = NextId();
				placeholders[id] = original.Expression;
				return original.WithExpression(SyntaxFactory.IdentifierName(id));
			}

			ExpressionSyntax body = SyntaxFactory.IdentifierName("f");

			foreach (var step in steps)
				body = step.Args is null
					? SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, body, SyntaxFactory.IdentifierName(step.Name))
					: SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, body, SyntaxFactory.IdentifierName(step.Name)), Placeholderize(step.Args));

			if (frameSeen && !keepSeen && !PartitionOnlyFunctions.Contains(functionName) && !WindowedOrderedSetFunctions.Contains(functionName))
			{
				ExpressionSyntax Prop(string name) => SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, body, SyntaxFactory.IdentifierName(name));

				body = Prop(frameIsRange ? "RangeBetween" : "RowsBetween");
				body = frameStartValue is not null
					? SyntaxFactory.InvocationExpression((MemberAccessExpressionSyntax)Prop(frameStart!), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(PlaceholderizeArg(frameStartValue))))
					: Prop(frameStart!);
				body = Prop("And");
				var endMember = frameEnd ?? "CurrentRow"; // single-boundary legacy form -> ... And CurrentRow
				body = frameEndValue is not null
					? SyntaxFactory.InvocationExpression((MemberAccessExpressionSyntax)Prop(endMember), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(PlaceholderizeArg(frameEndValue))))
					: Prop(endMember);
			}

			// Sql.Window.<Fn>(<valueArgs>, f => <body>) — reuse the user's Sql qualifier from `<Sql>.Ext`.
			// A bare `Ext.<Fn>` root (via `using static LinqToDB.Sql;`) has an identifier receiver, not `<Sql>.Ext`,
			// so there's no qualifier to reuse — bail to no-fix (the diagnostic still reports).
			if (rootAccess.Expression is not MemberAccessExpressionSyntax rootQualifierAccess)
				return null;

			var sqlQualifier = rootQualifierAccess.Expression;
			var windowAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
				SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, sqlQualifier.WithoutTrivia(), SyntaxFactory.IdentifierName("Window")),
				SyntaxFactory.IdentifierName(functionName));

			var lambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier("f")), body);

			var callArgs = new List<ArgumentSyntax>();
			foreach (var a in valueArgs)
				callArgs.Add(PlaceholderizeArg(a));
			callArgs.Add(SyntaxFactory.Argument(lambda));

			ExpressionSyntax newExpression = SyntaxFactory.InvocationExpression(windowAccess, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(callArgs)));

			newExpression = newExpression.NormalizeWhitespace();
			newExpression = newExpression.ReplaceNodes(
				newExpression.DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => placeholders.ContainsKey(n.Identifier.Text)),
				(original, _) => placeholders[original.Identifier.Text]);

			// The Sql.Window overload's return type may differ from the legacy ToValue<TR>() slot (ranking returns
			// long/double, statistical/percentile families differ), so a mechanical rewrite can be a narrowing
			// assignment that won't compile. Only offer the fix when the rewritten call's type fits the target slot —
			// unless the user opted into applying it regardless (linq2db.L2DB1001.apply_fix_on_return_type_mismatch),
			// electing to resolve any resulting type error by hand.
			if (!ignoreReturnTypeMismatch && !ReturnTypeFitsTarget(toValueInvocation, newExpression, model))
				return null;

			// Preserve outer trivia and salvage any comments that lived on the original chain's scaffolding so none is lost.
			var firstToken = toValueInvocation.GetFirstToken();
			var lastToken  = toValueInvocation.GetLastToken();

			var interiorComments = toValueInvocation.DescendantTokens()
				.SelectMany(t => t.LeadingTrivia.Concat(t.TrailingTrivia))
				.Where(t => (t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
					&& !firstToken.LeadingTrivia.Contains(t)
					&& !lastToken.TrailingTrivia.Contains(t)
					&& !placeholders.Values.Any(v => v.FullSpan.Contains(t.FullSpan)))
				.ToList();

			newExpression = newExpression.WithLeadingTrivia(firstToken.LeadingTrivia);

			var trailing = new List<SyntaxTrivia>();
			foreach (var comment in interiorComments)
			{
				trailing.Add(SyntaxFactory.Space);
				trailing.Add(comment);
				// A single-line comment runs to the end of its line; without a terminator the token that follows the
				// rewritten expression (the statement's ';') would be swallowed into the comment and stop compiling.
				if (comment.IsKind(SyntaxKind.SingleLineCommentTrivia))
					trailing.Add(SyntaxFactory.CarriageReturnLineFeed);
			}

			trailing.AddRange(lastToken.TrailingTrivia);

			return newExpression.WithTrailingTrivia(SyntaxFactory.TriviaList(trailing));
		}

		// Appends OrderBy/ThenBy(+Desc) steps: the first ordering uses OrderBy, subsequent ones ThenBy.
		static void AddOrderSteps(List<Step> steps, List<OrderClause> clauses)
		{
			var natural = Enumerable.Reverse(clauses).ToList(); // collected ToValue->root; natural order is the reverse

			for (var i = 0; i < natural.Count; i++)
			{
				var method = (i == 0 ? "OrderBy" : "ThenBy") + (natural[i].IsDescending ? "Desc" : "");
				steps.Add(new Step(method, natural[i].Args));
			}
		}

		// True when the rewritten Sql.Window call's return type is accepted at the original expression's position:
		// it converts implicitly to the target slot, the target is unknown, or the context infers its type from the
		// expression and the rewritten type is identical to the legacy one. The rewritten expression is bound
		// speculatively at the original position so the check covers every family without enumerating return types.
		static bool ReturnTypeFitsTarget(ExpressionSyntax legacyExpression, ExpressionSyntax rewritten, SemanticModel model)
		{
			var newType = model.GetSpeculativeTypeInfo(legacyExpression.SpanStart, rewritten, SpeculativeBindingOption.BindAsExpression).Type;
			if (newType is null || newType.TypeKind == TypeKind.Error)
				return false;

			// A type-inferred context (var initializer / anonymous-object member) imposes no explicit slot to
			// narrow into, but the inferred type follows the expression's own type — so a family whose Sql.Window
			// return type differs from the legacy ToValue() slot (e.g. NTile: int -> long) would silently change
			// the inferred variable's type. Only treat the rewrite as a no-op when the two types are identical.
			if (IsTypeInferredContext(legacyExpression))
			{
				var legacyType = model.GetTypeInfo(legacyExpression).Type;
				return legacyType is not null && SymbolEqualityComparer.Default.Equals(legacyType, newType);
			}

			var target = model.GetTypeInfo(legacyExpression).ConvertedType;
			if (target is null || target.TypeKind == TypeKind.Error)
				return true;

			var conversion = model.Compilation.ClassifyConversion(newType, target);
			return conversion.IsImplicit || conversion.IsIdentity;
		}

		static bool IsTypeInferredContext(ExpressionSyntax expression)
		{
			var parent = expression.Parent;
			while (parent is ParenthesizedExpressionSyntax parenthesized)
				parent = parenthesized.Parent;

			return parent switch
			{
				AnonymousObjectMemberDeclaratorSyntax => true,
				EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: true } } } => true,
				_ => false,
			};
		}

		// Resolve the modifier argument to its canonical enum-member name by its compile-time constant *value*, not
		// its source spelling: a direct `Sql.<Enum>.<Member>`, a const alias (`K.D`), or any other constant-valued
		// expression all map to the same member. Returns null when the argument is not a compile-time constant (the
		// caller then declines the fix) so a runtime-computed modifier is never silently mis-rewritten.
		static string? EnumMemberName(ExpressionSyntax expression, ITypeSymbol enumType, SemanticModel model)
		{
			var constant = model.GetConstantValue(expression);
			if (!constant.HasValue)
				return null;

			foreach (var member in enumType.GetMembers())
				if (member is IFieldSymbol { HasConstantValue: true } field && Equals(field.ConstantValue, constant.Value))
					return field.Name;

			return null;
		}

		static bool IsAnalyticFunctionsClass(INamedTypeSymbol? type)
			=> type is { Name: AnalyticFunctionsTypeName };

		static bool IsAnalyticNestedType(INamedTypeSymbol? type)
			=> type?.ContainingType is { Name: AnalyticFunctionsTypeName };
	}
}
