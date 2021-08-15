namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Compiler pragma directive type.
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
		Error
	}
}
