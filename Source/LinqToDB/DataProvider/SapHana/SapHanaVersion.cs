namespace LinqToDB.DataProvider.SapHana
{
	/// <summary>
	/// SQL Dialect version.
	/// </summary>
	public enum SapHanaVersion
	{
		/// <summary>
		/// Base SQL dialect, matching SAP HANA 1.
		/// </summary>
		SapHana1,
		/// <summary>
		/// SQL dialect with new features from SAP HANA 2 SPS 04.
		/// </summary>
		SapHana2sps04,
	}
}
