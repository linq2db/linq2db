namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Represents code element, that have type and name (e.g. class field).
	/// </summary>
	public interface ITypedName
	{
		/// <summary>
		/// Element name.
		/// </summary>
		CodeIdentifier Name { get; }
		/// <summary>
		/// Element type.
		/// </summary>
		CodeTypeToken  Type { get; }
	}
}
