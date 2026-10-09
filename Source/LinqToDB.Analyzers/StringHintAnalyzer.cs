using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LinqToDB.Analyzers
{
	/// <summary>
	/// Reports a raw-text hint - <c>TableHint</c>, <c>With</c>, <c>TablesInScopeHint</c> or <c>QueryHint</c> called with
	/// a constant string - whose text is one a provider's typed hint helper already emits, e.g.
	/// <c>TableHint("NOLOCK")</c> where <c>AsSqlServer().WithNoLock()</c> exists.
	/// </summary>
	/// <remarks>
	/// The generic overloads on <c>LinqExtensions</c> emit their text for every provider a query runs on, while a typed
	/// helper emits only on its own provider. On a provider-specific receiver (<c>AsSqlServer().TableHint("NOLOCK")</c>)
	/// the two are interchangeable, so the rule is a warning there. On a generic receiver the rewrite also restricts
	/// the hint to the chosen provider, so the message names every provider that has the hint and the code fix offers
	/// one rewrite per provider.
	/// <para>
	/// Typed helpers are found by the naming convention of the generated hint templates rather than by a hard-coded
	/// table: a <c>const string</c> in a <c>Table</c>, <c>Query</c> or <c>Hint</c> class nested in a
	/// <c>LinqToDB.DataProvider.&lt;P&gt;.&lt;P&gt;Hints</c> type maps to a parameterless extension method of the same
	/// <c>*Hints</c> type - <c>With{N}</c> / <c>{N}Hint</c> for a table, <c>With{N}InScope</c> / <c>{N}InScopeHint</c> for
	/// tables in scope, <c>Option{N}</c> / <c>{N}Hint</c> for a query. Only hint classes and methods covered
	/// by that lookup can trigger the rule, so a constant without a typed helper is never reported. The test suite runs
	/// the lookup over the real linq2db assembly, so a template change that breaks the convention fails a test rather
	/// than silently narrowing the rule.
	/// </para>
	/// </remarks>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class StringHintAnalyzer : DiagnosticAnalyzer
	{
		/// <summary>Diagnostic id for the string hint with a typed equivalent rule.</summary>
		public const string DiagnosticId = "L2DB2001";

		/// <summary>Relative path of the hints guide inside the linq2db package, cited by the message.</summary>
		public const string GuidePath = "skills/linq2db/docs/hints.md";

		/// <summary><see cref="Diagnostic.Properties"/> key: number of typed rewrites offered.</summary>
		public const string FixCountKey = "FixCount";

		/// <summary><see cref="Diagnostic.Properties"/> key: <c>"true"</c> when the receiver is not provider-specific, so a rewrite restricts the hint to one provider.</summary>
		public const string GenericReceiverKey = "GenericReceiver";

		/// <summary>Per-rewrite property key prefixes; the rewrite index is appended (<c>Fix0.Helper</c>, ...).</summary>
		public const string FixProviderKey    = ".Provider";
		/// <summary>Namespace that declares the typed helper (and its <c>As*</c> method).</summary>
		public const string FixNamespaceKey   = ".Namespace";
		/// <summary>The <c>As*</c> method to insert before the helper, empty when the receiver already has the provider type.</summary>
		public const string FixAsMethodKey    = ".AsMethod";
		/// <summary><c>"true"</c> when <c>AsQueryable()</c> must precede the <c>As*</c> call (a table receiver needs the query form).</summary>
		public const string FixAsQueryableKey = ".AsQueryable";
		/// <summary>The typed helper method name.</summary>
		public const string FixHelperKey      = ".Helper";

		const string LinqExtensionsMetadataName = "LinqToDB.LinqExtensions";
		const string ITableMetadataName         = "LinqToDB.ITable`1";

		const string TableHintName         = "TableHint";
		const string WithName              = "With";
		const string TablesInScopeHintName = "TablesInScopeHint";
		const string QueryHintName         = "QueryHint";

		static readonly LocalizableString Title = "String hint has a typed equivalent";

		const string MessageFormat = "Hint '{0}' has a typed equivalent: {1}. linq2db ships usage guidance for hints in its package: " + GuidePath;

		const string Description =
			"A hint passed as text to TableHint, With, TablesInScopeHint or QueryHint has a typed helper that emits the same SQL. " +
			"The typed helper is checked by the compiler and emits only on its own provider, while the generic text overloads emit their text for every provider the query runs on. " +
			"On a receiver that is already provider-specific (AsSqlServer() and the like) the rewrite is exact. On a generic receiver it also restricts the hint to the chosen provider.";

		// RS1032 wants a multi-sentence message to end with a period. This one ends with the guide's package path
		// instead, so the path can be copied as is; a trailing period would read as part of the file name.
#pragma warning disable RS1032
		internal static readonly DiagnosticDescriptor Rule = new(
			id:                 DiagnosticId,
			title:              Title,
			messageFormat:      MessageFormat,
			category:           "LinqToDB",
			defaultSeverity:    DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description:        Description,
			helpLinkUri:        "https://github.com/linq2db/linq2db/wiki/L2DB2001");
#pragma warning restore RS1032

		/// <inheritdoc/>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		{
			get { return ImmutableArray.Create(Rule); }
		}

		/// <inheritdoc/>
		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(static startContext =>
			{
				var linqExtensions = startContext.Compilation.GetTypeByMetadataName(LinqExtensionsMetadataName);
				var tableType      = startContext.Compilation.GetTypeByMetadataName(ITableMetadataName);

				if (linqExtensions is null || tableType is null)
					return;

				var analyzer = new CompilationAnalyzer(linqExtensions, tableType);

				startContext.RegisterOperationAction(analyzer.AnalyzeInvocation, OperationKind.Invocation);
			});
		}

		/// <summary>What a hint call applies to; selects the helper naming pattern and the constants that can match.</summary>
		enum HintScope
		{
			Table,
			TablesInScope,
			Query,
		}

		/// <summary>One typed helper that emits a given hint text.</summary>
		sealed class TypedHint
		{
			public TypedHint(string provider, string displayName, string ns, IMethodSymbol helper, INamedTypeSymbol receiverInterface)
			{
				Provider          = provider;
				DisplayName       = displayName;
				Namespace         = ns;
				Helper            = helper;
				ReceiverInterface = receiverInterface;
			}

			public string           Provider          { get; }
			public string           DisplayName       { get; }
			public string           Namespace         { get; }
			public IMethodSymbol    Helper            { get; }
			public INamedTypeSymbol ReceiverInterface { get; }

			/// <summary>The <c>As*</c> extension that yields <see cref="ReceiverInterface"/>; null when the provider has none.</summary>
			public IMethodSymbol?   AsMethod          { get; set; }
		}

		/// <summary>The typed helpers of every provider in the referenced linq2db, keyed by scope and normalised hint text.</summary>
		sealed class HintCatalog
		{
			readonly Dictionary<string, List<TypedHint>> _byKey = new(StringComparer.Ordinal);

			// Provider-specific receiver interfaces (table and query forms) -> provider key.
			readonly Dictionary<INamedTypeSymbol, string> _providerInterfaces = new(SymbolEqualityComparer.Default);

			public static string Normalize(string hint)
			{
				return hint.Trim().ToUpperInvariant();
			}

			static string Key(HintScope scope, string normalizedHint)
			{
				return ((int)scope).ToString(CultureInfo.InvariantCulture) + "|" + normalizedHint;
			}

			public IReadOnlyList<TypedHint>? Find(HintScope scope, string hint)
			{
				return _byKey.TryGetValue(Key(scope, Normalize(hint)), out var list) ? list : null;
			}

			public string? ProviderOf(ITypeSymbol type)
			{
				if (type is INamedTypeSymbol named && _providerInterfaces.TryGetValue(named.OriginalDefinition, out var provider))
					return provider;

				foreach (var iface in type.AllInterfaces)
					if (_providerInterfaces.TryGetValue(iface.OriginalDefinition, out provider))
						return provider;

				return null;
			}

			public static HintCatalog Build(IAssemblySymbol linq2db, INamedTypeSymbol tableType)
			{
				var catalog = new HintCatalog();

				var dataProvider = FindNamespace(linq2db.GlobalNamespace, "LinqToDB", "DataProvider");

				if (dataProvider is null)
					return catalog;

				foreach (var providerNamespace in dataProvider.GetNamespaceMembers())
				{
					var provider    = providerNamespace.Name;
					var displayName = GetDisplayName(provider);
					var ns          = providerNamespace.ToDisplayString();

					var asMethods = CollectAsMethods(providerNamespace);

					foreach (var hintsType in providerNamespace.GetTypeMembers())
					{
						if (!hintsType.IsStatic || hintsType.DeclaredAccessibility != Accessibility.Public || !hintsType.Name.EndsWith("Hints", StringComparison.Ordinal))
							continue;

						var helpers = CollectHelpers(hintsType);

						foreach (var constantClass in hintsType.GetTypeMembers())
						{
							if (constantClass.DeclaredAccessibility != Accessibility.Public)
								continue;

							var scopes = GetScopes(constantClass.Name);

							if (scopes.Length == 0)
								continue;

							foreach (var member in constantClass.GetMembers())
							{
								if (member is not IFieldSymbol { IsConst: true, DeclaredAccessibility: Accessibility.Public, ConstantValue: string value } field)
									continue;

								foreach (var scope in scopes)
								{
									var helper = FindHelper(helpers, scope, field.Name, tableType);

									if (helper is null)
										continue;

									var receiver = ((INamedTypeSymbol)helper.Parameters[0].Type).OriginalDefinition;

									catalog._providerInterfaces[receiver] = provider;

									var typed = new TypedHint(provider, displayName, ns, helper, receiver);

									if (asMethods.TryGetValue(receiver, out var asMethod))
										typed.AsMethod = asMethod;

									catalog.Add(scope, value, typed);
								}
							}
						}
					}

					// Every provider-specific interface an As* method yields counts as that provider's receiver, even
					// one no typed helper takes: a hint on such a receiver is already scoped to that provider.
					foreach (var pair in asMethods)
						if (!catalog._providerInterfaces.ContainsKey(pair.Key))
							catalog._providerInterfaces[pair.Key] = provider;
				}

				// A stable order whatever the namespace enumeration order: the message and the fix list read the same
				// in every build.
				foreach (var list in catalog._byKey.Values)
					list.Sort(static (x, y) => string.CompareOrdinal(x.DisplayName, y.DisplayName));

				return catalog;
			}

			void Add(HintScope scope, string value, TypedHint typed)
			{
				var key = Key(scope, Normalize(value));

				if (!_byKey.TryGetValue(key, out var list))
				{
					list = new List<TypedHint>();
					_byKey.Add(key, list);
				}

				// Aliased constants (MySQL Bka / BatchedKeyAccess, Oracle UseNL / UseNestedLoop) map one text to several
				// helpers of one provider; the first in declaration order stands for the provider.
				foreach (var existing in list)
					if (string.Equals(existing.Provider, typed.Provider, StringComparison.Ordinal))
						return;

				list.Add(typed);
			}

			static INamespaceSymbol? FindNamespace(INamespaceSymbol root, params string[] path)
			{
				var current = root;

				foreach (var part in path)
				{
					INamespaceSymbol? next = null;

					foreach (var child in current.GetNamespaceMembers())
					{
						if (string.Equals(child.Name, part, StringComparison.Ordinal))
						{
							next = child;
							break;
						}
					}

					if (next is null)
						return null;

					current = next;
				}

				return current;
			}

			static HintScope[] GetScopes(string constantClassName)
			{
				return constantClassName switch
				{
					"Table" => new[] { HintScope.Table, HintScope.TablesInScope },
					"Query" => new[] { HintScope.Query },
					"Hint"  => new[] { HintScope.Table, HintScope.TablesInScope, HintScope.Query },
					_       => Array.Empty<HintScope>(),
				};
			}

			static Dictionary<string, List<IMethodSymbol>> CollectHelpers(INamedTypeSymbol hintsType)
			{
				var helpers = new Dictionary<string, List<IMethodSymbol>>(StringComparer.Ordinal);

				foreach (var member in hintsType.GetMembers())
				{
					// A typed helper is a public generic extension with the receiver as its only parameter.
					if (member is not IMethodSymbol { IsExtensionMethod: true, DeclaredAccessibility: Accessibility.Public, Arity: 1 } method
						|| method.Parameters.Length != 1
						|| method.Parameters[0].Type is not INamedTypeSymbol { TypeKind: TypeKind.Interface })
						continue;

					if (!helpers.TryGetValue(method.Name, out var list))
					{
						list = new List<IMethodSymbol>();
						helpers.Add(method.Name, list);
					}

					list.Add(method);
				}

				return helpers;
			}

			static IMethodSymbol? FindHelper(Dictionary<string, List<IMethodSymbol>> helpers, HintScope scope, string name, INamedTypeSymbol tableType)
			{
				return scope switch
				{
					HintScope.Table         => Pick(helpers, tableType, wantTable: true,  "With" + name, name + "Hint"),
					HintScope.TablesInScope => Pick(helpers, tableType, wantTable: false, "With" + name + "InScope", name + "InScopeHint"),
					_                       => Pick(helpers, tableType, wantTable: false, "Option" + name, name + "Hint"),
				};
			}

			static IMethodSymbol? Pick(Dictionary<string, List<IMethodSymbol>> helpers, INamedTypeSymbol tableType, bool wantTable, params string[] names)
			{
				foreach (var name in names)
				{
					if (!helpers.TryGetValue(name, out var list))
						continue;

					foreach (var method in list)
						if (IsTableInterface(method.Parameters[0].Type, tableType) == wantTable)
							return method;
				}

				return null;
			}

			static Dictionary<INamedTypeSymbol, IMethodSymbol> CollectAsMethods(INamespaceSymbol providerNamespace)
			{
				var result = new Dictionary<INamedTypeSymbol, IMethodSymbol>(SymbolEqualityComparer.Default);

				foreach (var type in providerNamespace.GetTypeMembers())
				{
					if (!type.IsStatic || type.DeclaredAccessibility != Accessibility.Public)
						continue;

					foreach (var member in type.GetMembers())
					{
						if (member is IMethodSymbol { IsExtensionMethod: true, DeclaredAccessibility: Accessibility.Public, Arity: 1 } method
							&& method.Name.StartsWith("As", StringComparison.Ordinal)
							&& method.Parameters.Length == 1
							&& method.ReturnType is INamedTypeSymbol { TypeKind: TypeKind.Interface } returnType
							&& SymbolEqualityComparer.Default.Equals(returnType.ContainingNamespace, providerNamespace)
							&& !result.ContainsKey(returnType.OriginalDefinition))
						{
							result.Add(returnType.OriginalDefinition, method);
						}
					}
				}

				return result;
			}

			static string GetDisplayName(string provider)
			{
				return provider switch
				{
					"SqlServer" => "SQL Server",
					"SqlCe"     => "SQL Server CE",
					"MySql"     => "MySQL",
					"Ydb"       => "YDB",
					_           => provider,
				};
			}
		}

		static bool IsTableInterface(ITypeSymbol type, INamedTypeSymbol tableType)
		{
			return Implements(type, tableType);
		}

		// The type is the generic interface definition, a construction of it, or implements one.
		static bool Implements(ITypeSymbol type, INamedTypeSymbol interfaceDefinition)
		{
			if (type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, interfaceDefinition))
				return true;

			foreach (var iface in type.AllInterfaces)
				if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, interfaceDefinition))
					return true;

			return false;
		}

		/// <summary>
		/// Per-compilation state. Kept off the analyzer instance (RS1008). The catalog is built on the first hint call
		/// that reaches it, so a compilation without one pays only the name checks.
		/// </summary>
		sealed class CompilationAnalyzer
		{
			readonly INamedTypeSymbol  _linqExtensions;
			readonly INamedTypeSymbol  _tableType;
			readonly Lazy<HintCatalog> _catalog;

			public CompilationAnalyzer(INamedTypeSymbol linqExtensions, INamedTypeSymbol tableType)
			{
				_linqExtensions = linqExtensions;
				_tableType      = tableType;
				_catalog        = new Lazy<HintCatalog>(() => HintCatalog.Build(linqExtensions.ContainingAssembly, tableType), LazyThreadSafetyMode.ExecutionAndPublication);
			}

			public void AnalyzeInvocation(OperationAnalysisContext context)
			{
				var invocation = (IInvocationOperation)context.Operation;
				var method     = invocation.TargetMethod;

				HintScope scope;

				switch (method.Name)
				{
					case TableHintName        :
					case WithName             : scope = HintScope.Table;         break;
					case TablesInScopeHintName: scope = HintScope.TablesInScope; break;
					case QueryHintName        : scope = HintScope.Query;         break;
					default                   : return;
				}

				var definition = method.ReducedFrom ?? method;

				// Only linq2db's own (receiver, string) overloads: the generic ones on LinqExtensions and the provider
				// ones on a *Hints type. Parameterised overloads carry arguments no typed helper without parameters has.
				if (!definition.IsExtensionMethod
					|| definition.Parameters.Length != 2
					|| definition.Parameters[1].Type.SpecialType != SpecialType.System_String
					|| !SymbolEqualityComparer.Default.Equals(definition.ContainingAssembly, _linqExtensions.ContainingAssembly))
					return;

				var isGenericOverload = SymbolEqualityComparer.Default.Equals(definition.ContainingType, _linqExtensions);

				if (!isGenericOverload && !definition.ContainingType.Name.EndsWith("Hints", StringComparison.Ordinal))
					return;

				IOperation? receiverValue = null;
				string?     hint          = null;

				foreach (var argument in invocation.Arguments)
				{
					if (argument.Parameter is null)
						continue;

					if (argument.Parameter.Ordinal == 0)
						receiverValue = argument.Value;
					else if (argument.Parameter.Ordinal == 1 && argument.Value.ConstantValue is { HasValue: true, Value: string text })
						hint = text;
				}

				if (hint is null || receiverValue is null)
					return;

				var candidates = _catalog.Value.Find(scope, hint);

				if (candidates is null)
					return;

				// The receiver as written, before the implicit conversion to the overload's parameter type: that is what
				// says whether the query is already scoped to a provider (AsSqlServer().With("NOLOCK") binds the generic
				// With through ITable<T>).
				while (receiverValue is IConversionOperation { IsImplicit: true } conversion)
					receiverValue = conversion.Operand;

				var receiverType = receiverValue.Type;

				if (receiverType is null)
					return;

				var receiverProvider = _catalog.Value.ProviderOf(receiverType);
				var properties       = ImmutableDictionary.CreateBuilder<string, string?>(StringComparer.Ordinal);
				var equivalents      = new StringBuilder();
				var count            = 0;

				foreach (var candidate in candidates)
				{
					if (receiverProvider is not null && !string.Equals(receiverProvider, candidate.Provider, StringComparison.Ordinal))
						continue;

					var needsAs = !Implements(receiverType, candidate.ReceiverInterface);

					if (needsAs && candidate.AsMethod is null)
						continue;

					// AsXxx() on a table binds the ITable overload and yields the provider's table type, which the query
					// form of a helper does not take; going through AsQueryable() first selects the IQueryable overload.
					var needsAsQueryable = needsAs
						&& !IsTableInterface(candidate.ReceiverInterface, _tableType)
						&& IsTableInterface(receiverType, _tableType);

					var prefix = "Fix" + count.ToString(CultureInfo.InvariantCulture);

					properties[prefix + FixProviderKey]    = candidate.DisplayName;
					properties[prefix + FixNamespaceKey]   = candidate.Namespace;
					properties[prefix + FixAsMethodKey]    = needsAs ? candidate.AsMethod!.Name : string.Empty;
					properties[prefix + FixAsQueryableKey] = needsAsQueryable ? "true" : "false";
					properties[prefix + FixHelperKey]      = candidate.Helper.Name;

					if (count > 0)
						equivalents.Append(", ");

					if (needsAsQueryable)
						equivalents.Append("AsQueryable().");

					if (needsAs)
						equivalents.Append(candidate.AsMethod!.Name).Append("().");

					equivalents.Append(candidate.Helper.Name).Append("()");

					if (receiverProvider is null)
						equivalents.Append(" for ").Append(candidate.DisplayName);

					count++;
				}

				if (count == 0)
					return;

				properties[FixCountKey]        = count.ToString(CultureInfo.InvariantCulture);
				properties[GenericReceiverKey] = receiverProvider is null ? "true" : "false";

				context.ReportDiagnostic(Diagnostic.Create(
					Rule,
					GetLocation(invocation),
					properties.ToImmutable(),
					hint.Trim(),
					equivalents.ToString()));
			}

			// From the method name to the end of the argument list: the hint call alone, not the chain in front of it.
			static Location GetLocation(IInvocationOperation invocation)
			{
				if (invocation.Syntax is InvocationExpressionSyntax syntax)
				{
					SyntaxNode? name = syntax.Expression switch
					{
						MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
						MemberBindingExpressionSyntax binding     => binding.Name,
						_                                         => null,
					};

					if (name is not null)
						return Location.Create(syntax.SyntaxTree, Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(name.SpanStart, syntax.Span.End));
				}

				return invocation.Syntax.GetLocation();
			}
		}
	}
}
