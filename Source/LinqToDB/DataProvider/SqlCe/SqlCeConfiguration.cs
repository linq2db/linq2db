namespace LinqToDB.DataProvider.SqlCe
{
	public static class SqlCeConfiguration
	{
		/// <summary>
		/// Enables force inlining of function parameters to support SQL CE 3.0.
		/// Default value: <c>false</c>.
		/// </summary>
		public static bool InlineFunctionParameters = false;
	}
}
