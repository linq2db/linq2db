namespace LinqToDB.DataProvider.Access
{
	/// <summary>
	/// Access engine version.
	/// </summary>
	public enum AccessVersion
	{
		/// <summary>
		/// Use automatic detection of used engine.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// Legacy JET engine.
		/// </summary>
		Jet,
		/// <summary>
		/// Access ACE engine.
		/// </summary>
		Ace,
	}
}
