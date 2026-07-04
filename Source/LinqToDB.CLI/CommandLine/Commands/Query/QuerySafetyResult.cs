namespace LinqToDB.CommandLine
{
	internal sealed record QuerySafetyResult(bool IsSafe, string? Error)
	{
		public static QuerySafetyResult Safe { get; } = new(true, null);

		public static QuerySafetyResult Unsafe(string error)
		{
			return new(false, error);
		}
	}
}
