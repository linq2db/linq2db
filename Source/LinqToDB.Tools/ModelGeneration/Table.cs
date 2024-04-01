using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	using SchemaProvider;

	public interface ITable : IClass
	{
		TableSchema? TableSchema             { get; set; }
		string?      Schema                  { get; set; }
		string?      TableName               { get; set; }
		string?      DataContextPropertyName { get; set; }
		MemberBase?  DataContextProperty     { get; set; }
		bool         IsView                  { get; set; }
		bool         IsProviderSpecific      { get; set; }
		bool         IsDefaultSchema         { get; set; }
		string?      Description             { get; set; }
		string?      AliasPropertyName       { get; set; }
		string?      AliasTypeName           { get; set; }
		string?      TypePrefix              { get; set; }
		string?      TypeName                { get; set; }

		Dictionary<string,IColumn>     Columns     { get; set; }
		Dictionary<string,IForeignKey> ForeignKeys { get; set; }
	}

	public class Table<T> : Class<T>, ITable
		where T : Table<T>, new()
	{
		public TableSchema? TableSchema             { get; set; }
		public string?      Schema                  { get; set; }
		public string?      TableName               { get; set; }
		public string?      DataContextPropertyName { get; set; }
		public MemberBase?  DataContextProperty     { get; set; }
		public bool         IsView                  { get; set; }
		public bool         IsProviderSpecific      { get; set; }
		public bool         IsDefaultSchema         { get; set; }
		public string?      Description             { get; set; }
		public string?      AliasPropertyName       { get; set; }
		public string?      AliasTypeName           { get; set; }
		public string?      TypePrefix              { get; set; }

		public string? TypeName
		{
			get => Name;
			set => Name = value;
		}

		public Dictionary<string,IColumn>     Columns     { get; set; } = new();
		public Dictionary<string,IForeignKey> ForeignKeys { get; set; } = new();
	}
}
