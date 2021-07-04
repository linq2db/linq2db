using System;
using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.CodeGen.ContextModel
{
	public class ColumnModel
	{
		public bool IsIdentity { get; set; }

		public bool CanInsert { get; set; }
		public bool CanUpdate { get; set; }

		public string ColumnName { get; set; } = null!;
		public string? Description { get; set; }

		public DbType Type { get; set; } = null!;
		public DataType DataType { get; set; }

		public bool IsPrimaryKey { get; set; }
		public int? PrimaryKeyOrdinal { get; set; }

		public string PropertyName { get; set; } = null!;

		// TODO: temp
		public Type CLRType { get; set; } = null!;
		public string? ProviderType { get; set; }
	}
}
