namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Provides access to built-in language providers.
	/// </summary>
	public static class LanguageProviders
	{
		public static ILanguageProvider CSharp => CSharpLanguageProvider.Instance;

		// TODO: add F# and VB.NET support
		// F# request: https://github.com/linq2db/linq2db/issues/1553
	}
}
