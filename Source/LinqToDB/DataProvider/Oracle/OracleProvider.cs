namespace LinqToDB.DataProvider.Oracle
{
	/// <summary>
	/// Lists supported Oracle ADO.NET providers.
	/// </summary>
	public enum OracleProvider
	{
		/// <summary>
		/// Automatically detect provider.
		/// First we try to locate <see cref="Managed"/> provider, then <see cref="Devart"/> and otherwise <see cref="Native"/> provider used.
		/// </summary>
		AutoDetect,
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
		Devart,
	}
}
