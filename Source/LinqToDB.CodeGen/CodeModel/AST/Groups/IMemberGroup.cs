namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Marker interface for member groups.
	/// </summary>
	public interface IMemberGroup : ICodeElement
	{
		/// <summary>
		/// Empty group flag.
		/// </summary>
		bool IsEmpty { get; }
	}
}
