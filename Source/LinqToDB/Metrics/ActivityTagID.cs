using System.Data.Common;

namespace LinqToDB.Metrics
{
	/// <summary>
	/// Activity Tag ID.
	/// </summary>
	public enum ActivityTagID
	{
		None = 0,
		/// <summary>Database configuration name (connection string name).</summary>
		ConfigurationString,
		/// <summary>Data Provider name.</summary>
		DataProviderName,
		/// <summary>The name of the database server to which to connect.</summary>
		/// <remarks>Tag value taken from <see cref="DbConnection.DataSource"/> property and could contain incorrect data if provider doesn't fill it properly.</remarks>
		DataSourceName,
		/// <summary>The name of the current database.</summary>
		/// <remarks>Tag value taken from <see cref="DbConnection.Database"/> property and could contain incorrect data if provider doesn't fill it properly.</remarks>
		DatabaseName,
		/// <summary>The text command to run against the data source.</summary>
		CommandText
	}
}
