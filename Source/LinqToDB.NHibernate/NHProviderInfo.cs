using System.Data;
using System.Data.Common;
using NHibernate;

namespace LinqToDB.NHibernateExtension
{
	/// <summary>
	/// Required integration information about underlying database provider, extracted from EF.Core.
	/// </summary>
	public class NHProviderInfo
	{
		/// <summary>
		/// Gets or sets database connection instance.
		/// </summary>
		public IDbConnection? Connection { get; set; }

		/// <summary>
		/// Gets or sets EF.Core context instance.
		/// </summary>
		public ISession? Session { get; set; }

		/// <summary>
		/// Gets or sets EF.Core context options instance.
		/// </summary>
		public ISessionFactory? Options { get; set; }
	}
}
