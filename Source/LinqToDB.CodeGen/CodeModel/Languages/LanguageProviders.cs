namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Provides access to built-in language providers.
	/// </summary>
	public static class LanguageProviders
	{
		public static ILanguageProvider CSharp => CSharpLanguageProvider.Instance;

		// TODO: add F# and VB.NET support
	}
}
