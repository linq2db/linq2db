namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Provides access to built-in language providers.
	/// </summary>
	public static class LanguageProviders
	{
		public static ILanguageProvider CSharp => CSharpLanguageProvider.Instance;

		public static ILanguageProvider FSharp => FSharpLanguageProvider.Instance;

		// TODO: add VB.NET support
	}
}
