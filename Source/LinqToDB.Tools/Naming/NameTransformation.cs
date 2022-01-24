namespace LinqToDB.Naming
{
	/// <summary>
	/// Defines name transformation modes.
	/// </summary>
	public enum NameTransformation
	{
		/// <summary>
		/// Split name into words using underscore as word separator.
		/// E.g.: SOME_NAME -> [SOME, NAME].
		/// </summary>
		SplitByUnderscore,
		// used by DataModelLoader.GenerateAssociationName() method
		/// <summary>
		/// Association name generation strategy, inherited from T4 templates.
		/// </summary>
		T4Compat,
	}
}
