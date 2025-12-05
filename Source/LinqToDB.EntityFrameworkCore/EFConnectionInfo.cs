using System.Data.Common;

namespace LinqToDB.EntityFrameworkCore
{
	/// <summary>
	/// Contains database connectivity information, extracted from EF.Core.
	/// </summary>
	public sealed class EFConnectionInfo
	{
		/// <summary>
		/// Gets or sets database connection instance.
		/// </summary>
		public DbConnection? Connection { get; set; }

		/// <summary>
		/// Gets or sets database transaction instance.
		/// </summary>
		public DbTransaction? Transaction { get; set; }

		/// <summary>
		/// Gets or sets database connection string.
		/// </summary>
		public string? ConnectionString { get; set; }
	}
}
