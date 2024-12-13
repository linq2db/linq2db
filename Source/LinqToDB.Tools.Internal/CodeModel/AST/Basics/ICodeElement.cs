namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Base interface, implemented by all AST nodes.
	/// </summary>
	public interface ICodeElement
	{
		/// <summary>
		/// Type of node.
		/// </summary>
		CodeElementType ElementType { get; }
	}
}
