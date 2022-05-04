using LinqToDB.Data;

namespace LinqToDB.Infrastructure
{
	using DataProvider.Oracle;
	using Internal;

	/// <summary>
	///     <para>
	///         Allows Oracle specific configuration to be performed on <see cref="DataContextOptions" />.
	///     </para>
	/// </summary>
	public class OracleDataContextOptionsBuilder
		: RelationalDataContextOptionsBuilder<OracleDataContextOptionsBuilder, OracleOptionsExtension>
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="SqlServerDataContextOptionsBuilder" /> class.
		/// </summary>
		/// <param name="optionsBuilder"> The options builder. </param>
		public OracleDataContextOptionsBuilder(DataContextOptionsBuilder optionsBuilder)
			: base(optionsBuilder)
		{
		}

		/// <summary>
		/// Oracle dialect will be detected automatically.
		/// </summary>
		public virtual OracleDataContextOptionsBuilder AutoDetectServerVersion()
			=> WithOption(e => e.WithServerVersion(null));

		/// <summary>
		/// Specify Oracle dialect.
		/// </summary>
		public virtual OracleDataContextOptionsBuilder UseServerVersion(OracleVersion serverVersion)
			=> WithOption(e => e.WithServerVersion(serverVersion));

		/// <summary>
		/// Specify Oracle ADO.NET Provider. Option has no effect for .NET Core, .NET Core always uses managed provider.
		/// </summary>
		public virtual OracleDataContextOptionsBuilder UseNativeProvider()
			=> WithOption(e => e.WithManaged(false));

		/// <summary>
		/// Specify BulkCopyType used by Oracle Provider by default.
		/// </summary>
		public virtual OracleDataContextOptionsBuilder UseDefaultBulkCopyType(BulkCopyType defaultBulkCopyType)
			=> WithOption(e => e.WithDefaultBulkCopyType(defaultBulkCopyType));

		/// <summary>
		/// Specify AlternativeBulkCopy used by Oracle Provider.
		/// </summary>
		public virtual OracleDataContextOptionsBuilder UseAlternativeBulkCopy(AlternativeBulkCopy alternativeBulkCopy)
			=> WithOption(e => e.WithAlternativeBulkCopy(alternativeBulkCopy));

	}
}
