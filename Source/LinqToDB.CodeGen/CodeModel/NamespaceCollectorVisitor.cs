using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class NamespaceCollectorVisitor : NoopCodeModelVisitor
	{
		public HashSet<CodeIdentifier[]> Namespaces { get; }
		private HashSet<CodeIdentifier[]> _ignoredNamespaces;

		public NamespaceCollectorVisitor(ILanguageServices languageServices)
		{
			Namespaces = new HashSet<CodeIdentifier[]>(new NamespaceEqualityComparer(languageServices.GetNameComparer()));
			_ignoredNamespaces = new HashSet<CodeIdentifier[]>(new NamespaceEqualityComparer(languageServices.GetNameComparer()));
		}

		protected override void Visit(TypeReference type)
		{
			CollectNamespaces(type.Type);

			base.Visit(type);
		}

		protected override void Visit(TypeToken type)
		{
			CollectNamespaces(type.Type);

			base.Visit(type);
		}

		private  void CollectNamespaces(IType type)
		{
			if (type.Parent != null)
			{
				CollectNamespaces(type.Parent);
				return;
			}

			if (type.Kind == TypeKind.Generic)
			{
				foreach (var typeArg in type.TypeArguments!)
					CollectNamespaces(typeArg);
			}

			// skip aliased types
			if (type.IsAlias)
				return;

			if (type.Namespace != null && !_ignoredNamespaces.Contains(type.Namespace))
				Namespaces.Add(type.Namespace);
		}

		protected override void Visit(CodeElementNamespace @namespace)
		{
			var newNamespaces = new List<CodeIdentifier[]>();

			var name = (CodeIdentifier[])@namespace.Name.Clone();
			while (name.Length > 0)
			{
				if (_ignoredNamespaces.Add(name))
					newNamespaces.Add(name);
				Array.Resize(ref name, name.Length - 1);
			}

			base.Visit(@namespace);

			foreach (var ns in newNamespaces)
			{
				_ignoredNamespaces.Remove(ns);
			}
		}
	}
}
