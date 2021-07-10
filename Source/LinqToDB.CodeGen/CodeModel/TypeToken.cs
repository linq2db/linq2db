namespace LinqToDB.CodeGen.CodeModel
{
	public class TypeToken : ICodeElement
	{
		public TypeToken(IType type)
		{
			Type = type;
		}

		public IType Type { get; }

		public CodeElementType ElementType => CodeElementType.Type;
	}
}
