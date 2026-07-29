using System.Data;
using System.Data.Common;

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

		/// <summary>
		/// Gets or sets the active database transaction, if any. Passed to the provider auto-detection so its
		/// probe query runs inside the transaction — required by providers (e.g. Firebird) whose connection is
		/// always transactional and reject a command with no transaction while one is pending.
		/// </summary>
		public DbTransaction? Transaction { get; set; }
	}
}
