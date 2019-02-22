#region Assembly System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Data.dll
#endregion

namespace Microsoft.SqlServer.Server
{
	//
	// Summary:
	//     Describes the type of access to system data for a user-defined method or function.
	public enum SystemDataAccessKind
	{
		//
		// Summary:
		//     The method or function does not access system data.
		None = 0,
		//
		// Summary:
		//     The method or function reads system data.
		Read = 1
	}
}