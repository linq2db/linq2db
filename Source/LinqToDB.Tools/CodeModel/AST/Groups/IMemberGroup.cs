namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Non-generic interface for member groups.
	/// </summary>
	public interface IMemberGroup : ICodeElement
	{
		/// <summary>
		/// Empty group flag.
		/// </summary>
		bool IsEmpty { get; }
	}
}
