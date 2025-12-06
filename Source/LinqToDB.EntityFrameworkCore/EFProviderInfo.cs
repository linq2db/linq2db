using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LinqToDB.EntityFrameworkCore
{
	/// <summary>
	/// Required integration information about underlying database provider, extracted from EF.Core.
	/// </summary>
	public sealed class EFProviderInfo
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
		/// Gets or sets EF.Core context instance.
		/// </summary>
		public DbContext? Context { get; set; }

		/// <summary>
		/// Gets or sets EF.Core context options instance.
		/// </summary>
		public IDbContextOptions? Options { get; set; }
	}
}
