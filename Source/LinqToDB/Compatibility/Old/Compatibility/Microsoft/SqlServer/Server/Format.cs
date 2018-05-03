#region Assembly System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Data.dll
#endregion

namespace Microsoft.SqlServer.Server
{
	//
	// Summary:
	//     Used by Microsoft.SqlServer.Server.SqlUserDefinedTypeAttribute and Microsoft.SqlServer.Server.SqlUserDefinedAggregateAttribute
	//     to indicate the serialization format of a user-defined type (UDT) or aggregate.
	public enum Format
	{
		//
		// Summary:
		//     The serialization format is unknown.
		Unknown = 0,
		//
		// Summary:
		//     The Native serialization format uses a very simple algorithm that enables SQL
		//     Server to store an efficient representation of the UDT on disk. Types marked
		//     for Native serialization can only have value types (structs in Microsoft Visual
		//     C# and structures in Microsoft Visual Basic .NET) as members. Members of reference
		//     types (such as classes in Visual C# and Visual Basic), either user-defined or
		//     those existing in the framework (such as System.String), are not supported.
		Native = 1,
		//
		// Summary:
		//     The UserDefined serialization format gives the developer full control over the
		//     binary format through the Microsoft.SqlServer.Server.IBinarySerialize.Write and
		//     Microsoft.SqlServer.Server.IBinarySerialize.Read methods.
		UserDefined = 2
	}
}