using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class TypeCollectorVisitor : NoopCodeModelVisitor
	{
		private readonly ILanguageServices _langServices;

		public ISet<IType> ExternalTypes { get; }
		public ISet<IType> LocalTypes { get; }

		public TypeCollectorVisitor(ILanguageServices langServices)
		{
			_langServices = langServices;

			ExternalTypes = new HashSet<IType>(new TypeNameComparer(langServices.GetNameComparer()));
			LocalTypes = new HashSet<IType>(new TypeNameComparer(langServices.GetNameComparer()));
		}

		protected override void Visit(TypeReference type)
		{
			VisitType(type.Type);
		}

		protected override void Visit(TypeToken type)
		{
			VisitType(type.Type);
		}

		private void VisitType(IType type)
		{
			if (type.Parent != null)
				VisitType(type.Parent);

			switch (type.Kind)
			{
				case TypeKind.Generic:
					if (type.External)
						ExternalTypes.Add(type);
					else
						LocalTypes.Add(type);

					foreach (var typeArg in type.TypeArguments!)
						VisitType(typeArg);
					break;
				case TypeKind.OpenGeneric:
				case TypeKind.Regular:
					if (type.External)
						ExternalTypes.Add(type);
					else
						LocalTypes.Add(type);
					break;
					// TODO: we don't need to handle other types for now
			}
		}
	}
}
