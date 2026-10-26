using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CodeGenerators
{
	/// <summary>
	/// Guards the query builders' schema invariant: a build step that has a source in reach must read the source's
	/// <c>MappingSchema</c>, not the data context's. The two differ when a table handle was created before
	/// <c>UseMappingSchema</c> / <c>AddMappingSchema</c> and the query is built inside the scope: the handle carries a
	/// snapshot (<c>SqlQueryRootExpression</c>), while <c>ExpressionBuilder.MappingSchema</c> reads the context live.
	/// A step that holds the source but reads the live schema resolves one facet of an entity through each.
	/// <para>
	/// A <i>live read</i> is a read of a property in <c>LinqToDB.Internal.Linq.Builder</c> whose getter returns
	/// <c>&lt;IDataContext member&gt;.MappingSchema</c> (derived, not listed), or of <c>IDataContext.MappingSchema</c>
	/// through an <c>ExpressionBuilder</c>. It is reported when one of these holds:
	/// </para>
	/// <list type="bullet">
	/// <item>a context's own <c>MappingSchema</c> getter, or the constructor assignment of its backing field or of the
	/// property itself, reads it while the type holds a source context other than <c>Parent</c>, or while such a
	/// constructor has a source carrier among its parameters;</item>
	/// <item>a source carrier is in scope - a <c>MappingSchema</c>, an <c>EntityDescriptor</c> / <c>ColumnDescriptor</c> /
	/// <c>AssociationDescriptor</c>, an <c>IBuildContext</c> or a <c>ContextRefExpression</c>;</item>
	/// <item>it is read through a context's <c>Builder</c>;</item>
	/// <item>it is inside <c>MethodCallBuilder.BuildMethodCall</c>, whose source is <c>methodCall.Arguments[0]</c> even
	/// before it is built - except as an argument to a method-attribute lookup.</item>
	/// </list>
	/// The one exemption is the documented fallback <c>&lt;source&gt;.MappingSchema ?? builder.MappingSchema</c>.
	/// </summary>
	/// <remarks>
	/// Not visible to the rule: a source reachable only through an unbuilt <c>Expression</c> outside
	/// <c>BuildMethodCall</c>, an alias written as an auto-property assigned in a constructor, and a root context's own
	/// <c>MappingSchema</c> (<c>=&gt; Builder.MappingSchema</c>), the live schema one hop further. Method-attribute
	/// lookups stay on the live schema on purpose: no source exists before the sequence is built, and whether
	/// extensions should follow the snapshot is undecided (#5951).
	/// </remarks>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class BuildContextMappingSchemaAnalyzer : DiagnosticAnalyzer
	{
		const string BuilderNamespace       = "LinqToDB.Internal.Linq.Builder";
		const string ExpressionBuilderName  = "LinqToDB.Internal.Linq.Builder.ExpressionBuilder";
		const string BuildContextName       = "LinqToDB.Internal.Linq.Builder.IBuildContext";
		const string MethodCallBuilderName  = "LinqToDB.Internal.Linq.Builder.MethodCallBuilder";
		const string ContextRefName         = "LinqToDB.Internal.Expressions.ContextRefExpression";
		const string MappingSchemaName      = "LinqToDB.Mapping.MappingSchema";
		const string EntityDescriptorName   = "LinqToDB.Mapping.EntityDescriptor";
		const string ColumnDescriptorName   = "LinqToDB.Mapping.ColumnDescriptor";
		const string AssocDescriptorName    = "LinqToDB.Mapping.AssociationDescriptor";
		const string DataContextName        = "LinqToDB.IDataContext";
		const string SchemaProperty         = "MappingSchema";

		// Method-attribute lookups that stay on the live schema inside BuildMethodCall: (method, declaring type).
		static readonly (string Method, string Type)[] LiveLookups =
		[
			("GetExtensionAttributes",    "ExtensionAttribute"),
			("GetExtensionAttributes",    "QueryExtensionAttribute"),
			("GetTableFunctionAttribute", "MappingExtensions"),
			("SkipMethodChain",           "InternalExtensions"),
		];

		static readonly DiagnosticDescriptor Rule = new(
			id:                 "LINQ2DB0007",
			title:              "Read the source's MappingSchema, not the data context's",
			messageFormat:      "{0}",
			category:           "Usage",
			defaultSeverity:    DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description:        "A table handle carries the schema it was created under; the data context's schema can be replaced later by UseMappingSchema / AddMappingSchema. A build step that has the source in reach must resolve mapping through it, otherwise one query mixes two schemas. Fall back to the data context's schema only where no source exists, as `source?.MappingSchema ?? builder.MappingSchema`.");

		/// <inheritdoc/>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

		/// <inheritdoc/>
		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(static startContext =>
			{
				// The source assembly, not the compilation: the invariant is about linq2db's own builders, and a
				// consumer that merely references linq2db must cost nothing.
				var assembly       = startContext.Compilation.Assembly;
				var expressionType = assembly.GetTypeByMetadataName(ExpressionBuilderName);

				if (expressionType == null)
					return;

				var model = Model.Create(assembly, expressionType, out var drift);

				if (model == null)
				{
					var location = expressionType.Locations.Length > 0 ? expressionType.Locations[0] : Location.None;

					startContext.RegisterSymbolAction(symbolContext =>
					{
						if (SymbolEqualityComparer.Default.Equals(symbolContext.Symbol, expressionType))
							symbolContext.ReportDiagnostic(Diagnostic.Create(Rule, location,
								$"The LINQ2DB0007 model could not be read: {drift}. The rule is disabled for the whole compilation until the analyzer is updated."));
					}, SymbolKind.NamedType);

					return;
				}

				startContext.RegisterOperationAction(opContext => Analyze(opContext, model), OperationKind.PropertyReference);
			});
		}

		static void Analyze(OperationAnalysisContext context, Model model)
		{
			var op = (IPropertyReferenceOperation)context.Operation;

			if (op.Property.Name != SchemaProperty)
				return;

			if (op.Parent is IAssignmentOperation assignment && ReferenceEquals(assignment.Target, op))
				return;

			var member = OuterMember(context.ContainingSymbol);
			if (member == null || !InBuilderNamespace(member.ContainingType))
				return;

			if (!model.IsLiveRead(op))
				return;

			var read = Climb(op);

			if (IsDocumentedFallback(read, model))
				return;

			var enclosingType = member.ContainingType;

			// Definition site: the context decides its own schema here, so the type's members can make it wrong, and a
			// constructor's parameters too. Its own members are no carriers there - the read is what defines them.
			var definitionSite = IsDefinitionSite(read, member, enclosingType, model);
			if (definitionSite)
			{
				var source = model.FindSourceMember(enclosingType);
				if (source != null)
				{
					Report(context, op, $"'{enclosingType.Name}' holds the source context '{source.Name}': return its MappingSchema instead of the data context's");
					return;
				}

				if (member.MethodKind != MethodKind.Constructor)
					return;
			}

			var carrier = FindCarrier(op, read, member, enclosingType, model, membersAllowed: !definitionSite);
			if (carrier != null)
			{
				Report(context, op, $"'{carrier.Value.Name}' is in scope: read '{carrier.Value.Suggestion}' instead of the data context's MappingSchema");
				return;
			}

			if (definitionSite)
				return;

			var throughContext = FindContextBuilder(op, model);
			if (throughContext != null)
			{
				Report(context, op, $"Read through the context '{throughContext}': use '{throughContext}.MappingSchema' instead of its Builder's data-context schema");
				return;
			}

			if (model.IsBuildMethodCall(member) && !IsLiveLookupArgument(read))
				Report(context, op, "Inside BuildMethodCall the source is methodCall.Arguments[0]: read its snapshot via 'ExpressionBuilder.TryGetRootMappingSchema(...) ?? builder.MappingSchema' instead of the data context's MappingSchema");
		}

		static void Report(OperationAnalysisContext context, IOperation op, string message)
			=> context.ReportDiagnostic(Diagnostic.Create(Rule, op.Syntax.GetLocation(), message));

		#region Shape helpers

		static IOperation Climb(IOperation op)
		{
			var current = op;

			while (current.Parent is IConversionOperation or IParenthesizedOperation)
				current = current.Parent;

			return current;
		}

		static IOperation? Unwrap(IOperation? op)
		{
			while (true)
			{
				switch (op)
				{
					case IConversionOperation    conversion   : op = conversion.Operand;    break;
					case IParenthesizedOperation parenthesized: op = parenthesized.Operand; break;
					default                                   : return op;
				}
			}
		}

		/// <summary>
		/// The member whose body holds the operation. Lambdas and local functions are not symbols here, so the
		/// containing symbol already is the member; an accessor stays an accessor.
		/// </summary>
		static IMethodSymbol? OuterMember(ISymbol? symbol)
		{
			while (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction } nested)
				symbol = nested.ContainingSymbol;

			return symbol as IMethodSymbol;
		}

		static bool InBuilderNamespace(INamedTypeSymbol? type)
		{
			var ns = type?.ContainingNamespace?.ToDisplayString();

			return ns != null && (ns == BuilderNamespace || ns.StartsWith(BuilderNamespace + ".", System.StringComparison.Ordinal));
		}

		/// <summary>
		/// <c>source.MappingSchema ?? builder.MappingSchema</c>: the read is the whole right operand and the left operand
		/// is a source's schema. A read nested deeper in the right operand is not the fallback.
		/// </summary>
		static bool IsDocumentedFallback(IOperation read, Model model)
		{
			if (read.Parent is not ICoalesceOperation coalesce || !ReferenceEquals(coalesce.WhenNull, read))
				return false;

			return Unwrap(coalesce.Value) switch
			{
				IConditionalAccessOperation { WhenNotNull: IPropertyReferenceOperation { Property.Name: SchemaProperty, Instance: { } receiver } } access
					=> model.IsSourceCarrier(access.Operation.Type) && !IsParent(access.Operation) && !IsParent(receiver),
				IPropertyReferenceOperation { Property.Name: SchemaProperty, Instance: { } instance }
					=> model.IsSourceCarrier(instance.Type) && !IsParent(instance),
				IInvocationOperation invocation
					=> invocation.TargetMethod.Name == "TryGetRootMappingSchema",
				ILocalReferenceOperation or IParameterReferenceOperation or IFieldReferenceOperation or IPropertyReferenceOperation
					=> model.IsSchema(coalesce.Value.Type),
				_   => false,
			};
		}

		static bool IsParent(IOperation op) => Unwrap(op) switch
		{
			IPropertyReferenceOperation  property  => property.Property.Name == "Parent",
			IParameterReferenceOperation parameter => parameter.Parameter.Name == "parent",
			_                                      => false,
		};

		static bool IsDefinitionSite(IOperation read, IMethodSymbol member, INamedTypeSymbol type, Model model)
		{
			if (!model.IsContext(type))
				return false;

			if (member.MethodKind == MethodKind.PropertyGet && member.AssociatedSymbol is IPropertySymbol { Name: SchemaProperty })
				return true;

			return member.MethodKind == MethodKind.Constructor
				&& read.Parent is ISimpleAssignmentOperation { Target: var target }
				&& target switch
				{
					IFieldReferenceOperation    field    => SymbolEqualityComparer.Default.Equals(field.Field.ContainingType, type) && model.IsSchema(field.Field.Type),
					IPropertyReferenceOperation property => property.Property.Name == SchemaProperty && property.Instance is IInstanceReferenceOperation,
					_                                    => false,
				};
		}

		static bool IsLiveLookupArgument(IOperation read)
		{
			if (read.Parent is not IArgumentOperation { Parent: IInvocationOperation invocation })
				return false;

			var method = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;

			foreach (var (name, typeName) in LiveLookups)
			{
				if (method.Name != name)
					continue;

				for (var type = method.ContainingType; type != null; type = type.ContainingType)
					if (type.Name == typeName)
						return true;
			}

			return false;
		}

		/// <summary><c>ctx.Builder.MappingSchema</c>, implicit <c>Builder.MappingSchema</c> in a context, and the
		/// <c>.Builder.DataContext.MappingSchema</c> spelling: some link of the receiver chain is a context's <c>Builder</c>.</summary>
		static string? FindContextBuilder(IPropertyReferenceOperation op, Model model)
		{
			for (var current = Unwrap(op.Instance); current is IMemberReferenceOperation reference; current = Unwrap(reference.Instance))
			{
				if (reference is IPropertyReferenceOperation { Property.Name: "Builder" } builder
					&& builder.Instance is { } owner
					&& model.IsContext(owner.Type)
					&& !IsParent(owner))
				{
					return owner is IInstanceReferenceOperation ? "this" : owner.Syntax.ToString();
				}
			}

			return null;
		}

		#endregion

		#region Carriers

		readonly struct Carrier(string name, string suggestion)
		{
			public string Name       { get; } = name;
			public string Suggestion { get; } = suggestion;
		}

		static Carrier? FindCarrier(IPropertyReferenceOperation op, IOperation read, IMethodSymbol member, INamedTypeSymbol enclosingType, Model model, bool membersAllowed)
		{
			var semanticModel = op.SemanticModel;
			if (semanticModel == null)
				return null;

			var position        = op.Syntax.SpanStart;
			var staticFunction  = StaticFunction(op.Syntax);
			var instanceAllowed = membersAllowed && !member.IsStatic && staticFunction == null && !InConstructorInitializer(op.Syntax) && model.IsContext(enclosingType);

			// The variable the read is assigned to does not hold a source yet.
			var assigned = read.Parent is IAssignmentOperation assignment && ReferenceEquals(assignment.Value, read)
				? AssignedSymbol(assignment.Target)
				: null;

			Carrier?           best     = null;
			var                bestRank = int.MaxValue;
			DataFlowAnalysis?  flow     = null;

			foreach (var symbol in semanticModel.LookupSymbols(position))
			{
				if (SymbolEqualityComparer.Default.Equals(symbol, assigned))
					continue;

				ITypeSymbol? type;
				bool?        nullable = null;

				// The type gate goes first: the flow and nullability checks below are the expensive part.
				switch (symbol)
				{
					case IParameterSymbol parameter:
						if (parameter.Name == "parent" || !model.IsSourceCarrier(parameter.Type) || !Captured(parameter, staticFunction)
							|| !AssignedAt(parameter, op.Syntax, semanticModel, ref flow))
							continue;
						type = parameter.Type;
						break;

					case ILocalSymbol local:
						if (!model.IsSourceCarrier(local.Type) || !DeclaredBefore(local, op.Syntax) || !Captured(local, staticFunction)
							|| !AssignedAt(local, op.Syntax, semanticModel, ref flow))
							continue;
						type     = local.Type;
						nullable = VarNullability(local, semanticModel);
						break;

					case IFieldSymbol field when instanceAllowed && !field.IsStatic && !field.IsImplicitlyDeclared && IsOwnMember(field, enclosingType):
						type = field.Type;
						break;

					// A root's own MappingSchema returns the live schema, so it is no source; any other context's is.
					case IPropertySymbol property when instanceAllowed && !property.IsStatic && property.Name != "Parent" && IsOwnMember(property, enclosingType)
						&& !(property.Name == SchemaProperty && model.HasRootSchemaGetter(enclosingType)):
						type = property.Type;
						break;

					default:
						continue;
				}

				var (rank, suggestion) = Classify(symbol.Name, type, nullable ?? type?.NullableAnnotation == NullableAnnotation.Annotated, op.Syntax.ToString(), model);

				if (rank < bestRank)
				{
					bestRank = rank;
					best     = new Carrier(symbol.Name, suggestion!);
				}
			}

			return best;
		}

		static ISymbol? AssignedSymbol(IOperation target) => Unwrap(target) switch
		{
			ILocalReferenceOperation     local     => local.Local,
			IParameterReferenceOperation parameter => parameter.Parameter,
			IFieldReferenceOperation     field     => field.Field,
			IPropertyReferenceOperation  property  => property.Property,
			_                                      => null,
		};

		/// <summary>A nullable carrier is suggested as the fallback, which compiles where it may be null.</summary>
		static (int Rank, string? Suggestion) Classify(string name, ITypeSymbol? type, bool nullable, string read, Model model)
		{
			if (model.IsSchema(type))
				return (0, nullable ? $"{name} ?? {read}" : name);

			var access = nullable ? "?." : ".";

			if (model.IsDescriptor(type))
				return (1, Fallback(name + access + "MappingSchema"));

			if (model.IsContextRef(type))
				return (2, Fallback(name + access + "BuildContext.MappingSchema"));

			if (model.IsContext(type))
				return (2, Fallback(name + access + "MappingSchema"));

			return (int.MaxValue, null);

			string Fallback(string source) => nullable ? $"{source} ?? {read}" : source;
		}

		static bool IsOwnMember(ISymbol member, INamedTypeSymbol enclosingType)
		{
			for (var type = enclosingType; type != null; type = type.BaseType)
				if (SymbolEqualityComparer.Default.Equals(member.ContainingType, type))
					return true;

			return false;
		}

		/// <summary>
		/// Named earlier than the read, and not the local whose own initializer holds it - <c>var ms = builder.MappingSchema;</c>
		/// is not a source for itself. The name, not the declaring node: a <c>foreach</c> variable's node spans its body.
		/// </summary>
		static bool DeclaredBefore(ILocalSymbol local, SyntaxNode read)
		{
			if (local.Locations.Length == 0 || local.DeclaringSyntaxReferences.Length == 0)
				return false;

			var name = local.Locations[0];

			if (name.SourceTree != read.SyntaxTree || name.SourceSpan.Start >= read.SpanStart)
				return false;

			return local.DeclaringSyntaxReferences[0].GetSyntax() is not VariableDeclaratorSyntax declarator
				|| !declarator.Span.Contains(read.Span);
		}

		/// <summary>
		/// A pattern or <c>out</c> variable, a local declared without an initializer and an <c>out</c> parameter can be
		/// unassigned where the read sits, so they are a source only where definite assignment holds.
		/// </summary>
		static bool AssignedAt(ISymbol variable, SyntaxNode read, SemanticModel semanticModel, ref DataFlowAnalysis? flow)
		{
			var mayBeUnassigned = variable switch
			{
				IParameterSymbol parameter => parameter.RefKind == RefKind.Out,
				ILocalSymbol     local     => local.DeclaringSyntaxReferences[0].GetSyntax() is SingleVariableDesignationSyntax or VariableDeclaratorSyntax { Initializer: null },
				_                          => false,
			};

			if (!mayBeUnassigned || read is not ExpressionSyntax expression)
				return true;

			flow ??= semanticModel.AnalyzeDataFlow(expression);
			if (flow is not { Succeeded: true })
				return true;

			foreach (var symbol in flow.DefinitelyAssignedOnEntry)
				if (SymbolEqualityComparer.Default.Equals(symbol, variable))
					return true;

			return false;
		}

		/// <summary>The innermost <c>static</c> lambda or local function holding <paramref name="node"/>.</summary>
		static SyntaxNode? StaticFunction(SyntaxNode node)
		{
			foreach (var ancestor in node.Ancestors())
			{
				switch (ancestor)
				{
					case AnonymousFunctionExpressionSyntax lambda when lambda.Modifiers.Any(SyntaxKind.StaticKeyword):
						return lambda;
					case LocalFunctionStatementSyntax function when function.Modifiers.Any(SyntaxKind.StaticKeyword):
						return function;
					case MemberDeclarationSyntax:
						return null;
				}
			}

			return null;
		}

		/// <summary>A static function captures nothing: only variables declared inside it are usable there.</summary>
		static bool Captured(ISymbol variable, SyntaxNode? staticFunction)
		{
			if (staticFunction == null)
				return true;

			foreach (var reference in variable.DeclaringSyntaxReferences)
				if (reference.SyntaxTree == staticFunction.SyntaxTree && staticFunction.Span.Contains(reference.Span))
					return true;

			return false;
		}

		/// <summary>
		/// A <c>var</c> local is nullable by declaration whatever it holds, so its initializer's flow state (or a
		/// <c>foreach</c> element's annotation) decides. <see langword="null"/> for an explicitly typed local.
		/// </summary>
		static bool? VarNullability(ILocalSymbol local, SemanticModel semanticModel) => local.DeclaringSyntaxReferences[0].GetSyntax() switch
		{
			VariableDeclaratorSyntax { Initializer.Value: var value, Parent: VariableDeclarationSyntax { Type.IsVar: true } }
				=> semanticModel.GetTypeInfo(value).Nullability.FlowState == NullableFlowState.MaybeNull,
			ForEachStatementSyntax { Type.IsVar: true } loop
				=> semanticModel.GetForEachStatementInfo(loop).ElementType?.NullableAnnotation == NullableAnnotation.Annotated,
			_   => null,
		};

		static bool InConstructorInitializer(SyntaxNode node)
		{
			foreach (var ancestor in node.Ancestors())
			{
				if (ancestor is ConstructorInitializerSyntax)
					return true;

				if (ancestor is MemberDeclarationSyntax)
					return false;
			}

			return false;
		}

		#endregion

		#region Model

		sealed class Model
		{
			Model(
				INamedTypeSymbol expressionBuilder,
				INamedTypeSymbol buildContext,
				INamedTypeSymbol methodCallBuilder,
				INamedTypeSymbol contextRef,
				INamedTypeSymbol mappingSchema,
				INamedTypeSymbol dataContext,
				INamedTypeSymbol[] descriptors)
			{
				_expressionBuilder = expressionBuilder;
				_buildContext      = buildContext;
				_methodCallBuilder = methodCallBuilder;
				_contextRef        = contextRef;
				_mappingSchema     = mappingSchema;
				_dataContext       = dataContext;
				_descriptors       = descriptors;
			}

			readonly INamedTypeSymbol   _expressionBuilder;
			readonly INamedTypeSymbol   _buildContext;
			readonly INamedTypeSymbol   _methodCallBuilder;
			readonly INamedTypeSymbol   _contextRef;
			readonly INamedTypeSymbol   _mappingSchema;
			readonly INamedTypeSymbol   _dataContext;
			readonly INamedTypeSymbol[] _descriptors;

			readonly ConcurrentDictionary<IPropertySymbol, bool>   _aliases = new(SymbolEqualityComparer.Default);
			readonly ConcurrentDictionary<INamedTypeSymbol, bool> _roots   = new(SymbolEqualityComparer.Default);

			public static Model? Create(IAssemblySymbol assembly, INamedTypeSymbol expressionBuilder, out string? drift)
			{
				var names = new[]
				{
					BuildContextName, MethodCallBuilderName, ContextRefName, MappingSchemaName, DataContextName,
					EntityDescriptorName, ColumnDescriptorName, AssocDescriptorName,
				};

				var types = new INamedTypeSymbol[names.Length];

				for (var i = 0; i < names.Length; i++)
				{
					var type = assembly.GetTypeByMetadataName(names[i]);

					if (type == null)
					{
						drift = $"'{names[i]}' was not found in the assembly that declares '{ExpressionBuilderName}'";
						return null;
					}

					types[i] = type;
				}

				var model = new Model(expressionBuilder, types[0], types[1], types[2], types[3], types[4], [types[5], types[6], types[7]]);

				// The alias set is derived from getter shape, so ExpressionBuilder's own property must still have it.
				var anchor = false;
				foreach (var candidate in expressionBuilder.GetMembers(SchemaProperty))
					if (candidate is IPropertySymbol property && model.IsAlias(property))
						anchor = true;

				if (!anchor)
				{
					drift = $"'{ExpressionBuilderName}.{SchemaProperty}' is no longer a '<IDataContext member>.{SchemaProperty}' getter";
					return null;
				}

				// Members the triggers match by name: a rename would switch a trigger off without a diagnostic.
				if (!HasMember(types[0], "Builder", SymbolKind.Property))
				{
					drift = $"'{BuildContextName}.Builder' was not found in the assembly that declares '{ExpressionBuilderName}'";
					return null;
				}

				if (!HasMember(types[1], "BuildMethodCall", SymbolKind.Method))
				{
					drift = $"'{MethodCallBuilderName}.BuildMethodCall' was not found in the assembly that declares '{ExpressionBuilderName}'";
					return null;
				}

				drift = null;
				return model;
			}

			static bool HasMember(INamedTypeSymbol type, string name, SymbolKind kind)
			{
				foreach (var member in type.GetMembers(name))
					if (member.Kind == kind)
						return true;

				return false;
			}

			public bool IsSchema(ITypeSymbol? type) => DerivesFrom(type, _mappingSchema);

			public bool IsContextRef(ITypeSymbol? type) => DerivesFrom(type, _contextRef);

			public bool IsContext(ITypeSymbol? type)
			{
				if (type == null)
					return false;

				if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _buildContext))
					return true;

				foreach (var implemented in type.AllInterfaces)
					if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, _buildContext))
						return true;

				return false;
			}

			public bool IsDescriptor(ITypeSymbol? type)
			{
				foreach (var descriptor in _descriptors)
					if (DerivesFrom(type, descriptor))
						return true;

				return false;
			}

			public bool IsSourceCarrier(ITypeSymbol? type) => IsContext(type) || IsContextRef(type) || IsDescriptor(type) || IsSchema(type);

			public bool IsBuildMethodCall(IMethodSymbol member)
			{
				for (var method = member; method != null; method = method.OverriddenMethod)
					if (method.Name == "BuildMethodCall" && SymbolEqualityComparer.Default.Equals(method.ContainingType, _methodCallBuilder))
						return true;

				return false;
			}

			/// <summary>An alias read, or <c>IDataContext.MappingSchema</c> reached through an <c>ExpressionBuilder</c>.</summary>
			public bool IsLiveRead(IPropertyReferenceOperation op)
			{
				if (IsAlias(op.Property))
					return true;

				if (!SymbolEqualityComparer.Default.Equals(op.Property.ContainingType, _dataContext))
					return false;

				for (var current = Unwrap(op.Instance); current != null; current = Unwrap((current as IMemberReferenceOperation)?.Instance))
				{
					if (SymbolEqualityComparer.Default.Equals(current.Type, _expressionBuilder))
						return true;

					if (current is not IMemberReferenceOperation)
						break;
				}

				return false;
			}

			public bool IsAlias(IPropertySymbol property)
				=> _aliases.GetOrAdd(property, ComputeAlias);

			bool ComputeAlias(IPropertySymbol property)
			{
				if (property.Name != SchemaProperty || !IsSchema(property.Type) || !InBuilderNamespace(property.ContainingType))
					return false;

				foreach (var reference in property.DeclaringSyntaxReferences)
				{
					if (GetterExpression(reference.GetSyntax()) is not MemberAccessExpressionSyntax access
						|| access.Name.Identifier.ValueText != SchemaProperty
						|| access.Expression is not IdentifierNameSyntax receiver)
					{
						continue;
					}

					for (var type = property.ContainingType; type != null; type = type.BaseType)
					{
						foreach (var candidate in type.GetMembers(receiver.Identifier.ValueText))
						{
							var candidateType = candidate switch
							{
								IFieldSymbol    field => field.Type,
								IPropertySymbol prop  => prop.Type,
								_                     => null,
							};

							if (IsDataContext(candidateType))
								return true;
						}
					}
				}

				return false;
			}

			/// <summary>The type's effective <c>MappingSchema</c> getter returns <c>&lt;ExpressionBuilder member&gt;.MappingSchema</c>.</summary>
			public bool HasRootSchemaGetter(INamedTypeSymbol type)
				=> _roots.GetOrAdd(type, ComputeRoot);

			bool ComputeRoot(INamedTypeSymbol type)
			{
				for (var current = type; current != null; current = current.BaseType)
				{
					foreach (var candidate in current.GetMembers(SchemaProperty))
					{
						if (candidate is not IPropertySymbol { IsAbstract: false } property)
							continue;

						foreach (var reference in property.DeclaringSyntaxReferences)
						{
							if (GetterExpression(reference.GetSyntax()) is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } access
								&& access.Name.Identifier.ValueText == SchemaProperty)
							{
								for (var owner = type; owner != null; owner = owner.BaseType)
									foreach (var member in owner.GetMembers(receiver.Identifier.ValueText))
										if (SymbolEqualityComparer.Default.Equals(MemberType(member), _expressionBuilder))
											return true;
							}
						}

						return false;
					}
				}

				return false;
			}

			static ExpressionSyntax? GetterExpression(SyntaxNode node)
			{
				if (node is not PropertyDeclarationSyntax declaration)
					return null;

				if (declaration.ExpressionBody != null)
					return declaration.ExpressionBody.Expression;

				foreach (var accessor in declaration.AccessorList?.Accessors ?? default)
				{
					if (!accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
						continue;

					if (accessor.ExpressionBody != null)
						return accessor.ExpressionBody.Expression;

					return accessor.Body is { Statements: { Count: 1 } statements } && statements[0] is ReturnStatementSyntax { Expression: { } returned } ? returned : null;
				}

				return null;
			}

			static ITypeSymbol? MemberType(ISymbol member) => member switch
			{
				IFieldSymbol    field => field.Type,
				IPropertySymbol prop  => prop.Type,
				_                     => null,
			};

			bool IsDataContext(ITypeSymbol? type)
			{
				if (type == null)
					return false;

				if (SymbolEqualityComparer.Default.Equals(type, _dataContext))
					return true;

				foreach (var implemented in type.AllInterfaces)
					if (SymbolEqualityComparer.Default.Equals(implemented, _dataContext))
						return true;

				return false;
			}

			/// <summary>An instance context or context-reference member of the type, declared or inherited, other than <c>Parent</c>.</summary>
			public ISymbol? FindSourceMember(INamedTypeSymbol type)
			{
				for (var current = type; current != null; current = current.BaseType)
				{
					foreach (var candidate in current.GetMembers())
					{
						if (candidate.IsStatic || candidate.IsImplicitlyDeclared || candidate.Name == "Parent")
							continue;

						var candidateType = candidate switch
						{
							IFieldSymbol    field => field.Type,
							IPropertySymbol prop  => prop.Type,
							_                     => null,
						};

						if (IsContext(candidateType) || IsContextRef(candidateType))
							return candidate;
					}
				}

				return null;
			}

			static bool DerivesFrom(ITypeSymbol? type, INamedTypeSymbol baseType)
			{
				for (var current = type; current != null; current = current.BaseType)
					if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType))
						return true;

				return false;
			}
		}

		#endregion
	}
}
