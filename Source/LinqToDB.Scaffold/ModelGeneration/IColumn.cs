using System.Data;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IColumn : IProperty
	{
		public string?    ColumnName         { get; set; } // Column name in database
		public bool       IsIdentity         { get; set; }
		public string?    ColumnType         { get; set; } // Type of the column in database
		public string?    DataType           { get; set; }
		public int?       Length             { get; set; }
		public int?       Precision          { get; set; }
		public int?       Scale              { get; set; }
		public DbType     DbType             { get; set; }
		public string?    Description        { get; set; }
		public bool       IsPrimaryKey       { get; set; }
		public int        PrimaryKeyOrder    { get; set; }
		public bool       SkipOnUpdate       { get; set; }
		public bool       SkipOnInsert       { get; set; }
		public bool       IsDuplicateOrEmpty { get; set; }
		public bool       IsDiscriminator    { get; set; }
		public string?    AliasName          { get; set; }
		public string?    MemberName         { get; set; }
		public ModelType? ModelType          { get; set; }
	}
}
