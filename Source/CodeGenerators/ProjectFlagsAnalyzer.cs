using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace CodeGenerators
{
	/// <summary>
	/// Guards the query-builder's <c>ProjectFlags</c> invariant: the enum mixes a <b>mutually-exclusive build
	/// purpose</b> with <b>independent modifiers</b>, but <c>ProjectFlagExtensions</c> exposes one flat
	/// <c>Is*()</c> predicate per bit, so an impossible pair reads as idiomatic. A conjunction that can never
	/// be true compiles, passes every test, and silently guards dead code.
	/// <list type="bullet">
	/// <item><c>LINQ2DB0004</c> - a flag test that can never be true where it stands.</item>
	/// <item><c>LINQ2DB0005</c> - a flag test that is always true where it stands.</item>
	/// <item><c>LINQ2DB0006</c> - the model could not be read, so the two rules above are disabled.</item>
	/// </list>
	/// The model is not hardcoded here: it is derived from <c>ExpressionBuildVisitor.GetProjectFlags</c>, the
	/// only producer of values reaching <c>IBuildContext.MakeExpression</c>. One unconditional
	/// <c>flags |= ProjectFlags.X</c> per switch section is a purpose, a conditional add inside a section is a
	/// modifier permitted with that purpose, and an add after the switch is a free modifier. Changing that
	/// method therefore changes the rule, and a shape this reader cannot parse is a build error rather than a
	/// silently wrong answer.
	/// </summary>
	/// <remarks>
	/// Scope is boolean <i>conditions</i> only, never flag composition. Two values in the tree combine two
	/// purpose bits and so lie outside the derived model: <c>ProjectFlags.SQL | ProjectFlags.Expression</c>
	/// passed to <c>ParseGenericConstructor</c>, which reads only <c>flags.IsSql()</c> so the extra bit is
	/// inert, and <c>ProjectFlags.SQL | ProjectFlags.Subquery</c> stored in an <c>ExprCacheKey</c>, where the
	/// flags are opaque key material participating in equality and hashing. Neither receiving method contains
	/// a conjunction of flag predicates, so neither can produce a false positive today. If one ever does, the
	/// escape hatch is a local <c>#pragma warning disable</c> naming this remark - not a wider model.
	/// </remarks>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class ProjectFlagsAnalyzer : DiagnosticAnalyzer
	{
		const string FlagsTypeName      = "LinqToDB.Internal.Linq.Builder.ProjectFlags";
		const string ExtensionsTypeName = "LinqToDB.Internal.Linq.Builder.ProjectFlagExtensions";
		const string VisitorTypeName    = "LinqToDB.Internal.Linq.Builder.ExpressionBuildVisitor";
		const string ModelMethodName    = "GetProjectFlags";

		static readonly DiagnosticDescriptor NeverTrue = new(
			id:                 "LINQ2DB0004",
			title:              "ProjectFlags test can never be true here",
			messageFormat:      "'{0}' can never be true here: {1}. The code it guards is unreachable.",
			category:           "Usage",
			defaultSeverity:    DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description:        "ProjectFlags carries a mutually-exclusive build purpose plus independent modifiers, and GetProjectFlags is the only producer of the values that reach MakeExpression. A test no reachable value satisfies - or one an earlier return has already excluded on every path - guards code that never runs.");

		static readonly DiagnosticDescriptor AlwaysTrue = new(
			id:                 "LINQ2DB0005",
			title:              "ProjectFlags test is always true here",
			messageFormat:      "'{0}' is always true here: {1}. The clause is redundant.",
			category:           "Usage",
			defaultSeverity:    DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description:        "A flag clause every reachable ProjectFlags value satisfies adds nothing to the condition, and leaving it in place makes the impossible pairing it appears to exclude look idiomatic.");

		static readonly DiagnosticDescriptor ModelUnreadable = new(
			id:                 "LINQ2DB0006",
			title:              "ProjectFlags model could not be read",
			messageFormat:      "The ProjectFlags model could not be read: {0}. LINQ2DB0004 and LINQ2DB0005 are disabled until this analyzer's reader is updated.",
			category:           "Usage",
			defaultSeverity:    DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description:        "The purpose/modifier classification is derived from ExpressionBuildVisitor.GetProjectFlags and the ProjectFlagExtensions predicate bodies rather than hardcoded, so a change to either that this analyzer cannot parse must fail loudly instead of producing wrong answers.");

		/// <inheritdoc/>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [NeverTrue, AlwaysTrue, ModelUnreadable];

		/// <inheritdoc/>
		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(static startContext =>
			{
				// The source assembly, not the compilation: the invariant is about linq2db's own source, and a
				// consumer that merely references linq2db must cost nothing.
				var assembly = startContext.Compilation.Assembly;

				var flagsType = assembly.GetTypeByMetadataName(FlagsTypeName);
				if (flagsType is not { TypeKind: TypeKind.Enum })
					return;

				var extensionsType = assembly.GetTypeByMetadataName(ExtensionsTypeName);
				var visitorType    = assembly.GetTypeByMetadataName(VisitorTypeName);

				if (extensionsType == null || visitorType == null)
					return;

				var model = new Lazy<FlagModel>(() => FlagModel.Read(flagsType, extensionsType, visitorType), isThreadSafe: true);

				// Drift is reported on the model's own declaration site, once, whether or not anything consumes it.
				// An unreadable predicate's location is inside ProjectFlagExtensions while a switch-shape failure's
				// is inside the visitor, and those are different files - so each failure is reported from the type
				// that declares it. Reporting all of them from one type leaves the other file's diagnostic pointing
				// outside the symbol it came from, which a host that filters per document is free to drop.
				startContext.RegisterSymbolAction(symbolContext =>
				{
					var isVisitor    = SymbolEqualityComparer.Default.Equals(symbolContext.Symbol, visitorType);
					var isExtensions = SymbolEqualityComparer.Default.Equals(symbolContext.Symbol, extensionsType);

					if (!isVisitor && !isExtensions)
						return;

					foreach (var (location, reason) in model.Value.Failures)
					{
						// Anything the extensions type does not own falls to the visitor, so a location inside
						// neither declaration is still reported exactly once rather than dropped.
						if (Declares(extensionsType, location) != isExtensions)
							continue;

						symbolContext.ReportDiagnostic(Diagnostic.Create(ModelUnreadable, location, reason));
					}
				}, SymbolKind.NamedType);

				startContext.RegisterOperationBlockStartAction(blockStart =>
				{
					if (!model.Value.IsUsable)
						return;

					// Cheap gate: Roslyn already visits every operation, so this costs nothing extra, and it keeps
					// the descendant walk and the control-flow graph off the thousands of bodies with no flag test.
					var seen = new bool[1];

					blockStart.RegisterOperationAction(operationContext =>
					{
						if (model.Value.TryReadAtom((IInvocationOperation)operationContext.Operation, out _, out _))
							seen[0] = true;
					}, OperationKind.Invocation);

					blockStart.RegisterOperationBlockEndAction(blockEnd =>
					{
						if (seen[0])
							Analyze(blockEnd, model.Value);
					});
				});
			});
		}

		/// <summary>
		/// Whether one of the type's own declarations spans the location, so drift found while reading that type
		/// is reported from it rather than from whichever type happened to trigger the read.
		/// </summary>
		static bool Declares(INamedTypeSymbol type, Location location)
		{
			foreach (var reference in type.DeclaringSyntaxReferences)
				if (reference.SyntaxTree == location.SourceTree && reference.Span.Contains(location.SourceSpan))
					return true;

			return false;
		}

		static void Analyze(OperationBlockAnalysisContext context, FlagModel model)
		{
			foreach (var block in context.OperationBlocks)
			{
				var tracked = CollectTrackedSymbols(block, model);
				if (tracked.Count == 0)
					continue;

				ControlFlowGraph graph;
				try
				{
					graph = context.GetControlFlowGraph(block);
				}
				catch (NotSupportedException)
				{
					// A body shape the flow-graph builder declines (field initializers, some expression bodies).
					continue;
				}

				foreach (var symbol in tracked)
					AnalyzeGraph(graph, symbol, model, context.ReportDiagnostic, context.CancellationToken);
			}
		}

		/// <summary>
		/// Candidate values to track: a parameter or local of the flags type that some recognised predicate reads.
		/// A field or property is never tracked - an intervening call could change it between two tests - and a
		/// value that is written anywhere in the body is dropped, because the flow analysis below assumes the
		/// value is fixed once produced.
		/// </summary>
		static List<ISymbol> CollectTrackedSymbols(IOperation block, FlagModel model)
		{
			var candidates = new List<ISymbol>();
			var written    = new List<ISymbol>();

			foreach (var operation in block.Descendants())
			{
				switch (operation)
				{
					case IInvocationOperation invocation
						when model.TryReadAtom(invocation, out var receiver, out _) && receiver != null:
					{
						if (!ContainsSymbol(candidates, receiver))
							candidates.Add(receiver);
						break;
					}

					case IAssignmentOperation assignment:
					{
						if (ReadSymbol(assignment.Target) is { } target)
							written.Add(target);
						break;
					}

					case IArgumentOperation { Parameter.RefKind: RefKind.Ref or RefKind.Out } argument:
					{
						if (ReadSymbol(argument.Value) is { } byRef)
							written.Add(byRef);
						break;
					}
				}
			}

			candidates.RemoveAll(c => ContainsSymbol(written, c));

			return candidates;
		}

		static bool ContainsSymbol(List<ISymbol> symbols, ISymbol symbol)
		{
			foreach (var candidate in symbols)
				if (SymbolEqualityComparer.Default.Equals(candidate, symbol))
					return true;

			return false;
		}

		/// <summary>
		/// Forward may-analysis: which of the model's reachable values can still hold at each block's entry.
		/// Join is set union, so a value excluded on one path but not another survives the merge - which is what
		/// makes an early return nested under an unrelated condition stop constraining the code after it.
		/// </summary>
		static void AnalyzeGraph(
			ControlFlowGraph      graph,
			ISymbol               tracked,
			FlagModel             model,
			Action<Diagnostic>    report,
			System.Threading.CancellationToken cancellationToken)
		{
			var size    = model.Domain.Length;
			var entries = new bool[graph.Blocks.Length][];

			for (var i = 0; i < entries.Length; i++)
				entries[i] = new bool[size];

			// The graph models no branch into an exception handler, so a catch / filter region's first block has
			// no predecessor. Seeding only the entry would leave it empty - over-constrained - and every merge
			// below the handler would inherit that. An all-possible seed can only ever produce fewer reports.
			var queue = new Queue<int>();

			foreach (var seed in graph.Blocks)
			{
				if (seed.Ordinal != 0 && seed.Predecessors.Length > 0)
					continue;

				var seedState = entries[seed.Ordinal];

				for (var i = 0; i < size; i++)
					seedState[i] = true;

				queue.Enqueue(seed.Ordinal);
			}

			while (queue.Count > 0)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var block = graph.Blocks[queue.Dequeue()];
				var state = entries[block.Ordinal];

				if (IsEmpty(state))
					continue;

				bool[] whenTrue, whenFalse;

				if (block.ConditionKind != ControlFlowConditionKind.None && block.BranchValue != null)
				{
					whenTrue  = Filter(state, block.BranchValue, tracked, model, expected: true);
					whenFalse = Filter(state, block.BranchValue, tracked, model, expected: false);
				}
				else
				{
					whenTrue = whenFalse = state;
				}

				// ConditionKind names the value that takes the *conditional* successor.
				var conditional = block.ConditionKind == ControlFlowConditionKind.WhenTrue ? whenTrue : whenFalse;
				var fallThrough = block.ConditionKind == ControlFlowConditionKind.WhenTrue ? whenFalse : whenTrue;

				Propagate(block.ConditionalSuccessor, conditional, entries, queue);
				Propagate(block.FallThroughSuccessor, fallThrough, entries, queue);
			}

			foreach (var block in graph.Blocks)
			{
				var state = entries[block.Ordinal];
				if (IsEmpty(state))
					continue;

				// A basic block has no internal branching, so its entry state holds for every operation in it.
				// Covering Operations - and a BranchValue that is a returned value rather than a condition - is
				// what reaches a test hoisted into a local, returned directly, or passed as an argument. Without
				// it the rule only ever sees a test written inline as the condition of an if.
				foreach (var operation in block.Operations)
					ReportBooleans(operation, state, tracked, model, report);

				if (block.BranchValue == null)
					continue;

				if (block.ConditionKind == ControlFlowConditionKind.None)
					ReportBooleans(block.BranchValue, state, tracked, model, report);
				else
					ReportConstants(block.BranchValue, state, tracked, model, report);
			}

			foreach (var localFunction in graph.LocalFunctions)
				AnalyzeGraph(graph.GetLocalFunctionControlFlowGraph(localFunction, cancellationToken), tracked, model, report, cancellationToken);

			foreach (var block in graph.Blocks)
			{
				foreach (var operation in block.Operations)
				{
					foreach (var descendant in operation.DescendantsAndSelf())
					{
						if (descendant is IFlowAnonymousFunctionOperation lambda)
							AnalyzeGraph(graph.GetAnonymousFunctionControlFlowGraph(lambda, cancellationToken), tracked, model, report, cancellationToken);
					}
				}
			}
		}

		static void Propagate(ControlFlowBranch? branch, bool[] state, bool[][] entries, Queue<int> queue)
		{
			if (branch?.Destination is not { } destination)
				return;

			var target  = entries[destination.Ordinal];
			var changed = false;

			for (var i = 0; i < state.Length; i++)
			{
				if (state[i] && !target[i])
				{
					target[i] = true;
					changed   = true;
				}
			}

			if (changed)
				queue.Enqueue(destination.Ordinal);
		}

		static bool[] Filter(bool[] state, IOperation condition, ISymbol tracked, FlagModel model, bool expected)
		{
			var result = new bool[state.Length];

			for (var i = 0; i < state.Length; i++)
			{
				if (!state[i])
					continue;

				// Unknown (null) keeps the value on both edges - an opaque sub-expression constrains nothing.
				var value = model.Evaluate(condition, tracked, model.Domain[i]);
				result[i] = value != !expected;
			}

			return result;
		}

		/// <summary>
		/// Descends to the outermost boolean-typed sub-expressions of a statement and judges each.
		/// <see cref="ReportConstants"/> walks through negations and boolean operators only, so handing it a
		/// statement would stop at the statement node; this finds the expressions worth handing it.
		/// </summary>
		static void ReportBooleans(IOperation node, bool[] state, ISymbol tracked, FlagModel model, Action<Diagnostic> report)
		{
			// An assignment carries the type of its right-hand side, so a bool one would otherwise be mistaken
			// for a judgeable expression - and it is exactly the shape that hoists a test into a local.
			if (node is IAssignmentOperation assignment)
			{
				ReportBooleans(assignment.Value, state, tracked, model, report);
				return;
			}

			if (node.Type?.SpecialType == SpecialType.System_Boolean)
			{
				ReportConstants(node, state, tracked, model, report);
				return;
			}

			foreach (var child in node.ChildOperations)
				ReportBooleans(child, state, tracked, model, report);
		}

		/// <summary>
		/// Reports the <b>outermost</b> fully-recognised sub-expression whose value is constant over the block's
		/// entry state, descending only through nodes that are not themselves constant. Outermost, not smallest:
		/// under <c>IsExpand()</c> the node <c>!flags.IsKeys()</c> is constantly true while its operand
		/// <c>flags.IsKeys()</c> is constantly false, and the redundant text a reader must delete is the negation.
		/// </summary>
		static void ReportConstants(IOperation node, bool[] state, ISymbol tracked, FlagModel model, Action<Diagnostic> report)
		{
			var inner = FlagModel.Unwrap(node) ?? node;

			// A node with no test of the tracked value is constant for uninteresting reasons (a literal, an
			// unrelated call) and must never be reported.
			if (model.ContainsAtom(inner, tracked))
			{
				bool? constant = null;
				var   uniform  = true;

				for (var i = 0; i < state.Length && uniform; i++)
				{
					if (!state[i])
						continue;

					var value = model.Evaluate(inner, tracked, model.Domain[i]);

					if (value == null)
						uniform = false;
					else if (constant == null)
						constant = value;
					else if (constant != value)
						uniform = false;
				}

				if (uniform && constant != null)
				{
					var value  = constant.Value;
					var reason = model.Explain(inner, tracked, value);
					var syntax = ClimbNegations(inner.Syntax, ref value);

					report(Diagnostic.Create(
						value ? AlwaysTrue : NeverTrue,
						syntax.GetLocation(),
						syntax.ToString(),
						reason));

					return;
				}
			}

			switch (inner)
			{
				case IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } unary:
					ReportConstants(unary.Operand, state, tracked, model, report);
					break;

				case IBinaryOperation
				{
					OperatorKind: BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr
						or BinaryOperatorKind.And or BinaryOperatorKind.Or
				} binary when binary.Type?.SpecialType == SpecialType.System_Boolean:
					ReportConstants(binary.LeftOperand,  state, tracked, model, report);
					ReportConstants(binary.RightOperand, state, tracked, model, report);
					break;
			}
		}

		/// <summary>
		/// Recovers the text the author actually wrote. The flow graph folds a leading <c>!</c> into the branch's
		/// <see cref="ControlFlowConditionKind"/>, so by the time a condition reaches this analyzer its operation
		/// tree carries the bare operand: for <c>flags.IsExpand() &amp;&amp; !flags.IsKeys()</c> the branch value is
		/// <c>flags.IsKeys()</c>. Reporting there would say "can never be true, the code it guards is unreachable"
		/// about a guard whose body is perfectly reachable - the redundant thing is the negation. So climb back out
		/// through the negations, flipping the answer at each one, and report on the outermost.
		/// </summary>
		static SyntaxNode ClimbNegations(SyntaxNode syntax, ref bool constant)
		{
			while (syntax.Parent != null)
			{
				if (syntax.Parent is ParenthesizedExpressionSyntax)
				{
					syntax = syntax.Parent;
					continue;
				}

				if (syntax.Parent is PrefixUnaryExpressionSyntax unary && unary.IsKind(SyntaxKind.LogicalNotExpression))
				{
					constant = !constant;
					syntax   = unary;
					continue;
				}

				break;
			}

			return syntax;
		}

		static bool IsEmpty(bool[] state)
		{
			foreach (var value in state)
				if (value)
					return false;

			return true;
		}

		static ISymbol? ReadSymbol(IOperation? operation) => FlagModel.Unwrap(operation) switch
		{
			IParameterReferenceOperation parameter => parameter.Parameter,
			ILocalReferenceOperation     local     => local.Local,
			_                                      => null,
		};

		#region Model

		/// <summary>
		/// A predicate over the flag bits: <c>AllOf</c> is <c>(value &amp; Mask) == Mask</c> (what
		/// <c>HasFlag</c> means), <c>AnyOf</c> is <c>(value &amp; Mask) != 0</c> (what the hand-rolled
		/// <c>IsSqlOrExpression</c> bitmask means).
		/// </summary>
		readonly struct Atom(int mask, bool allOf)
		{
			public int  Mask  { get; } = mask;
			public bool AllOf { get; } = allOf;

			public bool Holds(int value) => AllOf ? (value & Mask) == Mask : (value & Mask) != 0;
		}

		sealed class FlagModel
		{
			FlagModel(
				ImmutableArray<int>                        domain,
				Dictionary<string, Atom>                   predicates,
				INamedTypeSymbol                           extensionsType,
				INamedTypeSymbol                           flagsType,
				ImmutableArray<(Location, string)>         failures)
			{
				Domain          = domain;
				_predicates     = predicates;
				_extensionsType = extensionsType;
				_flagsType      = flagsType;
				Failures        = failures;
			}

			readonly Dictionary<string, Atom> _predicates;
			readonly INamedTypeSymbol         _extensionsType;
			readonly INamedTypeSymbol         _flagsType;

			/// <summary>Every flag value <c>GetProjectFlags</c> can produce.</summary>
			public ImmutableArray<int> Domain { get; }

			/// <summary>Why the model could not be read, if it could not be. Reported as LINQ2DB0006.</summary>
			public ImmutableArray<(Location Location, string Reason)> Failures { get; }

			public bool IsUsable => !Domain.IsDefaultOrEmpty;

			public static FlagModel Read(INamedTypeSymbol flagsType, INamedTypeSymbol extensionsType, INamedTypeSymbol visitorType)
			{
				var failures = ImmutableArray.CreateBuilder<(Location, string)>();

				var members = new Dictionary<string, int>(StringComparer.Ordinal);
				foreach (var member in flagsType.GetMembers())
				{
					if (member is IFieldSymbol { HasConstantValue: true, ConstantValue: int value } field)
						members[field.Name] = value;
				}

				var predicates = ReadPredicates(extensionsType, members, failures);
				var domain     = ReadDomain(visitorType, flagsType, members, failures);

				return new FlagModel(domain, predicates, extensionsType, flagsType, failures.ToImmutable());
			}

			static Dictionary<string, Atom> ReadPredicates(
				INamedTypeSymbol                            extensionsType,
				Dictionary<string, int>                     members,
				ImmutableArray<(Location, string)>.Builder  failures)
			{
				var predicates = new Dictionary<string, Atom>(StringComparer.Ordinal);

				foreach (var member in extensionsType.GetMembers())
				{
					if (member is not IMethodSymbol { MethodKind: MethodKind.Ordinary, IsStatic: true } method)
						continue;

					if (method.ReturnType.SpecialType != SpecialType.System_Boolean)
						continue;

					var body = SingleReturnExpression(method);

					if (body == null || !TryReadPredicateBody(body, members, out var atom))
					{
						failures.Add((
							method.Locations.Length > 0 ? method.Locations[0] : Location.None,
							$"the body of '{extensionsType.Name}.{method.Name}' is not a recognised single-expression flag predicate"));
						continue;
					}

					predicates[method.Name] = atom;
				}

				return predicates;
			}

			/// <summary>
			/// Derives the reachable value set from <c>GetProjectFlags</c>: in each switch section the
			/// unconditional <c>flags |= ProjectFlags.X</c> is that section's purpose and a conditional one is a
			/// modifier permitted with it, while a conditional add after the switch is a free modifier. Matching
			/// is structural, never positional, so reordering the statements is not a breaking change.
			/// </summary>
			static ImmutableArray<int> ReadDomain(
				INamedTypeSymbol                            visitorType,
				INamedTypeSymbol                            flagsType,
				Dictionary<string, int>                     members,
				ImmutableArray<(Location, string)>.Builder  failures)
			{
				var method = (IMethodSymbol?)null;
				foreach (var candidate in visitorType.GetMembers(ModelMethodName))
				{
					if (candidate is IMethodSymbol { Parameters.Length: 0 } m
						&& SymbolEqualityComparer.Default.Equals(m.ReturnType, flagsType))
					{
						method = m;
						break;
					}
				}

				var location = method?.Locations.Length > 0
					? method.Locations[0]
					: visitorType.Locations.Length > 0 ? visitorType.Locations[0] : Location.None;

				// List patterns are avoided throughout this file: System.Index is not in this project's minimal
				// Meziantou polyfill set, so `is [x, ..]` does not compile here.
				if (method == null
					|| method.DeclaringSyntaxReferences.Length == 0
					|| method.DeclaringSyntaxReferences[0].GetSyntax() is not MethodDeclarationSyntax { Body: { } methodBody })
				{
					failures.Add((location, $"'{visitorType.Name}.{ModelMethodName}' was not found as a parameterless method with a statement body"));
					return default;
				}

				var switchStatement = (SwitchStatementSyntax?)null;
				var freeModifiers   = 0;

				foreach (var statement in methodBody.Statements)
				{
					switch (statement)
					{
						case SwitchStatementSyntax found when switchStatement == null:
							switchStatement = found;
							break;

						case IfStatementSyntax { Else: null } conditional
							when TryReadFlagAdd(conditional.Statement, members, out var modifier):
							freeModifiers |= modifier;
							break;

						case LocalDeclarationStatementSyntax:
						case ReturnStatementSyntax:
							break;

						default:
							failures.Add((location, $"'{ModelMethodName}' contains a statement this reader does not recognise: '{Summarize(statement)}'"));
							return default;
					}
				}

				if (switchStatement == null)
				{
					failures.Add((location, $"'{ModelMethodName}' no longer switches on a single build purpose"));
					return default;
				}

				var purposes = new List<(int Purpose, int Optional)>();

				foreach (var section in switchStatement.Sections)
				{
					var purpose  = 0;
					var optional = 0;
					var count    = 0;

					foreach (var statement in section.Statements)
					{
						switch (statement)
						{
							case IfStatementSyntax { Else: null } conditional
								when TryReadFlagAdd(conditional.Statement, members, out var modifier):
								optional |= modifier;
								break;

							case BreakStatementSyntax:
							case ThrowStatementSyntax:
								break;

							default:
							{
								if (TryReadFlagAdd(statement, members, out var added))
								{
									purpose |= added;
									count++;
								}
								else
								{
									failures.Add((location, $"a '{ModelMethodName}' switch section contains a statement this reader does not recognise: '{Summarize(statement)}'"));
									return default;
								}

								break;
							}
						}
					}

					// The default section throws and adds nothing.
					if (count == 0 && optional == 0)
						continue;

					if (count != 1)
					{
						failures.Add((location, $"a '{ModelMethodName}' switch section adds {count.ToString(System.Globalization.CultureInfo.InvariantCulture)} unconditional flags where exactly one purpose was expected"));
						return default;
					}

					purposes.Add((purpose, optional));
				}

				if (purposes.Count == 0)
				{
					failures.Add((location, $"'{ModelMethodName}' produced no build purposes"));
					return default;
				}

				var classified = freeModifiers;
				foreach (var (purpose, optional) in purposes)
					classified |= purpose | optional;

				foreach (var member in members)
				{
					if (member.Value != 0 && (classified & member.Value) != member.Value)
					{
						failures.Add((location, $"'{member.Key}' is never produced by '{ModelMethodName}', so this analyzer cannot classify it as a purpose or a modifier"));
						return default;
					}
				}

				var freeBits = Bits(freeModifiers);
				var domain   = ImmutableArray.CreateBuilder<int>();

				foreach (var (purpose, optional) in purposes)
				{
					foreach (var optionalSubset in Subsets(Bits(optional)))
					{
						foreach (var freeSubset in Subsets(freeBits))
							domain.Add(purpose | optionalSubset | freeSubset);
					}
				}

				return domain.ToImmutable();
			}

			static string Summarize(SyntaxNode node)
			{
				var text = node.ToString().Replace("\r", string.Empty).Replace("\n", " ");
				return text.Length <= 60 ? text : text.Substring(0, 60) + "...";
			}

			static List<int> Bits(int mask)
			{
				var bits = new List<int>();

				for (var bit = 1; bit != 0; bit <<= 1)
				{
					if ((mask & bit) != 0)
						bits.Add(bit);
				}

				return bits;
			}

			static List<int> Subsets(List<int> bits)
			{
				var subsets = new List<int> { 0 };

				foreach (var bit in bits)
				{
					var count = subsets.Count;
					for (var i = 0; i < count; i++)
						subsets.Add(subsets[i] | bit);
				}

				return subsets;
			}

			static ExpressionSyntax? SingleReturnExpression(IMethodSymbol method)
			{
				if (method.DeclaringSyntaxReferences.Length == 0)
					return null;

				if (method.DeclaringSyntaxReferences[0].GetSyntax() is not MethodDeclarationSyntax declaration)
					return null;

				if (declaration.ExpressionBody?.Expression is { } expression)
					return expression;

				if (declaration.Body is { } body && body.Statements.Count == 1 && body.Statements[0] is ReturnStatementSyntax returned)
					return returned.Expression;

				return null;
			}

			static bool TryReadFlagAdd(SyntaxNode statement, Dictionary<string, int> members, out int mask)
			{
				mask = 0;

				if (statement is BlockSyntax { Statements.Count: 1 } block)
					statement = block.Statements[0];

				return statement is ExpressionStatementSyntax
					{
						Expression: AssignmentExpressionSyntax
						{
							RawKind: (int)SyntaxKind.OrAssignmentExpression,
							Right  : { } right,
						},
					}
					&& TryReadMask(right, members, out mask);
			}

			static bool TryReadPredicateBody(ExpressionSyntax body, Dictionary<string, int> members, out Atom atom)
			{
				atom = default;

				switch (Unwrap(body))
				{
					// flags.HasFlag(ProjectFlags.X)
					case InvocationExpressionSyntax
					{
						Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "HasFlag" },
						ArgumentList.Arguments.Count: 1,
					} call
						when TryReadMask(call.ArgumentList.Arguments[0].Expression, members, out var flag):
					{
						atom = new Atom(flag, allOf: true);
						return true;
					}

					// (flags & (ProjectFlags.A | ProjectFlags.B)) != 0
					case BinaryExpressionSyntax
					{
						RawKind: (int)SyntaxKind.NotEqualsExpression,
						Left   : { } left,
						Right  : { } right,
					}
						when IsZero(right)
							&& Unwrap(left) is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.BitwiseAndExpression, Right: { } maskExpression }
							&& TryReadMask(maskExpression, members, out var mask):
					{
						atom = new Atom(mask, allOf: false);
						return true;
					}
				}

				return false;
			}

			static bool IsZero(ExpressionSyntax expression)
				=> Unwrap(expression) is LiteralExpressionSyntax { Token.ValueText: "0" };

			static bool TryReadMask(ExpressionSyntax expression, Dictionary<string, int> members, out int mask)
			{
				switch (Unwrap(expression))
				{
					case MemberAccessExpressionSyntax { Name.Identifier.ValueText: { } name }
						when members.TryGetValue(name, out var value):
					{
						mask = value;
						return true;
					}

					case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.BitwiseOrExpression, Left: { } left, Right: { } right }
						when TryReadMask(left, members, out var l) && TryReadMask(right, members, out var r):
					{
						mask = l | r;
						return true;
					}
				}

				mask = 0;
				return false;
			}

			static ExpressionSyntax Unwrap(ExpressionSyntax expression)
			{
				while (expression is ParenthesizedExpressionSyntax parenthesized)
					expression = parenthesized.Expression;

				return expression;
			}

			public static IOperation? Unwrap(IOperation? operation)
			{
				while (true)
				{
					switch (operation)
					{
						case IParenthesizedOperation parenthesized: operation = parenthesized.Operand; break;
						case IConversionOperation     conversion   : operation = conversion.Operand;    break;
						default                                    : return operation;
					}
				}
			}

			/// <summary>
			/// Recognises a flag test and the value it reads. Both forms in the tree are covered - a
			/// <c>ProjectFlagExtensions</c> predicate and a direct <c>HasFlag</c> - and both are matched on the
			/// resolved symbol, never on the receiver's name: a local called <c>flags</c> holding
			/// <c>SqlProviderFlags</c> exposes members of the same shape.
			/// </summary>
			public bool TryReadAtom(IInvocationOperation invocation, out ISymbol? receiver, out Atom atom)
			{
				receiver = null;
				atom     = default;

				var method = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;

				if (SymbolEqualityComparer.Default.Equals(method.ContainingType, _extensionsType))
				{
					if (!_predicates.TryGetValue(method.Name, out atom))
						return false;

					var argument = invocation.Instance
						?? (invocation.Arguments.Length > 0 ? invocation.Arguments[0].Value : null);

					receiver = ReadSymbol(argument);
					return receiver != null;
				}

				if (string.Equals(method.Name, "HasFlag", StringComparison.Ordinal)
					&& method.ContainingType?.SpecialType == SpecialType.System_Enum
					&& SymbolEqualityComparer.Default.Equals(invocation.Instance?.Type, _flagsType)
					&& invocation.Arguments.Length == 1
					&& Unwrap(invocation.Arguments[0].Value)?.ConstantValue is { HasValue: true, Value: int flag })
				{
					atom     = new Atom(flag, allOf: true);
					receiver = ReadSymbol(invocation.Instance);
					return receiver != null;
				}

				return false;
			}

			public bool ContainsAtom(IOperation? node, ISymbol tracked)
			{
				if (node == null)
					return false;

				foreach (var operation in node.DescendantsAndSelf())
				{
					if (operation is IInvocationOperation invocation
						&& TryReadAtom(invocation, out var receiver, out _)
						&& SymbolEqualityComparer.Default.Equals(receiver, tracked))
					{
						return true;
					}
				}

				return false;
			}

			/// <summary>
			/// Three-valued evaluation of a condition for one candidate flag value. <c>null</c> means "depends on
			/// something this analyzer cannot see", which keeps the value alive on both edges of a branch.
			/// </summary>
			public bool? Evaluate(IOperation? node, ISymbol tracked, int value)
			{
				switch (Unwrap(node))
				{
					case ILiteralOperation { ConstantValue: { HasValue: true, Value: bool literal } }:
						return literal;

					case IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } unary:
						return Evaluate(unary.Operand, tracked, value) is { } operand ? !operand : null;

					case IBinaryOperation
					{
						OperatorKind: BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.And,
					} conjunction when conjunction.Type?.SpecialType == SpecialType.System_Boolean:
					{
						var left  = Evaluate(conjunction.LeftOperand,  tracked, value);
						var right = Evaluate(conjunction.RightOperand, tracked, value);

						if (left == false || right == false)
							return false;

						return left == true && right == true ? true : null;
					}

					case IBinaryOperation
					{
						OperatorKind: BinaryOperatorKind.ConditionalOr or BinaryOperatorKind.Or,
					} disjunction when disjunction.Type?.SpecialType == SpecialType.System_Boolean:
					{
						var left  = Evaluate(disjunction.LeftOperand,  tracked, value);
						var right = Evaluate(disjunction.RightOperand, tracked, value);

						if (left == true || right == true)
							return true;

						return left == false && right == false ? false : null;
					}

					case IInvocationOperation invocation
						when TryReadAtom(invocation, out var receiver, out var atom)
							&& SymbolEqualityComparer.Default.Equals(receiver, tracked):
						return atom.Holds(value);

					default:
						return null;
				}
			}

			/// <summary>
			/// Distinguishes the two reasons a test can be constant, because they call for different fixes: an
			/// impossible flag combination is a modelling mistake, while a value excluded on every path is dead
			/// code left behind by an earlier return.
			/// </summary>
			public string Explain(IOperation node, ISymbol tracked, bool constant)
			{
				var uniformOverWholeDomain = true;

				foreach (var value in Domain)
				{
					if (Evaluate(node, tracked, value) != constant)
					{
						uniformOverWholeDomain = false;
						break;
					}
				}

				return uniformOverWholeDomain
					? "no ProjectFlags value GetProjectFlags can produce gives a different answer"
					: "every value that can still reach this point is already excluded by an earlier test on the same path";
			}
		}

		#endregion
	}
}
