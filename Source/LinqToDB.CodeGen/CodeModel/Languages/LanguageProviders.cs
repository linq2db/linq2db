namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Provides access to built-in language providers.
	/// </summary>
	public static class LanguageProviders
	{
		public static readonly ILanguageProvider CSharp = new CSharpLanguageProvider();

		// TODO: add F# and VB.NET support
	}
}
