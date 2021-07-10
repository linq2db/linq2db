using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CollectAllNamespacesVisitor : NoopCodeModelVisitor
	{
		public HashSet<CodeIdentifier[]> Namespaces { get; }

		public CollectAllNamespacesVisitor(ILanguageServices languageServices)
		{
			Namespaces = new HashSet<CodeIdentifier[]>(new NamespaceEqualityComparer(languageServices.GetNameComparer()));
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

			if (type.Namespace != null)
				Namespaces.Add(type.Namespace);
		}

		protected override void Visit(CodeElementNamespace @namespace)
		{
			Namespaces.Add(@namespace.Name);
			base.Visit(@namespace);
		}
	}
}
