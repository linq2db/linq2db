using System.Data;
using System.Data.Common;
using NHibernate;

namespace LinqToDB.NHibernate
{
	/// <summary>
	/// Required integration information about the underlying database provider, extracted from NHibernate.
	/// </summary>
	public class NHProviderInfo
	{
		/// <summary>
		/// Gets or sets database connection instance.
		/// </summary>
		public IDbConnection? Connection { get; set; }

		/// <summary>
		/// Gets or sets the NHibernate session.
		/// </summary>
		public ISession? Session { get; set; }

		/// <summary>
		/// Gets or sets the NHibernate session factory.
		/// </summary>
		public ISessionFactory? Options { get; set; }
	}
}
