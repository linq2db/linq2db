namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Group of methods.
	/// </summary>
	public class MethodGroup : MemberGroup<CodeMethod>
	{
		public MethodGroup(bool tableLayout)
		{
			TableLayout = tableLayout;
		}

		/// <summary>
		/// Prefered group rendering layout: as-is or table layout.
		/// </summary>
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
