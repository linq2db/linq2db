namespace LinqToDB.CodeGen.CodeModel
{
	public class ClassGroup : MemberGroup<CodeClass>
	{
		private readonly CodeClass? _class;
		private readonly CodeElementNamespace? _namespace;

		public ClassGroup(CodeClass @class)
		{
			_class = @class;
		}

		public ClassGroup(CodeElementNamespace? @namespace)
		{
			_namespace = @namespace;
		}

		public override CodeElementType ElementType => CodeElementType.ClassGroup;

		public ClassBuilder New(CodeIdentifier name)
		{
			var @class = _class != null ? new CodeClass(_class, name) : new CodeClass(_namespace?.Name, name);
			Members.Add(@class);
			return new ClassBuilder(@class);
		}
	}
}
