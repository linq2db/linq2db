using System;
using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.CodeGen.ContextModel
{
	public class ResultTableColumnModel
	{
		public string ColumnName { get; set; } = null!;
		public DbType Type { get; set; } = null!;
		public DataType DataType { get; set; }

		public string PropertyName { get; set; } = null!;

		// TODO: temp
		public Type CLRType { get; set; } = null!;
		public string? ProviderType { get; set; }
	}

}
