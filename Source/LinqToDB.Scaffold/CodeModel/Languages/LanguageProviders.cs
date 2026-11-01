namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Provides access to built-in language providers.
	/// </summary>
	public static class LanguageProviders
	{
		/// <summary>
		/// C# language provider.
		/// </summary>
		public static ILanguageProvider CSharp => CSharpLanguageProvider.Instance;

		/// <summary>
		/// F# language provider. Generated code uses records and option-typed nullable columns, so the consuming
		/// application must enable F# support with <c>DataOptions.UseFSharp()</c> (linq2db.FSharp package).
		/// </summary>
		public static ILanguageProvider FSharp => FSharpLanguageProvider.Instance;

		// TODO: add VB.NET support
	}
}
