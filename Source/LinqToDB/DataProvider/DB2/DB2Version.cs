namespace LinqToDB.DataProvider.DB2
{
	/// <summary>
	/// DB2 server type.
	/// Note that for IMB i (DB2 iSeries) you should use 3rd-party provider linq2db4iSeries
	/// <see href="https://www.nuget.org/packages/linq2db4iSeries"/>.
	/// </summary>
	public enum DB2Version
	{
		/// <summary>
		/// Automatically detect server version by asking server for it's type.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// DB2 LUW (aka Db2) server.
		/// </summary>
		LUW,
		/// <summary>
		/// DB2 for z/OS server.
		/// </summary>
		zOS,
	}
}
