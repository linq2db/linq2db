namespace LinqToDB.CodeGen.CodeModel
{
	public class NamespaceBuilder
	{
		public NamespaceBuilder(CodeElementNamespace @namespace)
		{
			Namespace = @namespace;
		}

		public CodeElementNamespace Namespace { get; }

		public ClassGroup Classes()
		{
			var group = new ClassGroup(Namespace);
			Namespace.Members.Add(group);
			return group;
		}
	}
}
