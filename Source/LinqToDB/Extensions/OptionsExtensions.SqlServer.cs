using System;

// ReSharper disable once CheckNamespace

namespace LinqToDB
{
	using DataProvider.SqlServer;
	using Infrastructure;
	using Infrastructure.Internal;

	/// <summary>
	///     SQL Server specific extension methods for <see cref="DataContextOptionsBuilder" />.
	/// </summary>
	public static partial class OptionsExtensions
	{
		/// <summary>
		/// Configure connection to use specific SQL Server provider, dialect and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="DataContextOptionsBuilder"/>.</param>
		/// <param name="connectionString">SQL Server connection string.</param>
		/// <param name="provider">SQL Server provider to use.</param>
		/// <param name="dialect">SQL Server dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataContextOptionsBuilder UseSqlServer(
			this DataContextOptionsBuilder builder,
			string                         connectionString,
			SqlServerProvider              provider,
			SqlServerVersion               dialect)
		{
			return builder.UseSqlServer(connectionString, o => o.UseProvider(provider).UseServerVersion(dialect));
		}

		static SqlServerOptionsExtension GetOrCreateExtension(DataContextOptionsBuilder optionsBuilder)
		{
			return optionsBuilder.Options.FindExtension<SqlServerOptionsExtension>()
				?? new SqlServerOptionsExtension();
		}

		static void ConfigureWarnings(DataContextOptionsBuilder optionsBuilder)
		{
			var coreOptionsExtension
				= optionsBuilder.Options.FindExtension<CoreDataContextOptionsExtension>()
				?? new CoreDataContextOptionsExtension();

			((IDataContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(coreOptionsExtension);
		}
	}
}
