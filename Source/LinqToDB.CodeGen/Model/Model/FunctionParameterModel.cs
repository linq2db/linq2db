using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.Model
{
	public class FunctionParameterModel
	{
		public FunctionParameterModel(ParameterModel parameter)
		{
			Parameter = parameter;
		}

		public ParameterModel Parameter { get; set; }

		public string? DbName { get; set; }

		public DatabaseType? Type { get; set; }
		public DataType? DataType { get; set; }
		public bool IsNullable { get; set; }
	}
}
