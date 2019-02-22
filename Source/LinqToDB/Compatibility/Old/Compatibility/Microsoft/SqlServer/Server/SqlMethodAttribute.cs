#region Assembly System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Data.dll
#endregion

using System;

namespace Microsoft.SqlServer.Server
{
	//
	// Summary:
	//     Indicates the determinism and data access properties of a method or property
	//     on a user-defined type (UDT). The properties on the attribute reflect the physical
	//     characteristics that are used when the type is registered with SQL Server.
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class SqlMethodAttribute : SqlFunctionAttribute
	{
		//
		// Summary:
		//     An attribute on a user-defined type (UDT), used to indicate the determinism and
		//     data access properties of a method or a property on a UDT.
		public SqlMethodAttribute() { }

		//
		// Summary:
		//     Indicates whether SQL Server should invoke the method on null instances.
		//
		// Returns:
		//     true if SQL Server should invoke the method on null instances; otherwise false.
		//     If the method cannot be invoked (because of an attribute on the method), the
		//     SQL Server DbNull is returned.
		public bool InvokeIfReceiverIsNull { get; set; }
		//
		// Summary:
		//     Indicates whether a method on a user-defined type (UDT) is a mutator.
		//
		// Returns:
		//     true if the method is a mutator; otherwise false.
		public bool IsMutator { get; set; }
		//
		// Summary:
		//     Indicates whether the method on a user-defined type (UDT) is called when null
		//     input arguments are specified in the method invocation.
		//
		// Returns:
		//     true if the method is called when null input arguments are specified in the method
		//     invocation; false if the method returns a null value when any of its input parameters
		//     are null. If the method cannot be invoked (because of an attribute on the method),
		//     the SQL Server DbNull is returned.
		public bool OnNullCall { get; set; }
	}
}