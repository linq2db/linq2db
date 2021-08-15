using System.Linq;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// This visitor inspects code model and collect two collections:
	/// <list type="bullet">
	/// <item>list of namespaces for each type name. In other words all namespaces that have type with specific name.</item>
	/// <item>list of all names inside specific scope (namespace or full type name) like nested types, namespaces or type members like methods, properties or fields.</item>
	/// </list>
	/// Those collections used to detect and resolve name conflicts.
	/// </summary>
	public class NameScopesCollector : NoopCodeModelVisitor
	{
		private readonly ILanguageProvider _languageProvider;

		// contains all detected scopes with names, defined in them
		// for root scope use empty collection as key
		private readonly Dictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> _nameScopes;
		// contains all namespaces for specific type names
		// key: type name
		// value: all namespaces that contain type with such name
		private readonly Dictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> _typesNamespaces;

		// currently inspected scope
		private IEnumerable<CodeIdentifier> _currentScope;

		public NameScopesCollector(ILanguageProvider languageProvider)
		{
			_languageProvider  = languageProvider;
			_nameScopes        = new Dictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>>(_languageProvider.FullNameComparer);
			_typesNamespaces   = new Dictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>>(_languageProvider.IdentifierComparer);

			SetNewScope(Array.Empty<CodeIdentifier>());
		}

		/// <summary>
		/// Returns all identified naming scopes with names, defined directly in those scopes.
		/// </summary>
		public Dictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>>          ScopesWithNames => _nameScopes;
		/// <summary>
		/// Returns all identified type names with list of namespaces for each type name, where type with such name
		/// declared.
		/// </summary>
		public IReadOnlyDictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> TypesNamespaces => _typesNamespaces;

		#region visitors
		protected override void Visit(CodeMethod method)
		{
			AddNameToCurrentScope(method.Name);
			base.Visit(method);
		}

		protected override void Visit(CodeProperty property)
		{
			AddNameToCurrentScope(property.Name);
			base.Visit(property);
		}

		protected override void Visit(CodeNamespace @namespace)
		{
			var oldScope = _currentScope;

			foreach (var name in @namespace.Name)
			{
				AddNameToCurrentScope(name);
				SetNewScope(CombineWithCurrentScope(name));
			}

			base.Visit(@namespace);

			_currentScope = oldScope;
		}

		protected override void Visit(CodeClass @class)
		{
			var current = _currentScope;

			AddNameToCurrentScope(@class.Name);
			RegisterType(@class.Type);

			SetNewScope(CombineWithCurrentScope(@class.Name));

			base.Visit(@class);

			_currentScope = current;
		}

		protected override void Visit(CodeField field)
		{
			AddNameToCurrentScope(field.Name);
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
				_nameScopes.Add(newScope, new HashSet<CodeIdentifier>(_languageProvider.IdentifierComparer));

			_currentScope = newScope;
		}

		/// <summary>
		/// Adds name to currently inspected scope.
		/// </summary>
		/// <param name="name">Name to add to scope.</param>
		private void AddNameToCurrentScope(CodeIdentifier name)
		{
			_nameScopes[_currentScope].Add(name);
		}

		/// <summary>
		/// Register scope for provided type by type name.
		/// </summary>
		/// <param name="type">Type descriptor.</param>
		private void RegisterType(IType type)
		{
			// register only named types
			if ((type.Kind == TypeKind.Regular
				|| type.Kind == TypeKind.Generic
				|| type.Kind == TypeKind.OpenGeneric)
				&& type.Parent == null)
			{
				var ns = type.Namespace ?? Array.Empty<CodeIdentifier>();

				if (!_typesNamespaces.TryGetValue(type.Name!, out var namespaces))
					_typesNamespaces.Add(type.Name!, namespaces = new HashSet<IEnumerable<CodeIdentifier>>(_languageProvider.FullNameComparer));

				namespaces.Add(ns);
			}
		}
	}
}
