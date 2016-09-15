#region Assembly System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Data.dll
#endregion

using System;

namespace Microsoft.SqlServer.Server
{
	//
	// Summary:
	//     Used to mark a method definition of a user-defined aggregate as a function in
	//     SQL Server. The properties on the attribute reflect the physical characteristics
	//     used when the type is registered with SQL Server.
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class SqlFunctionAttribute : Attribute
	{
		//
		// Summary:
		//     An optional attribute on a user-defined aggregate, used to indicate that the
		//     method should be registered in SQL Server as a function. Also used to set the
		//     Microsoft.SqlServer.Server.SqlFunctionAttribute.DataAccess, Microsoft.SqlServer.Server.SqlFunctionAttribute.FillRowMethodName,
		//     Microsoft.SqlServer.Server.SqlFunctionAttribute.IsDeterministic, Microsoft.SqlServer.Server.SqlFunctionAttribute.IsPrecise,
		//     Microsoft.SqlServer.Server.SqlFunctionAttribute.Name, Microsoft.SqlServer.Server.SqlFunctionAttribute.SystemDataAccess,
		//     and Microsoft.SqlServer.Server.SqlFunctionAttribute.TableDefinition properties
		//     of the function attribute.
		public SqlFunctionAttribute() { }

		//
		// Summary:
		//     Indicates whether the function involves access to user data stored in the local
		//     instance of SQL Server.
		//
		// Returns:
		//     Microsoft.SqlServer.Server.DataAccessKind.None: Does not access data. Microsoft.SqlServer.Server.DataAccessKind.Read:
		//     Only reads user data.
		public DataAccessKind DataAccess { get; set; }
		//
		// Summary:
		//     The name of a method in the same class as the table-valued function (TVF) that
		//     is used by the TVF contract.
		//
		// Returns:
		//     A System.String value representing the name of a method used by the TVF contract.
		public string FillRowMethodName { get; set; }
		//
		// Summary:
		//     Indicates whether the user-defined function is deterministic.
		//
		// Returns:
		//     true if the function is deterministic; otherwise false.
		public bool IsDeterministic { get; set; }
		//
		// Summary:
		//     Indicates whether the function involves imprecise computations, such as floating
		//     point operations.
		//
		// Returns:
		//     true if the function involves precise computations; otherwise false.
		public bool IsPrecise { get; set; }
		//
		// Summary:
		//     The name under which the function should be registered in SQL Server.
		//
		// Returns:
		//     A System.String value representing the name under which the function should be
		//     registered.
		public string Name { get; set; }
		//
		// Summary:
		//     Indicates whether the function requires access to data stored in the system catalogs
		//     or virtual system tables of SQL Server.
		//
		// Returns:
		//     Microsoft.SqlServer.Server.DataAccessKind.None: Does not access system data.
		//     Microsoft.SqlServer.Server.DataAccessKind.Read: Only reads system data.
		public SystemDataAccessKind SystemDataAccess { get; set; }
		//
		// Summary:
		//     A string that represents the table definition of the results, if the method is
		//     used as a table-valued function (TVF).
		//
		// Returns:
		//     A System.String value representing the table definition of the results.
		public string TableDefinition { get; set; }
	}
}