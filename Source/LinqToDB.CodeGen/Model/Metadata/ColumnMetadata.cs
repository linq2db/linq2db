using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.Metadata
{
	public class ColumnMetadata
	{
		public ColumnMetadata(string name)
		{
			Name = name;
			IsColumn = true;
		}

		// set by framework
		public string? Name { get; set; }

		public DatabaseType? DbType { get; set; }
		public DataType? DataType { get; set; }
		public bool CanBeNull { get; set; }

		public bool SkipOnInsert { get; set; }
		public bool SkipOnUpdate { get; set; }
		
		public bool IsIdentity { get; set; }
		
		public bool IsPrimaryKey { get; set; }
		public int? PrimaryKeyOrder { get; set; }

		// additional metadata, that could be set by user
		public string? Configuration { get; set; }
		public string? MemberName { get; set; }
		public string? Storage { get; set; }
		public string? CreateFormat { get; set; }
		public bool IsColumn { get; set; }
		public bool IsDiscriminator { get; set; }
		public bool SkipOnEntityFetch { get; set; }
		public int? Order { get; set; }
	}
}
