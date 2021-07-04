using System;
using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.CodeGen.ContextModel
{
	public class TypeModel
	{
		public DbType Type { get; set; } = null!;
		public DataType DataType { get; set; }
		public Type CLRType { get; set; } = null!;
		public string? ProviderType { get; set; }
	}
}
