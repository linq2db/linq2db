using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CodeGenerators
{
	/// <summary>
	/// Guards the <c>Expressions</c> member map against shadowing. Map entries are applied in the expose pass,
	/// before the build visitor consults member translators or <c>Sql.ExpressionAttribute</c>-derived attributes,
	/// so whatever the map covers is dead code on the other two paths - and it still reads as live.
	/// <list type="bullet">
	/// <item><c>LINQ2DB0007</c> - a map entry whose member is also registered with a member translator covering
	/// the entry's section.</item>
	/// <item><c>LINQ2DB0008</c> - a map entry that makes one of the member's attributes unreachable: a common
	/// entry hides every attribute, a provider entry hides the attributes configured for that provider.</item>
	/// <item><c>LINQ2DB0009</c> - the map could not be read, so the two rules above cannot be trusted.</item>
	/// </list>
	/// The rule is "the same member on both sides", so cooperative chains - an entry that expands <i>into</i> a
	/// registered member - are excluded by construction.
	/// </summary>
	/// <remarks>
	/// A registration in the shared translation namespaces is inherited by every provider translator and covers
	/// every section; one in <c>LinqToDB.Internal.DataProvider.&lt;P&gt;</c> covers the sections whose key, up to
	/// its first dot, is <c>P</c>. Sections are recognised by the shape of the table initializers rather than by
	/// field names, so a rename keeps the rule alive and a reshaped table reports <c>LINQ2DB0009</c>.
	/// </remarks>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class MemberMapShadowAnalyzer : DiagnosticAnalyzer
	{
		const string ExpressionsTypeName     = "LinqToDB.Linq.Expressions";
		const string ExtensionsTypeName      = "LinqToDB.Internal.DataProvider.Translation.TranslationRegistrationExtensions";
		const string AttributeTypeName       = "LinqToDB.Sql+ExpressionAttribute";
		const string ProviderNamespacePrefix = "LinqToDB.Internal.DataProvider.";

		static readonly string[] SharedTranslationNamespaces =
		[
			"LinqToDB.Internal.DataProvider.Translation",
			"LinqToDB.Linq.Translation",
		];

		static readonly DiagnosticDescriptor ShadowedRegistration = new(
			id:                 "LINQ2DB0007",
			title:              "A member map entry shadows a member translator registration",
			messageFormat:      "'{0}' is mapped in the {1} section and also registered by {2}; the map entry is applied first, so the registration never runs there. Remove the map entry and move any provider-specific emulation into a translator override.",
			category:           "Usage",
			defaultSeverity:    DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description:        "Expressions map entries are applied in the expose pass, before the member translator registry is consulted, so a registration for a mapped member is dead code for every configuration the entry covers.",
			customTags:         WellKnownDiagnosticTags.CompilationEnd);

		static readonly DiagnosticDescriptor ShadowedAttribute = new(
			id:                 "LINQ2DB0008",
			title:              "A member map entry makes a function attribute unreachable",
			messageFormat:      "'{0}' is mapped in the {1} section, which makes its {2} for configuration '{3}' unreachable. Remove the map entry and implement the provider form as a translator, or remove the dead attribute.",
			category:           "Usage",
			defaultSeverity:    DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description:        "Expressions map entries are applied before Sql.ExpressionAttribute-derived attributes are read, so an attribute whose configuration the entry covers is never used.",
			customTags:         WellKnownDiagnosticTags.CompilationEnd);

		static readonly DiagnosticDescriptor ModelUnreadable = new(
			id:                 "LINQ2DB0009",
			title:              "The Expressions member map could not be read",
			messageFormat:      "The Expressions member map could not be read: {0}. LINQ2DB0007 and LINQ2DB0008 cannot be trusted until the reader is updated.",
			category:           "Usage",
			defaultSeverity:    DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description:        "Sections and entries are recognised from the shape of the table initializers in Expressions. A shape this analyzer cannot read must fail loudly rather than silently report no shadows.",
			customTags:         WellKnownDiagnosticTags.CompilationEnd);

		/// <inheritdoc/>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ShadowedRegistration, ShadowedAttribute, ModelUnreadable];

		/// <inheritdoc/>
		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(static startContext =>
			{
				// The source assembly, not the compilation: the invariant is about linq2db's own tables, and a
				// consumer that merely references linq2db must cost nothing.
				var assembly        = startContext.Compilation.Assembly;
				var expressionsType = assembly.GetTypeByMetadataName(ExpressionsTypeName);

				if (expressionsType == null)
					return;

				var extensionsType = assembly.GetTypeByMetadataName(ExtensionsTypeName);
				var attributeType  = assembly.GetTypeByMetadataName(AttributeTypeName);

				var entries       = new ConcurrentBag<MapEntry>();
				var registrations = new ConcurrentBag<Registration>();
				var fieldSections = new ConcurrentDictionary<IFieldSymbol, string>(SymbolEqualityComparer.Default);
				var failures      = new ConcurrentBag<(Location Location, string Reason)>();

				startContext.RegisterOperationAction(operationContext =>
				{
					var invocation = (IInvocationOperation)operationContext.Operation;
					var method     = invocation.TargetMethod;

					if (method.Name == "M" && SymbolEqualityComparer.Default.Equals(method.ContainingType, expressionsType))
					{
						CollectEntry(invocation, entries, failures);
					}
					else if (method.Name is "RegisterMethod" or "RegisterMember" or "RegisterConstructor" or "RegisterReplacement"
						&& extensionsType != null
						&& SymbolEqualityComparer.Default.Equals(method.ContainingType, extensionsType))
					{
						CollectRegistration(invocation, operationContext.ContainingSymbol, extensionsType, registrations, failures);
					}
					else if (method.Name == "Add"
						&& SymbolEqualityComparer.Default.Equals(operationContext.ContainingSymbol.ContainingType, expressionsType))
					{
						CollectFieldSection(invocation, expressionsType, fieldSections);
					}
				}, OperationKind.Invocation);

				startContext.RegisterCompilationEndAction(endContext =>
					Report(endContext, expressionsType, attributeType, entries, registrations, fieldSections, failures));
			});
		}

		#region Collection

		static void CollectEntry(IInvocationOperation invocation, ConcurrentBag<MapEntry> entries, ConcurrentBag<(Location, string)> failures)
		{
			// The public MapMember overloads forward a caller-supplied expression through M; only a lambda written
			// in place is a table entry.
			if (invocation.Arguments.Length != 1 || GetLambda(invocation.Arguments[0].Value) is not { } lambda)
				return;

			var location = invocation.Syntax.GetLocation();

			if (ResolveMember(lambda) is not { } key)
			{
				failures.Add((location, "a map entry's lambda does not name a method, property, field or constructor"));
				return;
			}

			if (ResolveSection(invocation) is not { } section)
			{
				failures.Add((location, $"the section of the map entry for '{Display(key.Member)}' could not be determined - an entry must sit directly in a dictionary initializer that is either a field initializer or the value of a section-keyed entry"));
				return;
			}

			entries.Add(new MapEntry(key, section, location));
		}

		static void CollectRegistration(
			IInvocationOperation              invocation,
			ISymbol                           containingSymbol,
			INamedTypeSymbol                  extensionsType,
			ConcurrentBag<Registration>       registrations,
			ConcurrentBag<(Location, string)> failures)
		{
			IOperation? pattern            = null;
			var         isGenericTypeMatch = false;

			foreach (var argument in invocation.Arguments)
			{
				if (argument.Parameter is not { } parameter)
					continue;

				// Ordinal 0 is the extended registration; the pattern lambda follows it.
				if (parameter.Ordinal == 1)
					pattern = argument.Value;
				else if (parameter.Name == "isGenericTypeMatch" && argument.Value.ConstantValue is { HasValue: true, Value: true })
					isGenericTypeMatch = true;
			}

			if (pattern == null)
				return;

			var registeringType = containingSymbol.ContainingType;

			if (GetLambda(pattern) is not { } lambda)
			{
				// The extension overloads forward their caller's pattern; anywhere else the member is unknowable here.
				if (!SymbolEqualityComparer.Default.Equals(registeringType, extensionsType))
					failures.Add((invocation.Syntax.GetLocation(), $"a {invocation.TargetMethod.Name} pattern is not a lambda written in place, so the member it registers is unknown"));

				return;
			}

			if (ResolveMember(lambda) is not { } key || registeringType == null)
				return;

			registrations.Add(new Registration(key, isGenericTypeMatch, registeringType, ProviderOf(registeringType.ContainingNamespace)));
		}

		/// <summary>
		/// Records <c>{ "key", field }</c> pairs of an outer section table, so entries written in a field's own
		/// initializer can be attributed to the section the field is registered under.
		/// </summary>
		static void CollectFieldSection(IInvocationOperation invocation, INamedTypeSymbol expressionsType, ConcurrentDictionary<IFieldSymbol, string> fieldSections)
		{
			if (invocation.Parent is not IObjectOrCollectionInitializerOperation || invocation.Arguments.Length != 2)
				return;

			if (invocation.Arguments[0].Value.ConstantValue is not { HasValue: true, Value: string key })
				return;

			if (Unwrap(invocation.Arguments[1].Value) is IFieldReferenceOperation { Field: var field }
				&& SymbolEqualityComparer.Default.Equals(field.ContainingType, expressionsType))
			{
				fieldSections[field] = key;
			}
		}

		static SectionRef? ResolveSection(IInvocationOperation entry)
		{
			// M(...) is the key argument of an initializer element's implicit Add(key, value).
			if (entry.Parent is not IArgumentOperation { Parameter.Ordinal: 0, Parent: IInvocationOperation add }
				|| add.Parent is not IObjectOrCollectionInitializerOperation { Parent: IObjectCreationOperation table })
			{
				return null;
			}

			var parent = table.Parent;

			while (parent is IConversionOperation)
				parent = parent.Parent;

			return parent switch
			{
				IFieldInitializerOperation { InitializedFields.Length: > 0 } initializer
					=> new SectionRef(null, initializer.InitializedFields[0]),

				// A provider table: the value of an outer section-keyed initializer element.
				IArgumentOperation { Parameter.Ordinal: 1, Parent: IInvocationOperation outerAdd }
					when outerAdd.Parent is IObjectOrCollectionInitializerOperation
						&& outerAdd.Arguments.Length == 2
						&& outerAdd.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string key }
					=> new SectionRef(key, null),

				_ => null,
			};
		}

		static IAnonymousFunctionOperation? GetLambda(IOperation operation)
		{
			while (true)
			{
				switch (operation)
				{
					case IAnonymousFunctionOperation lambda:
						return lambda;
					case IConversionOperation conversion:
						operation = conversion.Operand;
						break;
					case IDelegateCreationOperation creation:
						operation = creation.Target;
						break;
					default:
						return null;
				}
			}
		}

		/// <summary>
		/// The member a pattern lambda names, keyed as <c>MemberHelper.MemberInfoWithType</c> keys it at run time:
		/// the receiver's static type for an instance member, the declaring type otherwise.
		/// </summary>
		static MemberKey? ResolveMember(IAnonymousFunctionOperation lambda)
		{
			var body = lambda.Body.Operations.Length == 1 ? lambda.Body.Operations[0] : null;

			var value = body switch
			{
				IReturnOperation { ReturnedValue: { } returned } => returned,
				IExpressionStatementOperation statement          => statement.Operation,
				_                                                => null,
			};

			if (value == null)
				return null;

			return Unwrap(value) switch
			{
				IInvocationOperation     invocation => new MemberKey(invocation.Instance?.Type ?? invocation.TargetMethod.ContainingType, invocation.TargetMethod),
				IPropertyReferenceOperation property => new MemberKey(property.Instance?.Type ?? property.Property.ContainingType, property.Property),
				IFieldReferenceOperation    field    => new MemberKey(field.Instance?.Type ?? field.Field.ContainingType, field.Field),
				IObjectCreationOperation { Constructor: { } constructor } => new MemberKey(constructor.ContainingType, constructor),
				_ => null,
			};
		}

		static IOperation Unwrap(IOperation operation)
		{
			while (operation is IConversionOperation conversion)
				operation = conversion.Operand;

			return operation;
		}

		/// <summary>
		/// <see langword="null"/> for a registration every provider inherits; otherwise the provider segment of a
		/// <c>LinqToDB.Internal.DataProvider.&lt;P&gt;</c> namespace.
		/// </summary>
		static string? ProviderOf(INamespaceSymbol? ns)
		{
			var name = ns?.ToDisplayString() ?? string.Empty;

			foreach (var shared in SharedTranslationNamespaces)
				if (name == shared || name.StartsWith(shared + ".", StringComparison.Ordinal))
					return null;

			if (!name.StartsWith(ProviderNamespacePrefix, StringComparison.Ordinal))
				return null;

			var rest = name.Substring(ProviderNamespacePrefix.Length);
			var dot  = rest.IndexOf('.');

			return dot < 0 ? rest : rest.Substring(0, dot);
		}

		#endregion

		#region Reporting

		static void Report(
			CompilationAnalysisContext                    context,
			INamedTypeSymbol                              expressionsType,
			INamedTypeSymbol?                             attributeType,
			ConcurrentBag<MapEntry>                       entries,
			ConcurrentBag<Registration>                   registrations,
			ConcurrentDictionary<IFieldSymbol, string>    fieldSections,
			ConcurrentBag<(Location Location, string Reason)> failures)
		{
			foreach (var (location, reason) in failures)
				context.ReportDiagnostic(Diagnostic.Create(ModelUnreadable, location, reason));

			if (entries.IsEmpty && failures.IsEmpty)
			{
				var location = expressionsType.Locations.Length > 0 ? expressionsType.Locations[0] : Location.None;
				context.ReportDiagnostic(Diagnostic.Create(ModelUnreadable, location, "no member map entries were found"));
				return;
			}

			// Deterministic choice of the registration a diagnostic names, whatever order the bag yields.
			var orderedRegistrations = registrations
				.OrderBy(static r => r.Type.ToDisplayString(), StringComparer.Ordinal)
				.ToList();

			foreach (var entry in entries)
			{
				string section;

				if (entry.Section.Key != null)
				{
					section = entry.Section.Key;
				}
				else if (entry.Section.Field != null && fieldSections.TryGetValue(entry.Section.Field, out var fieldKey))
				{
					section = fieldKey;
				}
				else
				{
					context.ReportDiagnostic(Diagnostic.Create(ModelUnreadable, entry.Location,
						$"field '{entry.Section.Field?.Name}' holds map entries but is not registered under a section key"));
					continue;
				}

				var sectionDisplay = section.Length == 0 ? "common" : $"'{section}'";

				foreach (var registration in orderedRegistrations)
				{
					if (Covers(registration.Provider, section) && Matches(entry.Key, registration))
					{
						context.ReportDiagnostic(Diagnostic.Create(ShadowedRegistration, entry.Location,
							Display(entry.Key.Member), sectionDisplay, registration.Type.ToDisplayString()));
						break;
					}
				}

				if (attributeType == null)
					continue;

				foreach (var attribute in entry.Key.Member.GetAttributes())
				{
					if (!InheritsFrom(attribute.AttributeClass, attributeType))
						continue;

					var configuration = GetConfiguration(attribute);

					var dead = section.Length == 0
						|| configuration != null
							&& (configuration == section || configuration.StartsWith(section + ".", StringComparison.Ordinal));

					if (dead)
					{
						context.ReportDiagnostic(Diagnostic.Create(ShadowedAttribute, entry.Location,
							Display(entry.Key.Member), sectionDisplay, attribute.AttributeClass!.Name, configuration ?? "(default)"));
					}
				}
			}
		}

		static bool Covers(string? provider, string section)
		{
			if (section.Length == 0 || provider == null)
				return true;

			var dot  = section.IndexOf('.');
			var root = dot < 0 ? section : section.Substring(0, dot);

			return string.Equals(root, provider, StringComparison.OrdinalIgnoreCase);
		}

		static bool Matches(MemberKey entry, Registration registration)
		{
			if (!SymbolEqualityComparer.Default.Equals(entry.Type, registration.Key.Type))
				return false;

			if (SymbolEqualityComparer.Default.Equals(entry.Member, registration.Key.Member))
				return true;

			// TranslationRegistration stores the generic method definition for a generic-match registration.
			return registration.IsGenericTypeMatch
				&& entry.Member is IMethodSymbol { IsGenericMethod: true } entryMethod
				&& registration.Key.Member is IMethodSymbol { IsGenericMethod: true } registeredMethod
				&& SymbolEqualityComparer.Default.Equals(entryMethod.ConstructedFrom, registeredMethod.ConstructedFrom);
		}

		static bool InheritsFrom(INamedTypeSymbol? type, INamedTypeSymbol baseType)
		{
			for (; type != null; type = type.BaseType)
				if (SymbolEqualityComparer.Default.Equals(type, baseType))
					return true;

			return false;
		}

		static string? GetConfiguration(AttributeData attribute)
		{
			string? configuration = null;

			if (attribute.AttributeConstructor is { } constructor)
			{
				for (var i = 0; i < constructor.Parameters.Length && i < attribute.ConstructorArguments.Length; i++)
				{
					if (constructor.Parameters[i].Name == "configuration")
						configuration = attribute.ConstructorArguments[i].Value as string;
				}
			}

			foreach (var named in attribute.NamedArguments)
			{
				if (named.Key == "Configuration")
					configuration = named.Value.Value as string;
			}

			return string.IsNullOrEmpty(configuration) ? null : configuration;
		}

		static string Display(ISymbol member) => member.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

		#endregion

		sealed record MemberKey(ITypeSymbol Type, ISymbol Member);

		sealed record SectionRef(string? Key, IFieldSymbol? Field);

		sealed record MapEntry(MemberKey Key, SectionRef Section, Location Location);

		sealed record Registration(MemberKey Key, bool IsGenericTypeMatch, INamedTypeSymbol Type, string? Provider);
	}
}
