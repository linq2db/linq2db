namespace LinqToDB.CodeGen.CodeModel
{
	public class ConstructorGroup : MemberGroup<CodeConstructor>
	{
		private readonly CodeClass _class;
		public ConstructorGroup(CodeClass @class)
		{
			_class = @class;
		}

		public override CodeElementType ElementType => CodeElementType.ConstructorGroup;

		public ConstructorBuilder New()
		{
			var ctor = new CodeConstructor(_class);
			Members.Add(ctor);
			return new ConstructorBuilder(ctor);
		}
	}
}
