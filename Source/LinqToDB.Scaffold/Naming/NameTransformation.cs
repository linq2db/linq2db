namespace LinqToDB.Naming
{
	/// <summary>
	/// Defines name transformation modes.
	/// </summary>
	public enum NameTransformation
	{
		/// <summary>
		/// No transformations applied.
		/// </summary>
		None,
		/// <summary>
		/// Split name into words using underscore as word separator.
		/// E.g.: SOME_NAME -> [SOME, NAME].
		/// </summary>
		SplitByUnderscore,
		// used by DataModelLoader.GenerateAssociationName() method
		/// <summary>
		/// Association name generation strategy, similar to T4 templates implementation. Includes <see cref="SplitByUnderscore"/> behavior.
		/// </summary>
		Association,
	}
}
