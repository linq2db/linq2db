namespace LinqToDB.CodeGen.Model
{
	public sealed class CodeExternalPropertyOrField : CodeTypedName, ICodeElement
	{
		public CodeExternalPropertyOrField(CodeIdentifier name, CodeTypeToken type)
			: base(name, type)
		{
		}

		CodeElementType ICodeElement.ElementType => CodeElementType.ExternalPropertyOrField;
	}
}
