using System;

// ReSharper disable once CheckNamespace
namespace LinqToDB
{
	using DataProvider.Oracle;
	using Infrastructure.Internal;

	public static partial class OptionsExtensions
	{
		#region UseOracle

		/// <summary>
		/// Configure connection to use Oracle default provider, specific dialect and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="DataContextOptionsBuilder"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// </remarks>
		public static DataContextOptionsBuilder UseOracle(this DataContextOptionsBuilder builder, string connectionString, OracleVersion dialect)
		{
			return builder.UseOracle(connectionString, o => o.UseServerVersion(dialect));
		}

		/// <summary>
		/// Configure connection to use specific Oracle provider, dialect and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="DataContextOptionsBuilder"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <param name="useNativeProvider">if <c>true</c>, <c>Oracle.DataAccess</c> provider will be used; otherwise managed <c>Oracle.ManagedDataAccess</c>.</param>
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

		#endregion
	}
}
