namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// Defines supported YDB ADO.NET provider implementation libraries.
	/// </summary>
	public enum YdbProvider
	{
		/// <summary>
		/// Detect provider automatically.
		/// </summary>
		AutoDetect,

		/// <summary>
		/// Official YDB .NET SDK provider
		/// (<c>Ydb.Sdk</c>nbsp;package, see https://github.com/ydb-platform/ydb-dotnet-sdk).
		/// </summary>
		YdbSdk
	}
}
