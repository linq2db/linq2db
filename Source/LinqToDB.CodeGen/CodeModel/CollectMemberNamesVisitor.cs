using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CollectMemberNamesVisitor : NoopCodeModelVisitor
	{
		public ISet<string> MemberNames { get; }
		public ISet<string> MemberNamesWithOwner { get; }

		private readonly ILanguageServices _langServices;

		private readonly ICodeElement _root;

		public CollectMemberNamesVisitor(ILanguageServices langServices, ICodeElement root)
		{
			_langServices = langServices;
			_root = root;

			MemberNames = langServices.GetUniqueNameCollection();
			MemberNamesWithOwner = langServices.GetUniqueNameCollection();
		}

		protected override void Visit(CodeMethod method)
		{
			MemberNames.Add(method.Name.Name);
			MemberNamesWithOwner.Add(method.Name.Name);
			
			base.Visit(method);
		}

		protected override void Visit(CodeProperty property)
		{
			MemberNames.Add(property.Name.Name);
			MemberNamesWithOwner.Add(property.Name.Name);
			base.Visit(property);
		}

		protected override void Visit(CodeElementNamespace @namespace)
		{
			var name = @namespace.Name[@namespace.Name.Length - 1].Name;
			if (_root == @namespace)
			{
				MemberNamesWithOwner.Add(name);
				base.Visit(@namespace);
			}
			else
			{
				MemberNames.Add(name);
				MemberNamesWithOwner.Add(name);
				// don't walk inside of nested type
			}
		}

		protected override void Visit(CodeClass @class)
		{
			if (_root == @class)
			{
				MemberNamesWithOwner.Add(@class.Name.Name);
				base.Visit(@class);
			}
			else
			{
				MemberNames.Add(@class.Name.Name);
				MemberNamesWithOwner.Add(@class.Name.Name);
				// don't walk inside of nested type
			}
		}

		protected override void Visit(CodeField field)
		{
			MemberNames.Add(field.Name.Name);
			MemberNamesWithOwner.Add(field.Name.Name);
			base.Visit(field);
		}
	}
}
