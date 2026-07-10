using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LinqToDB.Analyzers.CodeFixes
{
	/// <summary>
	/// Rewrites a legacy <c>Sql.Ext.&lt;Fn&gt;(...)...Over()...ToValue()</c> chain into the equivalent
	/// <c>Sql.Window.&lt;Fn&gt;(&lt;args&gt;, f =&gt; f....)</c> call. Mirrors the internal
	/// <c>LegacyMemberConverterBase.TryConvertAnalyticFunction</c> mapping, but at the syntax level so the fix
	/// can preserve the user's argument expressions (and their trivia) verbatim. Returns <c>null</c> when the
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

		// Functions whose Sql.Window overloads return double? (vs the legacy chain's TR slot type).
		static readonly HashSet<string> NullableDoubleReturning = new(StringComparer.Ordinal)
		{
			"Median", "RatioToReport", "PercentileCont", "PercentileDisc",
			"StdDev", "StdDevPop", "StdDevSamp", "Variance", "VarPop", "VarSamp",
			"CovarPop", "CovarSamp", "Corr",
			"RegrSlope", "RegrIntercept", "RegrCount", "RegrR2", "RegrAvgX", "RegrAvgY", "RegrSXX", "RegrSYY", "RegrSXY",
		};

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

		public static ExpressionSyntax? TryRewrite(InvocationExpressionSyntax toValueInvocation, SemanticModel model, CancellationToken cancellationToken)
		{
			if (toValueInvocation.Expression is not MemberAccessExpressionSyntax toValueAccess)
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
								toValueInvocation, model, inv, ma, method, sawOver,
								partitionArgs, orderClauses, keepSeen, isKeepFirst, keepOrderClauses, withinGroupOrderClauses,
								frameSeen, frameIsRange, frameStart, frameStartValue, frameEnd, frameEndValue);
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
			IMethodSymbol rootMethod,
			bool sawOver,
			ArgumentListSyntax? partitionArgs,
			List<OrderClause> orderClauses,
			bool keepSeen, bool isKeepFirst, List<OrderClause>? keepOrderClauses, List<OrderClause>? withinGroupOrderClauses,
			bool frameSeen, bool frameIsRange, string? frameStart, ArgumentSyntax? frameStartValue, string? frameEnd, ArgumentSyntax? frameEndValue)
		{
			var functionName = rootAccess.Name.Identifier.Text;

			// Plain aggregate without OVER has no Sql.Window equivalent; unknown/irregular functions bail.
			if (!sawOver || !ConvertibleFunctions.Contains(functionName))
				return null;

			// The Sql.Window statistical / median / ratio / percentile functions return double?, which the legacy
			// ToValue<TR>() slot may not accept (e.g. an explicit `double`/`int?` local or return). Only offer the
			// fix when double? fits the target — or the context is type-inferred (anonymous member / var) and re-infers.
			if (NullableDoubleReturning.Contains(functionName) && !NullableDoubleFitsTarget(toValueInvocation, model))
				return null;

			// Split the root call's arguments into positional value args and the special modifier args
			// (AggregateModifier / Nulls / From / NullsPosition) that become builder calls.
			var valueArgs   = new List<ArgumentSyntax>();
			string? distinct = null, nullTreatment = null, fromPosition = null;

			var rootArgs   = rootInvocation.ArgumentList.Arguments;
			var parameters = rootMethod.Parameters; // reduced extension method: excludes the 'this' receiver

			for (var i = 0; i < rootArgs.Count; i++)
			{
				var paramType = i < parameters.Length ? parameters[i].Type.Name : null;

				switch (paramType)
				{
					case "AggregateModifier":
						var mod = EnumMemberName(rootArgs[i].Expression);
						if (mod is null) return null;
						if (string.Equals(mod, "Distinct", StringComparison.Ordinal)) distinct = "Distinct";
						break;
					case "Nulls":
						var nulls = EnumMemberName(rootArgs[i].Expression);
						if (nulls is null) return null;
						if (string.Equals(nulls, "Ignore",  StringComparison.Ordinal)) nullTreatment = "IgnoreNulls";
						if (string.Equals(nulls, "Respect", StringComparison.Ordinal)) nullTreatment = "RespectNulls";
						break;
					case "From":
						var from = EnumMemberName(rootArgs[i].Expression);
						if (from is null) return null;
						if (string.Equals(from, "First", StringComparison.Ordinal)) fromPosition = "FromFirst";
						if (string.Equals(from, "Last",  StringComparison.Ordinal)) fromPosition = "FromLast";
						break;
					case "NullsPosition":
						break;
					default:
						valueArgs.Add(rootArgs[i]);
						break;
				}
			}

			// Assemble the ordered builder steps for whichever function shape this is.
			var steps = new List<Step>();

			if (keepSeen)
			{
				// KEEP: f.KeepFirst()/KeepLast().OrderBy(...)[.ThenBy...].PartitionBy(...)   (order is mandatory)
				if (!KeepableFunctions.Contains(functionName) || keepOrderClauses is not { Count: > 0 })
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
			var sqlQualifier = ((MemberAccessExpressionSyntax)rootAccess.Expression).Expression;
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

		// True when double? is acceptable at the expression's position: the target type takes double? implicitly,
		// the target is unknown, or the context imposes no target type (anonymous member / var — it re-infers).
		static bool NullableDoubleFitsTarget(ExpressionSyntax expression, SemanticModel model)
		{
			if (IsTypeInferredContext(expression))
				return true;

			var target = model.GetTypeInfo(expression).ConvertedType;
			if (target is null)
				return true;

			var doubleType     = model.Compilation.GetSpecialType(SpecialType.System_Double);
			var nullableDouble = model.Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(doubleType);

			var conversion = model.Compilation.ClassifyConversion(nullableDouble, target);
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

		static string? EnumMemberName(ExpressionSyntax expression)
			=> expression is MemberAccessExpressionSyntax ma ? ma.Name.Identifier.Text : null;

		static bool IsAnalyticFunctionsClass(INamedTypeSymbol? type)
			=> type is { Name: AnalyticFunctionsTypeName };

		static bool IsAnalyticNestedType(INamedTypeSymbol? type)
			=> type?.ContainingType is { Name: AnalyticFunctionsTypeName };
	}
}
