using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.Model
{
	public class ReturnParameter
	{
		public ReturnParameter(ParameterModel parameter, DatabaseType type)
		{
			Parameter = parameter;
			Type = type;
		}

		public ParameterModel Parameter { get; set; }

		public DatabaseType Type { get; set; }

		public DataType? DataType { get; set; }

		public string? Name { get; set; }

		public bool IsNullable { get; set; }
	}
}
