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
	/// A <i>live read</i> is a read of a property in <c>LinqToDB.Internal.Linq.Builder</c> whose getter is
	/// <c>=&gt; &lt;IDataContext member&gt;.MappingSchema</c> (derived, not listed), or of <c>IDataContext.MappingSchema</c>
	/// through an <c>ExpressionBuilder</c>. It is reported when one of these holds:
	/// </para>
	/// <list type="bullet">
	/// <item>a context's own <c>MappingSchema</c> getter, or the constructor assignment of its backing field, reads it
	/// while the type holds a source context other than <c>Parent</c>;</item>
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
	/// <c>BuildMethodCall</c>, and an alias written as an auto-property assigned in a constructor. Method-attribute
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

			if (!model.IsLiveRead(op, member))
				return;

			var read = Climb(op);

			if (IsDocumentedFallback(read, model))
				return;

			var enclosingType = member.ContainingType;

			// Definition site: the context decides its own schema here, so only the type's members can make it wrong.
			if (IsDefinitionSite(read, member, enclosingType, model))
			{
				var source = model.FindSourceMember(enclosingType);
				if (source != null)
					Report(context, op, $"'{enclosingType.Name}' holds the source context '{source.Name}': return its MappingSchema instead of the data context's");

				return;
			}

			var carrier = FindCarrier(op, member, enclosingType, model);
			if (carrier != null)
			{
				Report(context, op, $"'{carrier.Value.Name}' is in scope: read '{carrier.Value.Suggestion}' instead of the data context's MappingSchema");
				return;
			}

			var throughContext = FindContextBuilder(op, model);
			if (throughContext != null)
			{
				Report(context, op, $"Read through the context '{throughContext}': use '{throughContext}.MappingSchema' instead of its Builder's data-context schema");
				return;
			}

			if (model.IsBuildMethodCall(member) && !IsLiveLookupArgument(read))
				Report(context, op, "Inside BuildMethodCall the source is methodCall.Arguments[0]: read its snapshot via 'builder.TryGetRootMappingSchema(...) ?? builder.MappingSchema' instead of the data context's MappingSchema");
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
				IConditionalAccessOperation { WhenNotNull: IPropertyReferenceOperation { Property.Name: SchemaProperty } } access
					=> model.IsSourceCarrier(access.Operation.Type) && !IsParent(access.Operation),
				IPropertyReferenceOperation { Property.Name: SchemaProperty, Instance: { } instance }
					=> model.IsSourceCarrier(instance.Type) && !IsParent(instance),
				IInvocationOperation invocation
					=> invocation.TargetMethod.Name == "TryGetRootMappingSchema",
				ILocalReferenceOperation or IParameterReferenceOperation or IFieldReferenceOperation
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
				&& read.Parent is ISimpleAssignmentOperation { Target: IFieldReferenceOperation field }
				&& SymbolEqualityComparer.Default.Equals(field.Field.ContainingType, type)
				&& model.IsSchema(field.Field.Type);
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
					&& model.IsContext(owner.Type))
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

		static Carrier? FindCarrier(IPropertyReferenceOperation op, IMethodSymbol member, INamedTypeSymbol enclosingType, Model model)
		{
			var semanticModel = op.SemanticModel;
			if (semanticModel == null)
				return null;

			var position        = op.Syntax.SpanStart;
			var instanceAllowed = !member.IsStatic && !InStaticFunction(op.Syntax) && !InConstructorInitializer(op.Syntax) && model.IsContext(enclosingType);

			Carrier? best     = null;
			var      bestRank = int.MaxValue;

			foreach (var symbol in semanticModel.LookupSymbols(position))
			{
				ITypeSymbol? type;

				switch (symbol)
				{
					case IParameterSymbol parameter:
						if (parameter.Name == "parent")
							continue;
						type = parameter.Type;
						break;

					case ILocalSymbol local:
						if (!DeclaredBefore(local, op.Syntax))
							continue;
						type = local.Type;
						break;

					case IFieldSymbol field when instanceAllowed && !field.IsStatic && !field.IsImplicitlyDeclared && IsOwnMember(field, enclosingType):
						type = field.Type;
						break;

					// The context's own MappingSchema is what R-def judges; counting it here would report every live
					// read in every context, roots included.
					case IPropertySymbol property when instanceAllowed && !property.IsStatic && property.Name is not ("Parent" or SchemaProperty) && IsOwnMember(property, enclosingType):
						type = property.Type;
						break;

					default:
						continue;
				}

				var (rank, suggestion) = Classify(symbol.Name, type, model);

				if (rank < bestRank)
				{
					bestRank = rank;
					best     = new Carrier(symbol.Name, suggestion!);
				}
			}

			return best;
		}

		static (int Rank, string? Suggestion) Classify(string name, ITypeSymbol? type, Model model)
		{
			if (model.IsSchema(type))
				return (0, name);

			if (model.IsDescriptor(type))
				return (1, name + ".MappingSchema");

			if (model.IsContextRef(type))
				return (2, name + ".BuildContext.MappingSchema");

			if (model.IsContext(type))
				return (2, name + ".MappingSchema");

			return (int.MaxValue, null);
		}

		static bool IsOwnMember(ISymbol member, INamedTypeSymbol enclosingType)
		{
			for (var type = enclosingType; type != null; type = type.BaseType)
				if (SymbolEqualityComparer.Default.Equals(member.ContainingType, type))
					return true;

			return false;
		}

		/// <summary>
		/// Declared earlier than the read, and not the local whose own initializer holds it - <c>var ms = builder.MappingSchema;</c>
		/// is not a source for itself.
		/// </summary>
		static bool DeclaredBefore(ILocalSymbol local, SyntaxNode read)
		{
			if (local.DeclaringSyntaxReferences.Length == 0)
				return false;

			var declaration = local.DeclaringSyntaxReferences[0];

			if (declaration.SyntaxTree != read.SyntaxTree || declaration.Span.Start >= read.SpanStart)
				return false;

			return !declaration.Span.Contains(read.Span);
		}

		static bool InStaticFunction(SyntaxNode node)
		{
			foreach (var ancestor in node.Ancestors())
			{
				switch (ancestor)
				{
					case AnonymousFunctionExpressionSyntax lambda when lambda.Modifiers.Any(SyntaxKind.StaticKeyword):
						return true;
					case LocalFunctionStatementSyntax function when function.Modifiers.Any(SyntaxKind.StaticKeyword):
						return true;
					case MemberDeclarationSyntax:
						return false;
				}
			}

			return false;
		}

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

			readonly ConcurrentDictionary<IPropertySymbol, bool> _aliases = new(SymbolEqualityComparer.Default);

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
					drift = $"'{ExpressionBuilderName}.{SchemaProperty}' is no longer an expression-bodied '=> <IDataContext member>.{SchemaProperty}' getter";
					return null;
				}

				drift = null;
				return model;
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

			/// <summary>An alias read, outside the alias's own getter, or <c>IDataContext.MappingSchema</c> reached through an <c>ExpressionBuilder</c>.</summary>
			public bool IsLiveRead(IPropertyReferenceOperation op, IMethodSymbol member)
			{
				if (member is { MethodKind: MethodKind.PropertyGet, AssociatedSymbol: IPropertySymbol owner } && IsAlias(owner))
					return false;

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
					if (reference.GetSyntax() is not PropertyDeclarationSyntax { ExpressionBody.Expression: MemberAccessExpressionSyntax access }
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

			/// <summary>An instance context member of the type, declared or inherited, other than <c>Parent</c>.</summary>
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

						if (IsContext(candidateType))
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
