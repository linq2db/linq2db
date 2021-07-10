namespace LinqToDB.CodeGen.CodeModel
{
	public abstract class CodeTypeBase : AttributeOwner, ITopLevelCodeElement, IMembersOwner
	{
		public MemberAttributes Attributes { get; set; }

		public CodeXmlComment? XmlDoc { get; set; }

		public abstract IType Type { get; }
	}

}
