using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// This visitor collects imports for all types, used in code model.
	/// </summary>
	internal sealed class ImportsCollector : NoopCodeModelVisitor
	{
		private readonly ILanguageProvider                      _languageProvider;
		private readonly HashSet<IReadOnlyList<CodeIdentifier>> _imports;
		private readonly HashSet<IReadOnlyList<CodeIdentifier>> _ignoredImports;

		public ImportsCollector(ILanguageProvider languageProvider)
		{
			_languageProvider = languageProvider;
			_imports          = new (languageProvider.FullNameEqualityComparer);
			_ignoredImports   = new (languageProvider.FullNameEqualityComparer);
		}

		/// <summary>
		/// Gets all discovered imports.
		/// </summary>
		public IReadOnlyCollection<IReadOnlyList<CodeIdentifier>> Imports => _imports;

		/// <summary>
		/// Reset visitor state.
		/// </summary>
		public void Reset()
		{
			_imports.Clear();
			// actually should be already empty here
			_ignoredImports.Clear();
		}

#region visitors
		protected override void Visit(CodeTypeReference type)
		{
			CollectTypeImports(type.Type);
			base.Visit(type);
		}

		protected override void Visit(CodeTypeToken type)
		{
			CollectTypeImports(type.Type);
			base.Visit(type);
		}

		protected override void Visit(CodeLambda method)
		{
			VisitList(method.CustomAttributes);

			if (!method.CanOmmitTypes)
				VisitList(method.Parameters);

			if (method.Body != null)
				VisitList(method.Body);
		}

		protected override void Visit(CodeReference reference)
		{
		}

		protected override void Visit(CodeNamespace @namespace)
		{
			var newNamespaces = new List<CodeIdentifier[]>();

			// create namespace array copy and add each sub-namespace to list of ignored imports
			// namespaces ignored as imports only for types, used inside current namespace
			var name = @namespace.Name.ToArray();
			while (name.Length > 0)
			{
				if (_ignoredImports.Add(name))
					newNamespaces.Add(name);
				Array.Resize(ref name, name.Length - 1);
			}

			base.Visit(@namespace);

			// clean ignore list from current namespace ignores
			foreach (var ns in newNamespaces)
				_ignoredImports.Remove(ns);
		}
#endregion

		/// <summary>
		/// Collect all namespaces from type descriptor.
		/// </summary>
		/// <param name="type">Type descriptor to inspect.</param>
		private void CollectTypeImports(IType type)
		{
			// skip aliased types
			if (_languageProvider.GetAlias(type) != null)
				return;

			if (type.Kind == TypeKind.Array)
			{
				CollectTypeImports(type.ArrayElementType!);
				return;
			}

			if (type.Parent != null)
			{
				CollectTypeImports(type.Parent);
				return;
			}

			if (type.Kind == TypeKind.Generic)
			{
				foreach (var typeArg in type.TypeArguments!)
					CollectTypeImports(typeArg);
			}

			if (type.Namespace != null && !_ignoredImports.Contains(type.Namespace))
				_imports.Add(type.Namespace);
		}
	}
}
