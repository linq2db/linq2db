namespace LinqToDB.CodeGen.CodeGeneration
{
	public interface ILanguageFeatures
	{
		string? MissingXmlCommentWarnCode { get; }
		bool SupportsNRT { get; }
	}
}
