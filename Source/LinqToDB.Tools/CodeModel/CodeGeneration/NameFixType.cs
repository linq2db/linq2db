namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Specify available options for name fix logic.
	/// </summary>
	public enum NameFixType
	{
		/// <summary>
		/// Replace invalid name with provided fixer.
		/// </summary>
		Replace,
		/// <summary>
		/// Replace invalid name with provided fixer and append position value to it.
		/// </summary>
		ReplaceWithPosition,
		/// <summary>
		/// Append provided fixer to invalid name.
		/// </summary>
		Suffix,
		/// <summary>
		/// Append provided fixer to invalid name and append position value afterwards.
		/// </summary>
		SuffixWithPosition
	}
}
