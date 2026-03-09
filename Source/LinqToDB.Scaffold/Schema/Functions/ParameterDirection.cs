namespace LinqToDB.Schema
{
	/// <summary>
	/// Function or procedure parameter direction.
	/// Usually applies to procedures only as most of databases support only input parameters for functions.
	/// </summary>
	public enum ParameterDirection
	{
		/// <summary>
		/// Input (IN) parameter.
		/// </summary>
		Input,
		/// <summary>
		/// Output (OUT) parameter.
		/// </summary>
		Output,
		/// <summary>
		/// Input/output (IN OUT) parameter.
		/// </summary>
		InputOutput,
	}
}
