namespace LinqToDB.CodeModel
{
	// technically it is not parameter attribute, but parameter type[ + custom attribute], but we don't need such details
	/// <summary>
	/// Parameter direction.
	/// </summary>
	public enum CodeParameterDirection
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
