using System;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Infrastructure;
using LinqToDB.Infrastructure.Internal;

// ReSharper disable once CheckNamespace
namespace LinqToDB
{
	public static class OracleDbContextOptionsExtensions
	{
		#region UseOracle
		/// <summary>
		/// Configure connection to use Oracle default provider, dialect and connection string.
		/// </summary>
		/// <param name="optionsBuilder">Instance of <see cref="DataContextOptionsBuilder"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="oracleOptionsAction">An optional action to allow additional Oracle specific configuration.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default Linq To DB tries to load managed version of Oracle provider.
		/// </para>
		/// <para>
		/// Oracle dialect will be choosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="OracleTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, Linq To DB will query server for version</item>
		/// <item>otherwise <see cref="OracleTools.DefaultVersion"/> (default: <see cref="OracleVersion.v12"/>) will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public static DataContextOptionsBuilder UseOracle(this DataContextOptionsBuilder optionsBuilder, string connectionString, Action<OracleDataContextOptionsBuilder>? oracleOptionsAction = null)
		{
			if (optionsBuilder == null)
				throw new ArgumentNullException(nameof(optionsBuilder));

			if (connectionString == null)
				throw new ArgumentNullException(nameof(connectionString));

			optionsBuilder = optionsBuilder
				.UseConnectionString(connectionString)
				.UseProvider(null)
				.UseDataProvider(null);

			var extension = GetOrCreateExtension(optionsBuilder);
			((IDataContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

			oracleOptionsAction?.Invoke(new OracleDataContextOptionsBuilder(optionsBuilder));

			return optionsBuilder;
		}

		/// <summary>
		/// Configure connection to use Oracle default provider, specific dialect and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="DataContextOptionsBuilder"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default Linq To DB tries to load managed version of Oracle provider.
		/// </para>
		/// </remarks>
		public static DataContextOptionsBuilder UseOracle(this DataContextOptionsBuilder builder, string connectionString, OracleVersion dialect)
		{
			return builder.UseOracle(connectionString, o => o.UseServerVersion(dialect));
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure connection to use specific Oracle provider, dialect and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="DataContextOptionsBuilder"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <param name="useNativeProvider">if <c>true</c>, <c>Oracle.DataAccess</c> provider will be used; othwerwise managed <c>Oracle.ManagedDataAccess</c>.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataContextOptionsBuilder UseOracle(this DataContextOptionsBuilder builder, string connectionString, OracleVersion dialect, bool useNativeProvider)
		{
			return builder.UseConnectionString(
				OracleTools.GetDataProvider(
					useNativeProvider ? ProviderName.OracleNative : ProviderName.OracleManaged,
					null,
					dialect),
				connectionString);
		}
#endif
		#endregion

		private static OracleOptionsExtension GetOrCreateExtension(DataContextOptionsBuilder optionsBuilder)
		{
			return optionsBuilder.Options.FindExtension<OracleOptionsExtension>()
			       ?? OracleTools.Options;
		}

		
	}
}
