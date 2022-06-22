namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Binary operation type.
	/// List of operations limited to those we currently use for code generation and could be extended in future.
	/// </summary>
	public enum BinaryOperation
	{
		/// <summary>
		/// Equality (==).
		/// </summary>
		Equal,
		/// <summary>
		/// Logical AND (&amp;&amp;).
		/// </summary>
		And,
		/// <summary>
		/// Addition (+).
		/// </summary>
		Add,
	}
}
