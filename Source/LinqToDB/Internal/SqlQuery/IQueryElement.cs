namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Sql AST node interface.
	/// </summary>
	public interface IQueryElement
	{
#if DEBUG
		string DebugText { get; }
#endif
		/// <summary>
		/// AST node type.
		/// </summary>
		QueryElementType       ElementType { get; }
		/// <summary>
		/// Generates debug text representation of AST node.
		/// </summary>
		QueryElementTextWriter ToString(QueryElementTextWriter writer);

		int GetElementHashCode();
	}
}
