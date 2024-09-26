namespace Tests
{
	public static class TestCategory
	{
		/// <summary>
		/// Tests in this category ignored for CI (Azure) run.
		/// </summary>
		public const string SkipCI = "SkipCI";

		/// <summary>
		/// Create test database tests.
		/// </summary>
		public const string Create = "Create";

		/// <summary>
		/// Free-text search tests.
		/// </summary>
		public const string FTS = "FreeText";
	}
}
