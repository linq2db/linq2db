using System.Data;

namespace LinqToDB.NHibernate
{
	/// <summary>
	/// Contains database connectivity information, extracted from NHibernate.
	/// </summary>
	public class NHConnectionInfo
	{
		/// <summary>
		/// Gets or sets database connection instance.
		/// </summary>
		public IDbConnection? Connection { get; set; }

		/// <summary>
		/// Gets or sets database connection string.
		/// </summary>
		public string? ConnectionString { get; set; }
	}
}
