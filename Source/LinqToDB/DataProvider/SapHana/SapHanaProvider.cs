namespace LinqToDB.DataProvider.SapHana
{
	/// <summary>
	/// SAP HANA 2 ADO.NET provider.
	/// </summary>
	public enum SapHanaProvider
	{
		/// <summary>
		/// Detect provider automatically.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// Unmanaged provider from SAP (Sap.Data.Hana or Sap.Data.Hana.Core).
		/// </summary>
		Unmanaged,
		/// <summary>
		/// ODBC HDBODBC/HDBODBC32 provider.
		/// </summary>
		ODBC
	}
}
