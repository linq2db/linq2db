namespace LinqToDB.CodeGen.CodeGeneration
{
	public class CSharpLanguageFeatures : ILanguageFeatures
	{
		public string? MissingXmlCommentWarnCode => "1591";
		public bool SupportsNRT => true;
	}
}
