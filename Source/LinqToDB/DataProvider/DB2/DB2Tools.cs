using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Tools;

namespace LinqToDB.DataProvider.DB2
{
	using Configuration;
	using Data;

	[PublicAPI]
	public static class DB2Tools
	{
		static readonly Lazy<IDataProvider> _db2DataProviderzOS = DataConnection.CreateDataProvider<DB2zOSDataProvider>();
		static readonly Lazy<IDataProvider> _db2DataProviderLUW = DataConnection.CreateDataProvider<DB2LUWDataProvider>();

		public static bool AutoDetectProvider { get; set; } = true;

		internal static IDataProvider? ProviderDetector(ConnectionOptions options)
		{
			// DB2 ODS provider could be used by informix
			if (options.ConfigurationString?.Contains("Informix") == true)
				return null;

			switch (options.ProviderName)
			{
				case ProviderName.DB2LUW: return _db2DataProviderLUW.Value;
				case ProviderName.DB2zOS: return _db2DataProviderzOS.Value;

				case ""             :
				case null           :

					if (options.ConfigurationString == "DB2")
						goto case ProviderName.DB2;
					break;

				case ProviderName.DB2    :
				case DB2ProviderAdapter.NetFxClientNamespace:
				case DB2ProviderAdapter.CoreClientNamespace :

					if (options.ConfigurationString?.Contains("LUW") == true)
						return _db2DataProviderLUW.Value;
					if (options.ConfigurationString?.Contains("z/OS") == true || options.ConfigurationString?.Contains("zOS") == true)
						return _db2DataProviderzOS.Value;

					if (AutoDetectProvider && options.ConnectionString != null)
					{
						try
						{
							using var conn = DB2ProviderAdapter.Instance.CreateConnection(options.ConnectionString);

							if (options.ConnectionInterceptor == null)
							{
								conn.Open();
							}
							else
							{
								using (ActivityService.Start(ActivityID.ConnectionInterceptorConnectionOpening))
									options.ConnectionInterceptor.ConnectionOpening(new(null), ((IConnectionWrapper)conn).Connection);

								conn.Open();

								using (ActivityService.Start(ActivityID.ConnectionInterceptorConnectionOpened))
									options.ConnectionInterceptor.ConnectionOpened(new(null), ((IConnectionWrapper)conn).Connection);
							}

							var iszOS = conn.eServerType == DB2ProviderAdapter.DB2ServerTypes.DB2_390;

							return iszOS ? _db2DataProviderzOS.Value : _db2DataProviderLUW.Value;
						}
						catch
						{
							// ignored
						}
					}

					return GetDataProvider();
			}

			return null;
		}

		public static IDataProvider GetDataProvider(DB2Version version = DB2Version.LUW)
		{
			if (version == DB2Version.zOS)
				return _db2DataProviderzOS.Value;

			return _db2DataProviderLUW.Value;
		}

		public static void ResolveDB2(string path)
		{
			new AssemblyResolver(path, DB2ProviderAdapter.AssemblyName);
#if !NETFRAMEWORK
			new AssemblyResolver(path, DB2ProviderAdapter.AssemblyNameOld);
#endif
		}

		public static void ResolveDB2(Assembly assembly)
		{
			new AssemblyResolver(assembly, assembly.GetName().Name!);
		}

		#region CreateDataConnection

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided DB2 connection string.
		/// </summary>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="version">DB2 version.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(string connectionString, DB2Version version = DB2Version.LUW)
		{
			return new DataConnection(GetDataProvider(version), connectionString);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided connection object.
		/// </summary>
		/// <param name="connection">Connection instance.</param>
		/// <param name="version">DB2 version.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(DbConnection connection, DB2Version version = DB2Version.LUW)
		{
			return new DataConnection(GetDataProvider(version), connection);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided transaction object.
		/// </summary>
		/// <param name="transaction">Transaction instance.</param>
		/// <param name="version">DB2 version.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(DbTransaction transaction, DB2Version version = DB2Version.LUW)
		{
			return new DataConnection(GetDataProvider(version), transaction);
		}

		#endregion

		#region BulkCopy

		/// <summary>
		/// Default bulk copy mode, used for DB2 by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
		/// methods, if mode is not specified explicitly.
		/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
		/// </summary>
		[Obsolete("Use DB2Options.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => DB2Options.Default.BulkCopyType;
			set => DB2Options.Default = DB2Options.Default with { BulkCopyType = value };
		}

		#endregion
	}
}
