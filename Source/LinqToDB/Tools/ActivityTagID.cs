using System;

namespace LinqToDB.Tools
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
		DataSourceName,
		/// <summary>The name of the current database.</summary>
		DatabaseName,
		/// <summary>The text command to run against the data source.</summary>
		CommandText
	}
}
