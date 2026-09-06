using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace LinqToDB.Analyzers
{
	/// <summary>
	/// Shared detection core for the server-side-only contract rules. Compiled into BOTH Roslyn components:
	/// it belongs to this project and is <c>&lt;Compile Include&gt;</c>-linked by <c>Source/CodeGenerators</c>,
	/// so the internal <c>LINQ2DB0002</c>/<c>LINQ2DB0003</c> and the shipped <c>L2DB1003</c>/<c>L2DB1004</c>
	/// decide identical cases. Keep every behavioural choice here; the hosts hold only descriptors,
	/// registration and (user-facing only) option reads.
	/// </summary>
	/// <remarks>
	/// Constrained by both projects at once: Roslyn 4.8 API surface only (this project pins 4.8.0 while
	/// CodeGenerators takes 5.6.0), no <see cref="ImmutableArray{T}"/> collection expressions, and the
	/// helper types are nested so one file declares one top-level type (MA0048) and one link suffices.
	/// </remarks>
	internal static class ServerSideOnlyContract
	{
		/// <summary>How a member's declaration and its implementation disagree, if they do.</summary>
		internal enum Violation
		{
			None,

			/// <summary>The body is a throw-only stub but nothing declares the member server-side-only.</summary>
			MissingMarker,

			/// <summary>The member is declared server-side-only but its stub throws the wrong exception type.</summary>
			WrongStubException,
		}

		/// <summary>Types the contract rules resolve once per compilation. Absent linq2db means silence.</summary>
		internal sealed class Symbols
		{
			Symbols(
				INamedTypeSymbol serverSideOnlyException,
				INamedTypeSymbol serverSideOnlyAttribute,
				INamedTypeSymbol expressionAttribute,
				INamedTypeSymbol extensionAttribute,
				INamedTypeSymbol tableFunctionAttribute,
				INamedTypeSymbol expressionMethodAttribute)
			{
				ServerSideOnlyException   = serverSideOnlyException;
				ServerSideOnlyAttribute   = serverSideOnlyAttribute;
				ExpressionAttribute       = expressionAttribute;
				ExtensionAttribute        = extensionAttribute;
				TableFunctionAttribute    = tableFunctionAttribute;
				ExpressionMethodAttribute = expressionMethodAttribute;
			}

			public INamedTypeSymbol ServerSideOnlyException   { get; }
			public INamedTypeSymbol ServerSideOnlyAttribute   { get; }
			public INamedTypeSymbol ExpressionAttribute       { get; }
			public INamedTypeSymbol ExtensionAttribute        { get; }
			public INamedTypeSymbol TableFunctionAttribute    { get; }
			public INamedTypeSymbol ExpressionMethodAttribute { get; }

			public static Symbols? TryCreate(Compilation compilation)
			{
				var serverSideOnlyException = compilation.GetTypeByMetadataName("LinqToDB.ServerSideOnlyException");
				var serverSideOnlyAttribute = compilation.GetTypeByMetadataName("LinqToDB.Mapping.ServerSideOnlyAttribute");
				var expressionAttribute     = compilation.GetTypeByMetadataName("LinqToDB.Sql+ExpressionAttribute");
				var extensionAttribute      = compilation.GetTypeByMetadataName("LinqToDB.Sql+ExtensionAttribute");
				var tableFunctionAttribute  = compilation.GetTypeByMetadataName("LinqToDB.Sql+TableFunctionAttribute");
				var expressionMethod        = compilation.GetTypeByMetadataName("LinqToDB.ExpressionMethodAttribute");

				if (serverSideOnlyException is null
					|| serverSideOnlyAttribute is null
					|| expressionAttribute is null
					|| extensionAttribute is null
					|| tableFunctionAttribute is null
					|| expressionMethod is null)
				{
					return null;
				}

				return new Symbols(
					serverSideOnlyException,
					serverSideOnlyAttribute,
					expressionAttribute,
					extensionAttribute,
					tableFunctionAttribute,
					expressionMethod);
			}
		}

		/// <summary>
		/// The effective exception-type sets, already merged with their defaults by the caller. The internal
		/// host passes the hardcoded defaults; the shipped host adds whatever <c>.editorconfig</c> supplied.
		/// </summary>
		internal readonly struct Options
		{
			public Options(
				ImmutableArray<INamedTypeSymbol> unmarkedStubExceptionTypes,
				ImmutableArray<INamedTypeSymbol> allowedStubExceptionTypes)
			{
				UnmarkedStubExceptionTypes = unmarkedStubExceptionTypes;
				AllowedStubExceptionTypes  = allowedStubExceptionTypes;
			}

			/// <summary>Exception types that, thrown by an unmarked stub, evidence server-side-only intent.</summary>
			public ImmutableArray<INamedTypeSymbol> UnmarkedStubExceptionTypes { get; }

			/// <summary>Exception types acceptable inside a stub that IS declared server-side-only.</summary>
			public ImmutableArray<INamedTypeSymbol> AllowedStubExceptionTypes { get; }
		}

		/// <summary>
		/// The cheap half: is this an analyzable member whose whole body is one throw? Split from
		/// <see cref="Classify"/> so a host can gate the expensive work - the shipped analyzer parses
		/// <c>.editorconfig</c> option values, which must not happen on every operation block.
		/// </summary>
		public static bool TryGetStub(
			ISymbol                    owningSymbol,
			ImmutableArray<IOperation> operationBlocks,
			out ISymbol?               member,
			out INamedTypeSymbol?      thrownType)
		{
			thrownType = null;

			if (!TryGetAnalyzableMember(owningSymbol, out member) || member is null)
				return false;

			thrownType = GetStubThrownType(operationBlocks);

			if (thrownType is null)
			{
				member = null;
				return false;
			}

			return true;
		}

		public static Violation Classify(ISymbol member, INamedTypeSymbol thrownType, Symbols symbols, Options options)
		{
			if (DeclaresServerSideOnly(member, symbols))
			{
				return Contains(options.AllowedStubExceptionTypes, thrownType)
					? Violation.None
					: Violation.WrongStubException;
			}

			// Arm A - an attribute already declares the member translatable, so any throwing body is a
			// contract gap. Arm B - nothing declares anything, so the thrown type is the only evidence of
			// intent and only a listed type counts.
			return HasMarkerCapableAttribute(member, symbols) || Contains(options.UnmarkedStubExceptionTypes, thrownType)
				? Violation.MissingMarker
				: Violation.None;
		}

		/// <summary>
		/// RegisterOperationBlockAction also fires for constructors, operators and indexer accessors, where
		/// <c>[ServerSideOnly]</c> cannot be applied (<c>AttributeTargets.Property | Method</c>) and a code
		/// fix would emit uncompilable code. A setter is excluded too: a <c>set =&gt; throw</c> beside a real
		/// getter is not a stub member.
		/// </summary>
		public static bool TryGetAnalyzableMember(ISymbol owningSymbol, out ISymbol? member)
		{
			member = null;

			if (owningSymbol is not IMethodSymbol method)
				return false;

			switch (method.MethodKind)
			{
				case MethodKind.Ordinary:
				case MethodKind.ExplicitInterfaceImplementation:
					member = method;
					return true;

				case MethodKind.PropertyGet:
					if (method.AssociatedSymbol is IPropertySymbol { IsIndexer: false } property)
					{
						member = property;
						return true;
					}

					return false;

				default:
					return false;
			}
		}

		/// <summary>
		/// The thrown exception type when the member's entire body is one <c>throw</c>, else
		/// <see langword="null"/>. Selecting the body block by kind rather than by count matters:
		/// OperationBlocks can also carry attribute operations for an attributed member.
		/// </summary>
		public static INamedTypeSymbol? GetStubThrownType(ImmutableArray<IOperation> operationBlocks)
		{
			IOperation? body = null;

			foreach (var block in operationBlocks)
			{
				// Select the body by kind, never by count: OperationBlocks also carries an entry per
				// attribute on the member, and every member these rules care about is attributed. Both a
				// block-bodied member and an expression-bodied `=> throw` arrive as a Block; Throw and
				// ExpressionStatement are accepted defensively, not because either shape produces them.
				switch (block.Kind)
				{
					case OperationKind.Block:
					case OperationKind.Throw:
					case OperationKind.ExpressionStatement:
						break;

					default:
						continue;
				}

				if (body is not null)
					return null;

				body = block;
			}

			// Descend to the throw rather than pattern-matching one fixed wrapper shape. A block-bodied
			// member and an expression-bodied `=> throw` both arrive as a Block here, but what sits inside
			// it differs by member shape (statement vs. return vs. the throw itself), and getting that
			// wrong silently classifies a real stub as "not a stub". The loop succeeds only if it lands on
			// a throw, so being permissive costs no precision.
			for (var depth = 0; depth < 4 && body is not null; depth++)
			{
				switch (body)
				{
					case IBlockOperation block:
						body = SingleOrNull(block.Operations);
						continue;

					case IExpressionStatementOperation statement:
						body = statement.Operation;
						continue;

					case IReturnOperation @return:
						body = @return.ReturnedValue;
						continue;

					case IConversionOperation conversion:
						body = conversion.Operand;
						continue;
				}

				break;
			}

			if (body is not IThrowOperation { Exception: { } exception })
				return null;

			while (exception is IConversionOperation conversion)
				exception = conversion.Operand;

			return exception is IObjectCreationOperation creation ? creation.Type as INamedTypeSymbol : null;
		}

		/// <summary>
		/// The four marker forms. Forms 1-3 mirror the runtime (<c>MappingExtensions.IsServerSideOnly</c>
		/// plus the TableFunction path in <c>ExpressionTreeOptimizationContext</c>); form 4,
		/// <c>[ExpressionMethod]</c>, is deliberately broader than the runtime because substitution makes the
		/// body unreachable inside a query.
		/// </summary>
		public static bool DeclaresServerSideOnly(ISymbol member, Symbols symbols)
		{
			if (DeclaredOn(member, symbols))
				return true;

			// A stub implementing an interface member inherits the interface's declaration: Sql.GroupBy is
			// typed IGroupBy, so a call binds the interface method and that is where the runtime reads it.
			foreach (var inherited in EnumerateInheritedMembers(member))
				if (DeclaredOn(inherited, symbols))
					return true;

			return false;
		}

		/// <summary>Whether an attribute is present that COULD carry the marker but does not (arm A).</summary>
		public static bool HasMarkerCapableAttribute(ISymbol member, Symbols symbols)
			=> TryFindMarkerCapableAttribute(member, symbols, out _);

		/// <summary>
		/// Arm A's attribute, and - through <see cref="AttributeData.ApplicationSyntaxReference"/> - where it is
		/// written. It can sit on an implemented INTERFACE member rather than on the reported one, because the
		/// runtime reads mapping attributes up the interface chain, so the code fix cannot assume it is on the
		/// member the diagnostic points at, or even in the same file. The analyzer passes this on as an
		/// additional location instead of making the fixer re-derive the walk.
		/// </summary>
		public static bool TryFindMarkerCapableAttribute(ISymbol member, Symbols symbols, out AttributeData? attribute)
		{
			if (TryFindMarkerCapableAttributeOn(member, symbols, out attribute))
				return true;

			foreach (var inherited in EnumerateInheritedMembers(member))
				if (TryFindMarkerCapableAttributeOn(inherited, symbols, out attribute))
					return true;

			attribute = null;
			return false;
		}

		/// <summary>
		/// Where a NEW marker has to be written when nothing declares the member. A stub implementing an
		/// interface member must be marked on the INTERFACE: the call binds the interface method, and the
		/// attribute walk goes up and never back down, so marking the implementation silences the rule while
		/// leaving that call client-evaluable - the shape <c>Sql.GroupBy</c> is. Returns <c>null</c> when the
		/// member implements nothing, which is the ordinary case and where the reported member is itself the
		/// right target.
		/// <para>
		/// Scoped to implemented interface members even though <see cref="DeclaresServerSideOnly"/> also walks
		/// base classes: a call bound to a derived type reads the derived member's own attributes first, so
		/// marking an <c>override</c> in place is already correct.
		/// </para>
		/// </summary>
		public static ISymbol? FindInterfaceMarkerTarget(ISymbol member)
		{
			foreach (var implemented in EnumerateImplementedInterfaceMembers(member))
				return implemented;

			return null;
		}

		// Only Sql.ExpressionAttribute declares ServerSideOnly, so only it can take the set-named-argument
		// remedy - narrower than DeclaredOn's marker set on purpose. A TableFunction-derived attribute is a
		// marker in its own right and unconditionally, so a member carrying one is already declared and never
		// reaches arm A; admitting it here would only offer a fix that emits ServerSideOnly = true on an
		// attribute that has no such property.
		static bool TryFindMarkerCapableAttributeOn(ISymbol member, Symbols symbols, out AttributeData? attribute)
		{
			foreach (var candidate in member.GetAttributes())
			{
				if (DerivesFrom(candidate.AttributeClass, symbols.ExpressionAttribute))
				{
					attribute = candidate;
					return true;
				}
			}

			attribute = null;
			return false;
		}

		static bool DeclaredOn(ISymbol member, Symbols symbols)
		{
			foreach (var attribute in member.GetAttributes())
			{
				var attributeClass = attribute.AttributeClass;

				if (attributeClass is null)
					continue;

				if (DerivesFrom(attributeClass, symbols.ServerSideOnlyAttribute)
					|| DerivesFrom(attributeClass, symbols.ExpressionMethodAttribute)
					|| DerivesFrom(attributeClass, symbols.TableFunctionAttribute))
				{
					return true;
				}

				if (DerivesFrom(attributeClass, symbols.ExpressionAttribute)
					&& IsEffectivelyServerSideOnly(attribute, attributeClass, symbols))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// An explicit <c>ServerSideOnly</c> argument wins; otherwise every <c>Sql.ExtensionAttribute</c>
		/// constructor sets it true, so a bare <c>[Sql.Extension("…")]</c> is marked with nothing written.
		/// </summary>
		static bool IsEffectivelyServerSideOnly(AttributeData attribute, INamedTypeSymbol attributeClass, Symbols symbols)
		{
			foreach (var argument in attribute.NamedArguments)
				if (string.Equals(argument.Key, "ServerSideOnly", StringComparison.Ordinal))
					return argument.Value.Value is bool value && value;

			return DerivesFrom(attributeClass, symbols.ExtensionAttribute);
		}

		/// <summary>
		/// The members the runtime also reads mapping attributes from. <c>MappingAttributesCache</c> walks both
		/// <c>type.GetInterfaces()</c> and <c>type.BaseType</c>, reading each type's own attributes rather than
		/// going through reflection inheritance - so <c>ServerSideOnlyAttribute</c>'s <c>Inherited = false</c>
		/// does not stop a base-class marker being honoured, and this walk has to match or correct code is
		/// reported. Covers <c>override</c>; a <c>new</c>-shadowed member has no symbol link to shadow.
		/// </summary>
		static IEnumerable<ISymbol> EnumerateInheritedMembers(ISymbol member)
		{
			foreach (var implemented in EnumerateImplementedInterfaceMembers(member))
				yield return implemented;

			for (var overridden = OverriddenMember(member); overridden is not null; overridden = OverriddenMember(overridden))
				yield return overridden;

			static ISymbol? OverriddenMember(ISymbol symbol) => symbol switch
			{
				IMethodSymbol method     => method.OverriddenMethod,
				IPropertySymbol property => property.OverriddenProperty,
				_                        => null,
			};
		}

		static IEnumerable<ISymbol> EnumerateImplementedInterfaceMembers(ISymbol member)
		{
			// An explicit implementation's Name is the dotted `I.M`, so the name filter below can never match
			// one - and the rule admits that symbol kind. Read its interface members off the symbol instead,
			// which is both exact and cheaper than the scan.
			switch (member)
			{
				case IMethodSymbol { ExplicitInterfaceImplementations.IsEmpty: false } method:
					foreach (var implementedMethod in method.ExplicitInterfaceImplementations)
						yield return implementedMethod;

					yield break;

				case IPropertySymbol { ExplicitInterfaceImplementations.IsEmpty: false } property:
					foreach (var implementedProperty in property.ExplicitInterfaceImplementations)
						yield return implementedProperty;

					yield break;
			}

			var containingType = member.ContainingType;

			if (containingType is null)
				yield break;

			foreach (var @interface in containingType.AllInterfaces)
			{
				foreach (var interfaceMember in @interface.GetMembers())
				{
					if (!string.Equals(interfaceMember.Name, member.Name, StringComparison.Ordinal))
						continue;

					var implementation = containingType.FindImplementationForInterfaceMember(interfaceMember);

					if (implementation is not null && SymbolEqualityComparer.Default.Equals(implementation, member))
						yield return interfaceMember;
				}
			}
		}

		static IOperation? SingleOrNull(ImmutableArray<IOperation> operations)
		{
			IOperation? single = null;

			foreach (var operation in operations)
			{
				if (single is not null)
					return null;

				single = operation;
			}

			return single;
		}

		static bool Contains(ImmutableArray<INamedTypeSymbol> types, INamedTypeSymbol type)
		{
			if (types.IsDefaultOrEmpty)
				return false;

			foreach (var candidate in types)
				if (SymbolEqualityComparer.Default.Equals(candidate, type))
					return true;

			return false;
		}

		static bool DerivesFrom(INamedTypeSymbol? type, INamedTypeSymbol baseType)
		{
			for (var current = type; current is not null; current = current.BaseType)
				if (SymbolEqualityComparer.Default.Equals(current, baseType))
					return true;

			return false;
		}
	}
}
