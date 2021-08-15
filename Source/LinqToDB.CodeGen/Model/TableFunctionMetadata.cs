using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.CodeGeneration
{
	public class TableFunctionMetadata
	{
		public TableFunctionMetadata()
		{
		}

		public TableFunctionMetadata(ObjectName name)
		{
			Name = name;
		}

		public ObjectName? Name { get; set; }
		public string? Configuration { get; set; }
		public int[]? ArgIndices { get; set; }
	}
}
