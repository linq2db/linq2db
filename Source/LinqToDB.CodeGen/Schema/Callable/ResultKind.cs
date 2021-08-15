namespace LinqToDB.CodeGen.Schema
{
	/// <summary>
	/// Type of function result.
	/// </summary>
	public enum ResultKind
	{
		/// <summary>
		/// Function returns no value.
		/// </summary>
		Void,
		/// <summary>
		/// Function could return different types of result.
		/// </summary>
		Dynamic,
		/// <summary>
		/// Function returns tuple object.
		/// </summary>
		Tuple,
		/// <summary>
		/// Function returns scalar value.
		/// </summary>
		Scalar
	}
}
