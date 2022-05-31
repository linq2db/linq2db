namespace LinqToDB.DataProvider.Oracle
{
	/// <summary>
	/// Lists supported Oracle ADO.NET providers.
	/// </summary>
	public enum OracleProvider
	{
		/// <summary>
		/// Oracle.ManagedDataAccess and Oracle.ManagedDataAccess.Core providers.
		/// </summary>
		Managed,
		/// <summary>
		/// Oracle.DataAccess legacy native provider for .NET Framework (ODP.NET).
		/// </summary>
		Native,
		/// <summary>
		/// Devart.Data.Oracle provider.
		/// </summary>
		Devart
	}
}
