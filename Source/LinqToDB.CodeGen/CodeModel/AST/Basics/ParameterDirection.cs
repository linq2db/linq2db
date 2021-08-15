namespace LinqToDB.CodeGen.Model
{
	// technically it is not parameter attribute, but type[ + custom attribute], but we don't need such details
	/// <summary>
	/// Parameter direction.
	/// </summary>
	public enum ParameterDirection
	{
		/// <summary>
		/// Input parameter.
		/// </summary>
		In,
		/// <summary>
		/// By-ref parameter.
		/// </summary>
		Ref,
		/// <summary>
		/// Output parameter.
		/// </summary>
		Out
	}
}
