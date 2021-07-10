using System;
using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.CodeGen.ContextModel
{
	public class ParameterModel
	{
		public string DbName { get; set; } = null!;
		public string ParameterName { get; set; } = null!;
		public string? Description { get; set; } = null!;
		public DbType Type { get; set; } = null!;
		public DataType DataType { get; set; }
		public Type CLRType { get; set; } = null!;
		public string? ProviderType { get; set; }


		public ParameterDirection Direction { get; set; }
	}
}
