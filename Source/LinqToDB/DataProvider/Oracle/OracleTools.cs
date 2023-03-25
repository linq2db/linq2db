using System;
using System.Data.Common;

namespace LinqToDB.DataProvider.Oracle
{
	using Data;

	public static partial class OracleTools
	{
		internal static OracleProviderDetector ProviderDetector = new();

		public static OracleVersion DefaultVersion
		{
			get => ProviderDetector.DefaultVersion;
			set => ProviderDetector.DefaultVersion = value;
		}

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(
			OracleVersion  version          = OracleVersion.AutoDetect,
			OracleProvider provider         = OracleProvider.Managed,
			string?        connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString : connectionString), provider, version);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(
			string connectionString,
			OracleVersion version   = OracleVersion.AutoDetect,
			OracleProvider provider = OracleProvider.Managed)
		{
			return new DataConnection(GetDataProvider(version, provider), connectionString);
		}

		public static DataConnection CreateDataConnection(
			DbConnection connection,
			OracleVersion version   = OracleVersion.AutoDetect,
			OracleProvider provider = OracleProvider.Managed)
		{
			return new DataConnection(GetDataProvider(version, provider), connection);
		}

		public static DataConnection CreateDataConnection(
			DbTransaction transaction,
			OracleVersion version   = OracleVersion.AutoDetect,
			OracleProvider provider = OracleProvider.Managed)
		{
			return new DataConnection(GetDataProvider(version, provider), transaction);
		}

		#endregion

		#region BulkCopy

		[Obsolete("Use OracleOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType  DefaultBulkCopyType
		{
			get => OracleOptions.Default.BulkCopyType;
			set => OracleOptions.Default = OracleOptions.Default with { BulkCopyType = value };
		}

		#endregion

		/// <summary>
		/// Specifies type of multi-row INSERT operation to generate for <see cref="BulkCopyType.RowByRow"/> bulk copy mode.
		/// Default value: <see cref="AlternativeBulkCopy.InsertAll"/>.
		/// </summary>
		[Obsolete("Use OracleOptions.Default.AlternativeBulkCopy instead.")]
		public static AlternativeBulkCopy UseAlternativeBulkCopy
		{
			get => OracleOptions.Default.AlternativeBulkCopy;
			set => OracleOptions.Default = OracleOptions.Default with { AlternativeBulkCopy = value };
		}

		/// <summary>
		/// Gets or sets flag to tell LinqToDB to quote identifiers, if they contain lowercase letters.
		/// Default value: <c>false</c>.
		/// This flag is added for backward compatibility and not recommended for use with new applications.
		/// </summary>
		[Obsolete("Use OracleOptions.Default.DontEscapeLowercaseIdentifiers instead.")]
		public static bool DontEscapeLowercaseIdentifiers
		{
			get => OracleOptions.Default.DontEscapeLowercaseIdentifiers;
			set => OracleOptions.Default = OracleOptions.Default with { DontEscapeLowercaseIdentifiers = value };
		}
	}
}
