namespace LinqToDB.CodeGen.CodeModel
{
	public class MethodGroup : MemberGroup<CodeMethod>
	{
		public MethodGroup(bool tableLayout)
		{
			TableLayout = tableLayout;
		}

		public bool TableLayout { get; }

		public override CodeElementType ElementType => CodeElementType.MethodGroup;

		public MethodBuilder New(CodeIdentifier name)
		{
			var method = new CodeMethod(name);
			Members.Add(method);
			return new MethodBuilder(method);
		}
	}
}
