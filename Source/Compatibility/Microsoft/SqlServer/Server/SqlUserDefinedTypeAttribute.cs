#region Assembly System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Data.dll
#endregion

using System;

namespace Microsoft.SqlServer.Server
{
	//
	// Summary:
	//     Used to mark a type definition in an assembly as a user-defined type (UDT) in
	//     SQL Server. The properties on the attribute reflect the physical characteristics
	//     used when the type is registered with SQL Server. This class cannot be inherited.
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public sealed class SqlUserDefinedTypeAttribute : Attribute
	{
		//
		// Summary:
		//     A required attribute on a user-defined type (UDT), used to confirm that the given
		//     type is a UDT and to indicate the storage format of the UDT.
		//
		// Parameters:
		//   format:
		//     One of the Microsoft.SqlServer.Server.Format values representing the serialization
		//     format of the type.
		public SqlUserDefinedTypeAttribute(Format format) { }

		//
		// Summary:
		//     The serialization format as a Microsoft.SqlServer.Server.Format.
		//
		// Returns:
		//     A Microsoft.SqlServer.Server.Format value representing the serialization format.
		public Format Format { get; }
		//
		// Summary:
		//     Indicates whether the user-defined type is byte ordered.
		//
		// Returns:
		//     true if the user-defined type is byte ordered; otherwise false.
		public bool IsByteOrdered { get; set; }
		//
		// Summary:
		//     Indicates whether all instances of this user-defined type are the same length.
		//
		// Returns:
		//     true if all instances of this type are the same length; otherwise false.
		public bool IsFixedLength { get; set; }
		//
		// Summary:
		//     The maximum size of the instance, in bytes.
		//
		// Returns:
		//     An System.Int32 value representing the maximum size of the instance.
		public int MaxByteSize { get; set; }
		//
		// Summary:
		//     The SQL Server name of the user-defined type.
		//
		// Returns:
		//     A System.String value representing the SQL Server name of the user-defined type.
		public string Name { get; set; }
		//
		// Summary:
		//     The name of the method used to validate instances of the user-defined type.
		//
		// Returns:
		//     A System.String representing the name of the method used to validate instances
		//     of the user-defined type.
		public string ValidationMethodName { get; set; }
	}
}