using System;

namespace LinqToDB.Infrastructure
{
	using DataProvider.SqlServer;
	using Internal;

	/// <summary>
	/// <para>
	/// Allows SQL Server specific configuration to be performed on <see cref="DataContextOptions" />.
	/// </para>
	/// </summary>
	public class SqlServerDataContextOptionsBuilder
		: RelationalDataContextOptionsBuilder<SqlServerDataContextOptionsBuilder,SqlServerOptionsExtension>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SqlServerDataContextOptionsBuilder" /> class.
		/// </summary>
		/// <param name="optionsBuilder"> The options builder. </param>
		public SqlServerDataContextOptionsBuilder(DataContextOptionsBuilder optionsBuilder)
			: base(optionsBuilder)
		{
		}

		/// <summary>
		/// SQL Server dialect will be detected automatically.
		/// </summary>
		public virtual SqlServerDataContextOptionsBuilder AutodetectServerVersion()
		{
			return WithOption(e => e.WithServerVersion(null));
		}

		/// <summary>
		/// Specify SQL Server dialect.
		/// </summary>
		public virtual SqlServerDataContextOptionsBuilder UseServerVersion(SqlServerVersion serverVersion)
		{
			return WithOption(e => e.WithServerVersion(serverVersion));
		}

		/// <summary>
		/// Specify SQL Server ADO.NET Provider.
		/// </summary>
		public virtual SqlServerDataContextOptionsBuilder UseProvider(SqlServerProvider serverProvider)
		{
			return WithOption(e => e.WithServerProvider(serverProvider));
		}
	}
}
