namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Compiler pragma directive type.
	/// List of pragmas limited to one we currently support.
	/// </summary>
	public enum PragmaType
	{
		/// <summary>
		/// Disable warning(s) directive.
		/// </summary>
		DisableWarning,
		/// <summary>
		/// Enable NRT context.
		/// </summary>
		NullableEnable,
		/// <summary>
		/// Compilation error pragma.
		/// </summary>
		Error,
	}
}
