namespace LinqToDB.CodeGen.CodeModel
{
	public abstract class CodeElementBase : ICodeElement
	{
		public abstract CodeElementType ElementType { get; }
	}
}
