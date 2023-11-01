namespace LinqToDB.SqlQuery
{
	/// <summary>
	/// Defines query visitor behavior of <see cref="QueryElementVisitor"/> visitor implementation.
	/// </summary>
	public enum VisitMode
	{
		/// <summary>
		/// Read-only visitor, which doesn't modify or clone query AST.
		/// </summary>
		ReadOnly,
		/// <summary>
		/// Visitor (may) modify existing AST in-place.
		/// </summary>
		Modify,
		/// <summary>
		/// Read-only visitor, which produce new clone (optionally with modifications) of query AST.
		/// </summary>
		Transform
	}

}
