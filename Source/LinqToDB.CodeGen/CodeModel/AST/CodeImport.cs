namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Import (using) statement.
	/// </summary>
	public class CodeImport : ITopLevelElement
	{
		public CodeImport(CodeIdentifier[] @namespace)
		{
			Namespace = @namespace;
		}

		/// <summary>
		/// Imported namespace.
		/// </summary>
		public CodeIdentifier[] Namespace { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Import;
	}
}
