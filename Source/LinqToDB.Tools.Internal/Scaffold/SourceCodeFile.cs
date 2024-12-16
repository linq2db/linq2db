namespace LinqToDB.Scaffold
{
	/// <summary>
	/// Single-file source code with file name.
	/// </summary>
	/// <param name="FileName">File name (with extension; without path).</param>
	/// <param name="Code">Source code.</param>
	public record SourceCodeFile(string FileName, string Code);
}
