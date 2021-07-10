namespace LinqToDB.CodeGen.CodeModel
{
	public class TypeReference : ICodeExpression
	{
		public TypeReference(IType type)
		{
			Type = type;
		}

		public IType Type { get; }

		public CodeElementType ElementType => CodeElementType.TypeReference;
	}
}
