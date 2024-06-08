namespace LinqToDB.CommandLine
{
	/// <summary>
	/// CLI return codes.
	/// </summary>
	internal static class StatusCodes
	{
		/// <summary>
		/// Command executed successfully (must be 0, as it is default success code for CLI interfaces).
		/// </summary>
		public const int SUCCESS           = 0;
		/// <summary>
		/// Invalid arguments.
		/// </summary>
		public const int INVALID_ARGUMENTS = -1;
		/// <summary>
		/// Command failed with unhadnled exception.
		/// </summary>
		public const int INTERNAL_ERROR    = -2;
		/// <summary>
		/// Command failed with expected error.
		/// </summary>
		public const int EXPECTED_ERROR    = -3;
		/// <summary>
		/// T4 template processing error.
		/// </summary>
		public const int T4_ERROR          = -4;
	}
}
