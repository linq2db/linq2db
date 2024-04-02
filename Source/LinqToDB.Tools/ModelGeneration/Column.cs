using System;
using System.Data;

namespace LinqToDB.Tools.ModelGeneration
{
	public interface IColumn : IProperty
	{
		public string?    ColumnName         { get; set; } // Column name in database
		public bool       IsNullable         { get; set; }
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

	public class Column<T> : Property<T>, IColumn
		where T : Column<T>, new()
	{
		public Column()
		{
		}

		public Column(ModelType type)
		{
			ModelType   = type;
			TypeBuilder = () => ModelType.ToTypeName();
		}

		public string? ColumnName         { get; set; } // Column name in database
		public bool    IsNullable         { get; set; }
		public bool    IsIdentity         { get; set; }
		public string? ColumnType         { get; set; } // Type of the column in database
		public string? DataType           { get; set; }
		public int?    Length             { get; set; }
		public int?    Precision          { get; set; }
		public int?    Scale              { get; set; }
		public DbType  DbType             { get; set; }
		public string? Description        { get; set; }
		public bool    IsPrimaryKey       { get; set; }
		public int     PrimaryKeyOrder    { get; set; }
		public bool    SkipOnUpdate       { get; set; }
		public bool    SkipOnInsert       { get; set; }
		public bool    IsDuplicateOrEmpty { get; set; }
		public bool    IsDiscriminator    { get; set; }
		public string? AliasName          { get; set; }

		public string? MemberName
		{
			get => Name;
			set => Name = value;
		}

		public ModelType? ModelType { get; set; }

		public override bool EnforceNotNullable => ModelGenerator.EnableNullableReferenceTypes && ModelGenerator.EnforceModelNullability && ModelType is { IsReference: true, IsNullable: false };
	}
}
