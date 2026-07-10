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

		// Functions this rewriter converts. Irregular OVER shapes (Median/RatioToReport partition-only, windowed
		// PercentileCont/Disc via WITHIN GROUP, KEEP) are intentionally excluded for now — they bail to no-fix.
		static readonly HashSet<string> ConvertibleFunctions = new(StringComparer.Ordinal)
		{
			"RowNumber", "Rank", "DenseRank", "PercentRank", "CumeDist", "NTile",
			"Sum", "Average", "Min", "Max", "Count", "LongCount",
			"StdDev", "StdDevPop", "StdDevSamp", "Variance", "VarPop", "VarSamp",
			"CovarPop", "CovarSamp", "Corr",
			"RegrSlope", "RegrIntercept", "RegrCount", "RegrR2", "RegrAvgX", "RegrAvgY", "RegrSXX", "RegrSYY", "RegrSXY",
			"Lead", "Lag",
			"FirstValue", "LastValue", "NthValue",
		};

		public static ExpressionSyntax? TryRewrite(InvocationExpressionSyntax toValueInvocation, SemanticModel model, CancellationToken cancellationToken)
		{
			if (toValueInvocation.Expression is not MemberAccessExpressionSyntax toValueAccess)
				return null;

			// Collected, in ToValue -> root order (reversed to natural order before building).
			var orderSegments = new List<(bool IsThen, bool IsDesc, ArgumentListSyntax Args)>();
			ArgumentListSyntax? partitionArgs = null;

			var    sawOver          = false;
			var    frameSeen        = false;
			var    frameIsRange     = false;
			string? frameStart      = null;
			string? frameEnd        = null;
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

						// Root analytic function reached (declared directly on the AnalyticFunctions class).
						if (IsAnalyticFunctionsClass(method.ContainingType))
						{
							return BuildFromRoot(
								toValueInvocation, inv, ma, method,
								sawOver, partitionArgs, orderSegments,
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
								orderSegments.Add((name.StartsWith("Then", StringComparison.Ordinal), name.EndsWith("Desc", StringComparison.Ordinal), inv.ArgumentList));
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
								// KeepFirst/KeepLast, Filter, ListAgg, and anything unrecognized: no mechanical conversion.
								return null;
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
								return null; // ordered-set (percentile) — not handled here
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

		// Maps a legacy boundary onto the new IBoundaryPart member, using the declaring interface to tell start from end.
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
			InvocationExpressionSyntax rootInvocation,
			MemberAccessExpressionSyntax rootAccess,
			IMethodSymbol rootMethod,
			bool sawOver,
			ArgumentListSyntax? partitionArgs,
			List<(bool IsThen, bool IsDesc, ArgumentListSyntax Args)> orderSegments,
			bool frameSeen, bool frameIsRange, string? frameStart, ArgumentSyntax? frameStartValue, string? frameEnd, ArgumentSyntax? frameEndValue)
		{
			var functionName = rootAccess.Name.Identifier.Text;

			// Plain aggregate without OVER has no Sql.Window equivalent; unknown/irregular functions bail.
			if (!sawOver || !ConvertibleFunctions.Contains(functionName))
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
						if (mod is null) return null;                 // non-literal modifier — can't map safely
						if (string.Equals(mod, "Distinct", StringComparison.Ordinal)) distinct = "Distinct";
						// All / None are the SQL default -> dropped
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
						break; // not used at the function level
					default:
						valueArgs.Add(rootArgs[i]);
						break;
				}
			}

			// A frame is only valid on aggregate/value/statistical functions; otherwise bail to the legacy pipeline.
			if (frameSeen && (frameStart is null || !FrameableFunctions.Contains(functionName)))
				return null;

			orderSegments.Reverse();

			// --- Build the builder lambda body with placeholder args, format the scaffold, then splice originals. ---
			var placeholders = new Dictionary<string, SyntaxNode>(StringComparer.Ordinal);
			var counter      = 0;

			string NextPlaceholderId() => "__l2db_" + counter++.ToString(CultureInfo.InvariantCulture);

			ArgumentListSyntax Placeholderize(ArgumentListSyntax original)
			{
				var args = original.Arguments.Select(a =>
				{
					var id = NextPlaceholderId();
					placeholders[id] = a.Expression;
					return a.WithExpression(SyntaxFactory.IdentifierName(id));
				});
				return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(args));
			}

			ArgumentSyntax PlaceholderizeArg(ArgumentSyntax original)
			{
				var id = NextPlaceholderId();
				placeholders[id] = original.Expression;
				return original.WithExpression(SyntaxFactory.IdentifierName(id));
			}

			ExpressionSyntax body = SyntaxFactory.IdentifierName("f");

			ExpressionSyntax Call(string method, ArgumentListSyntax args)
				=> SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, body, SyntaxFactory.IdentifierName(method)), args);

			ExpressionSyntax Prop(string name)
				=> SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, body, SyntaxFactory.IdentifierName(name));

			if (distinct      is not null) body = Call(distinct,      SyntaxFactory.ArgumentList());
			if (fromPosition  is not null) body = Call(fromPosition,  SyntaxFactory.ArgumentList());
			if (nullTreatment is not null) body = Call(nullTreatment, SyntaxFactory.ArgumentList());

			if (partitionArgs is not null)
				body = Call("PartitionBy", Placeholderize(partitionArgs));

			foreach (var (_, isDesc, args) in orderSegments)
			{
				// First ordering uses OrderBy/OrderByDesc; subsequent ones ThenBy/ThenByDesc.
				var isFirst = ReferenceEquals(args, orderSegments[0].Args);
				var method  = (isFirst ? "OrderBy" : "ThenBy") + (isDesc ? "Desc" : "");
				body = Call(method, Placeholderize(args));
			}

			if (frameSeen)
			{
				body = Prop(frameIsRange ? "RangeBetween" : "RowsBetween");
				body = frameStartValue is not null
					? Call(frameStart!, SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(PlaceholderizeArg(frameStartValue))))
					: Prop(frameStart!);
				body = Prop("And");
				var endMember = frameEnd ?? "CurrentRow"; // single-boundary legacy form -> ... And CurrentRow
				body = frameEndValue is not null
					? Call(endMember, SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(PlaceholderizeArg(frameEndValue))))
					: Prop(endMember);
			}

			// Sql.Window.<Fn>(<valueArgs>, f => <body>) — reuse the user's Sql qualifier from `<Sql>.Ext`.
			var sqlQualifier = ((MemberAccessExpressionSyntax)rootAccess.Expression).Expression; // the `Sql` (or `LinqToDB.Sql`) part
			var windowAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
				SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, sqlQualifier.WithoutTrivia(), SyntaxFactory.IdentifierName("Window")),
				SyntaxFactory.IdentifierName(functionName));

			var lambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier("f")), body);

			var callArgs = new List<ArgumentSyntax>();
			foreach (var a in valueArgs)
				callArgs.Add(PlaceholderizeArg(a));
			callArgs.Add(SyntaxFactory.Argument(lambda));

			ExpressionSyntax newExpression = SyntaxFactory.InvocationExpression(windowAccess, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(callArgs)));

			// Format the freshly-built scaffold, then splice the original argument subtrees back in (restoring their
			// trivia — e.g. comments on arguments).
			newExpression = newExpression.NormalizeWhitespace();
			newExpression = newExpression.ReplaceNodes(
				newExpression.DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => placeholders.ContainsKey(n.Identifier.Text)),
				(original, _) => placeholders[original.Identifier.Text]);

			// Preserve the whole expression's outer (leading/trailing) trivia, and salvage any comments that lived
			// on the original chain's scaffolding (between calls) so the rewrite loses no comment. Comments already
			// inside a reused argument subtree are kept in place, so they're excluded here to avoid duplication.
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

		// The simple member name of an enum literal like `Sql.AggregateModifier.Distinct` (-> "Distinct"); null if not a plain member access.
		static string? EnumMemberName(ExpressionSyntax expression)
			=> expression is MemberAccessExpressionSyntax ma ? ma.Name.Identifier.Text : null;

		static bool IsAnalyticFunctionsClass(INamedTypeSymbol? type)
			=> type is { Name: AnalyticFunctionsTypeName };

		static bool IsAnalyticNestedType(INamedTypeSymbol? type)
			=> type?.ContainingType is { Name: AnalyticFunctionsTypeName };
	}
}
