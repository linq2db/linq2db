namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Marker interface for expression nodes.
	/// </summary>
	public interface ICodeExpression : ICodeElement
	{
		/// <summary>
		/// Expression type.
		/// </summary>
		IType Type { get; }
	}
}
