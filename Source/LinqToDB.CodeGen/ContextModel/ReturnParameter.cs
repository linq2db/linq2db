using System;
using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.CodeGen.ContextModel
{
	public class ReturnParameter
	{
		public DbType Type { get; set; } = null!;
		public Type CLRType { get; set; } = null!;
		public string? ProviderType { get; set; }
		public DataType DataType { get; set; }
		public string ParameterName { get; set; } = null!;
		public string? DbName { get; set; } = null!;
	}
}
