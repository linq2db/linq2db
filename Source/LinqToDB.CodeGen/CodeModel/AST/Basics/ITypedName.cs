namespace LinqToDB.CodeGen.Model
{
	public interface ITypedName
	{
		/// <summary>
		/// Name.
		/// </summary>
		CodeIdentifier Name { get; }
		/// <summary>
		/// Type.
		/// </summary>
		CodeTypeToken  Type { get; }
	}
}
