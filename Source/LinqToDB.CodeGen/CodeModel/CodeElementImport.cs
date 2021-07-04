namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeElementImport : ITopLevelCodeElement
	{
		public CodeElementImport(CodeIdentifier[] parts)
		{
			Parts = parts;
		}

		public CodeIdentifier[] Parts { get; }

		public CodeElementType ElementType => CodeElementType.Import;
	}
}
