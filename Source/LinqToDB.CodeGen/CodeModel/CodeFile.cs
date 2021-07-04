namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeFile : CodeElementList<ITopLevelCodeElement>, ICodeElement
	{
		public CodeFile(string fileName, string folder)
		{
			FileName = fileName;
			Folder = folder;
		}

		public string FileName { get; }
		public string Folder { get; }

		public CodeElementType ElementType => CodeElementType.File;
	}
}
