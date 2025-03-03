using System.Collections.Generic;

using LinqToDB.SchemaProvider;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
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
}
