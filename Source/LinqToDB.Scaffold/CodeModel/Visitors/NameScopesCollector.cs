using System.Linq;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using LinqToDB.Common;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// This visitor inspects code model and collect two collections:
	/// <list type="bullet">
	/// <item>list of namespaces for each type name. In other words all namespaces that have type with specific name.</item>
	/// <item>list of all names inside specific scope (namespace or full type name) like nested types, namespaces or type members like methods, properties or fields.</item>
	/// </list>
	/// Those collections used to detect and resolve name conflicts.
	/// </summary>
	internal sealed class NameScopesCollector : NoopCodeModelVisitor
	{
		private readonly ILanguageProvider _languageProvider;
		private readonly ISet<IType>       _visitedTypes;

		// contains all detected scopes with type, namespace and member names, defined in them
		// for root scope use empty collection as key
		private readonly Dictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> _nameScopes;
		// contains all detected scopes with type and namespace names, defined in them
		// for root scope use empty collection as key
		private readonly Dictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> _typeNameScopes;
		// contains all namespaces for specific type names
		// key: type name
		// value: all namespaces that contain type with such name
		private readonly Dictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> _typesNamespaces;

		// currently inspected scope
		private IEnumerable<CodeIdentifier> _currentScope;

		public NameScopesCollector(ILanguageProvider languageProvider)
		{
			_languageProvider = languageProvider;
			_nameScopes       = new (_languageProvider.FullNameEqualityComparer);
			_typeNameScopes   = new (_languageProvider.FullNameEqualityComparer);
			_typesNamespaces  = new (_languageProvider.IdentifierEqualityComparer);
			_visitedTypes     = new HashSet<IType>(_languageProvider.TypeEqualityComparerWithoutNRT);

			SetNewScope([]);
		}

		/// <summary>
		/// Returns all identified naming scopes with names (type, namespace, member names), defined directly in those scopes.
		/// </summary>
		public Dictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>>          ScopesWithNames     => _nameScopes;
		/// <summary>
		/// Returns all identified naming scopes with type and namespace names, defined directly in those scopes.
		/// </summary>
		public Dictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>>          ScopesWithTypeNames => _typeNameScopes;
		/// <summary>
		/// Returns all identified type names with list of namespaces for each type name, where type with such name
		/// declared.
		/// </summary>
		public IReadOnlyDictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> TypesNamespaces     => _typesNamespaces;

		#region visitors
		protected override void Visit(CodeMethod method)
		{
			AddNameToCurrentScope(method.Name, false);
			base.Visit(method);
		}

		protected override void Visit(CodeProperty property)
		{
			AddNameToCurrentScope(property.Name, false);
			base.Visit(property);
		}

		protected override void Visit(CodeNamespace @namespace)
		{
			var oldScope = _currentScope;

			foreach (var name in @namespace.Name)
			{
				AddNameToCurrentScope(name, true);
				SetNewScope(CombineWithCurrentScope(name));
			}

			base.Visit(@namespace);

			_currentScope = oldScope;
		}

		protected override void Visit(CodeClass @class)
		{
			var current = _currentScope;

			AddNameToCurrentScope(@class.Name, true);
			RegisterType(@class.Type);

			SetNewScope(CombineWithCurrentScope(@class.Name));

			base.Visit(@class);

			_currentScope = current;
		}

		protected override void Visit(CodeField field)
		{
			AddNameToCurrentScope(field.Name, false);
			base.Visit(field);
		}

		protected override void Visit(CodeTypeReference type)
		{
			RegisterType(type.Type);
			base.Visit(type);
		}

		protected override void Visit(CodeTypeToken type)
		{
			RegisterType(type.Type);
			base.Visit(type);
		}
		#endregion

		/// <summary>
		/// Combine identifier with current naming scope and return new naming scope.
		/// </summary>
		/// <param name="name">Identifier to append.</param>
		/// <returns>New naming scope.</returns>
		private CodeIdentifier[] CombineWithCurrentScope(CodeIdentifier name)
		{
			var newScope = new CodeIdentifier[_currentScope.Count() + 1];
			newScope[newScope.Length - 1] = name;
			var idx = 0;
			foreach (var n in _currentScope)
			{
				newScope[idx] = n;
				idx++;
			}

			return newScope;
		}

		/// <summary>
		/// Sets provided naming scope as current scope.
		/// </summary>
		/// <param name="newScope">New naming scope.</param>
		[MemberNotNull(nameof(_currentScope))]
		private void SetNewScope(IEnumerable<CodeIdentifier> newScope)
		{
			if (!_nameScopes.ContainsKey(newScope))
				_nameScopes.Add(newScope, new HashSet<CodeIdentifier>(_languageProvider.IdentifierEqualityComparer));
			if (!_typeNameScopes.ContainsKey(newScope))
				_typeNameScopes.Add(newScope, new HashSet<CodeIdentifier>(_languageProvider.IdentifierEqualityComparer));

			_currentScope = newScope;
		}

		/// <summary>
		/// Adds name to currently inspected scope.
		/// </summary>
		/// <param name="name">Name to add to scope.</param>
		/// <param name="typeOrNamespaceName">Name is type or namespace name.</param>
		private void AddNameToCurrentScope(CodeIdentifier name, bool typeOrNamespaceName)
		{
			_nameScopes[_currentScope].Add(name);

			if (typeOrNamespaceName)
				_typeNameScopes[_currentScope].Add(name);
		}

		/// <summary>
		/// Register scope for provided type by type name.
		/// </summary>
		/// <param name="type">Type descriptor.</param>
		private void RegisterType(IType type)
		{
			// register only named types
			if ((type.Kind   == TypeKind.Regular
				|| type.Kind == TypeKind.Generic
				|| type.Kind == TypeKind.OpenGeneric)
				&& type.Parent == null)
			{
				var ns = type.Namespace ?? [];

				if (!_typesNamespaces.TryGetValue(type.Name!, out var namespaces))
					_typesNamespaces.Add(type.Name!, namespaces = new HashSet<IEnumerable<CodeIdentifier>>(_languageProvider.FullNameEqualityComparer));

				namespaces.Add(ns);
			}

			RegisterExternalType(type);
		}

		/// <summary>
		/// Register external types in name scopes.
		/// </summary>
		/// <param name="type">Type to add to scopes.</param>
		private void RegisterExternalType(IType type)
		{
			if (!_visitedTypes.Add(type))
				return;

			var oldScope  = _currentScope;

			switch (type.Kind)
			{
				case TypeKind.Array       :
					RegisterExternalType(type.ArrayElementType!);
					break;
				case TypeKind.OpenGeneric :
				case TypeKind.Regular     :
				case TypeKind.Generic     :
					if (type.External)
					{
						var scope = new List<CodeIdentifier>();
						GetTypeScope(scope, type);
						SetNewScope(scope);
						AddNameToCurrentScope(type.Name!, true);

						while (scope.Count > 0)
						{
							var name = scope[scope.Count - 1];
							scope.RemoveAt(scope.Count - 1);
							SetNewScope(scope);
							AddNameToCurrentScope(name, true);
						}
					}

					if (type is GenericType generic)
					{
						foreach (var typeArg in generic.TypeArguments)
							RegisterExternalType(typeArg);
					}

					break;
				case TypeKind.Dynamic     :
				case TypeKind.TypeArgument:
				default                   :
					if (type.External)
						throw new InvalidOperationException($"Unsupported external type kind: {type.Kind}");

					break;
			}

			_currentScope = oldScope;
		}

		private void GetTypeScope(List<CodeIdentifier> scope, IType type)
		{
			if (type.Parent != null)
			{
				scope.Insert(0, type.Parent.Name!);
				GetTypeScope(scope, type.Parent);
			}
			else if (type.Namespace != null)
			{
				scope.InsertRange(0, type.Namespace);
			}
		}
	}
}
