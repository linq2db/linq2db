using System;

namespace LinqToDB.SchemaProvider
{
	/// <summary>
	/// Describes table column.
	/// </summary>
	public class ColumnSchema
	{
		/// <summary>
		/// Gets column name.
		/// </summary>
		public string      ColumnName           { get; set; }
		/// <summary>
		/// Gets db-specific column type.
		/// </summary>
		public string      ColumnType           { get; set; }
		/// <summary>
		/// Gets flag indicating that it is nullable column.
		/// </summary>
		public bool        IsNullable           { get; set; }
		/// <summary>
		/// Gets flag indicating that it is identity column.
		/// </summary>
		public bool        IsIdentity           { get; set; }
		/// <summary>
		/// Gets flag indicating that column is a part of primary key.
		/// </summary>
		public bool        IsPrimaryKey         { get; set; }
		/// <summary>
		/// Gets position of column in composite primary key.
		/// </summary>
		public int         PrimaryKeyOrder      { get; set; }
		/// <summary>
		/// Gets column description.
		/// </summary>
		public string      Description          { get; set; }

		/// <summary>
		/// Gets C# friendly column name.
		/// </summary>
		public string      MemberName           { get; set; }
		/// <summary>
		/// Gets .net column type as a string.
		/// </summary>
		public string      MemberType           { get; set; }
		/// <summary>
		/// Gets provider-specific .net column type as a string.
		/// </summary>
		public string      ProviderSpecificType { get; set; }
		/// <summary>
		/// Gets .net column type.
		/// </summary>
		public Type        SystemType           { get; set; }
		/// <summary>
		/// Gets column type as <see cref="DataType"/> enumeration value.
		/// </summary>
		public DataType    DataType             { get; set; }
		/// <summary>
		/// Gets flag indicating that insert operations without explicit column setter should ignore this column.
		/// </summary>
		public bool        SkipOnInsert         { get; set; }
		/// <summary>
		/// Gets flag indicating that update operations without explicit column setter should ignore this column.
		/// </summary>
		public bool        SkipOnUpdate         { get; set; }
		/// <summary>
		/// Gets column type length.
		/// </summary>
		public long?       Length               { get; set; }
		/// <summary>
		/// Gets column type precision.
		/// </summary>
		public int?        Precision            { get; set; }
		/// <summary>
		/// Gets column type scale.
		/// </summary>
		public int?        Scale                { get; set; }

		/// <summary>
		/// Gets column owner schema.
		/// </summary>
		public TableSchema Table;
	}
}
