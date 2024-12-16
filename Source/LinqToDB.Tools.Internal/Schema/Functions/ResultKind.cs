namespace LinqToDB.Schema
{
	/// <summary>
	/// Type of scalar result for function or procedure.
	/// </summary>
	public enum ResultKind
	{
		/// <summary>
		/// Function returns no value.
		/// </summary>
		Void,
		/// <summary>
		/// Function returns tuple object.
		/// </summary>
		Tuple,
		/// <summary>
		/// Function returns scalar value.
		/// </summary>
		Scalar,

		// not supported by schema providers right now
		///// <summary>
		///// Function could return different types of result.
		///// </summary>
		//Dynamic,
	}
}
