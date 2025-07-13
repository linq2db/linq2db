namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Defines query visitor behavior of <see cref="QueryElementVisitor"/> visitor implementation when visiting query element.
	/// </summary>
	public enum VisitMode
	{
		/// <summary>
		/// Read-only element inspection that shouldn't modify or replace AST.
		/// </summary>
		ReadOnly,
		/// <summary>
		/// Element inspection with potential in-place modification.
		/// </summary>
		Modify,
		/// <summary>
		/// Element inspection with potential generation of new element.
		/// </summary>
		Transform
	}

}
