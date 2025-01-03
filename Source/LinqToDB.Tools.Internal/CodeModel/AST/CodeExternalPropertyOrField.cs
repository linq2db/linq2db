namespace LinqToDB.CodeModel
{
	// for now we don't care wether we reference field or property
	/// <summary>
	/// Defines reference to property or field of existing type.
	/// </summary>
	public sealed class CodeExternalPropertyOrField : CodeTypedName, ICodeElement
	{
		public CodeExternalPropertyOrField(CodeIdentifier name, CodeTypeToken type)
			: base(name, type)
		{
		}

		CodeElementType ICodeElement.ElementType => CodeElementType.ExternalPropertyOrField;
	}
}
