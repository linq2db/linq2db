using System;
using System.Collections.Generic;
using System.Text;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Oracle;

namespace LinqToDB.Infrastructure.Internal
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public class OracleOptionsExtension : RelationalOptionsExtension
	{
		private DbContextOptionsExtensionInfo? _info;

		private OracleVersion?      _serverVersion;
		private bool                _managed = true;
		private BulkCopyType        _defaultBulkCopyType;
		private AlternativeBulkCopy _alternativeBulkCopy;

		public OracleOptionsExtension()
		{
		}

		// NB: When adding new options, make sure to update the copy ctor below.

		protected OracleOptionsExtension(OracleOptionsExtension copyFrom)
			: base(copyFrom)
		{
			_serverVersion       = copyFrom._serverVersion;
			_managed             = copyFrom._managed;
			_defaultBulkCopyType = copyFrom._defaultBulkCopyType;
			_alternativeBulkCopy = copyFrom._alternativeBulkCopy;
		}

		public override DbContextOptionsExtensionInfo Info
			=> _info ??= new ExtensionInfo(this);

		protected override RelationalOptionsExtension Clone()
			=> new OracleOptionsExtension(this);

		public virtual OracleVersion? ServerVersion       => _serverVersion;
		public virtual BulkCopyType   DefaultBulkCopyType => _defaultBulkCopyType;
		public virtual bool           Managed             => _managed;

		public virtual AlternativeBulkCopy AlternativeBulkCopy => _alternativeBulkCopy;

		public virtual OracleOptionsExtension WithServerVersion(OracleVersion? serverVersion)
			=> SetValue(o => o._serverVersion = serverVersion);

		/// <summary>
		/// Specify Oracle ADO.NET Provider. Option has no effect for .NET Core, .NET Core always uses managed provider.
		/// </summary>
		public virtual OracleOptionsExtension WithManaged(bool managed)
			=> SetValue(o => o._managed = managed);

		/// <summary>
		/// BulkCopyType used by Oracle Provider by default.
		/// </summary>
		public virtual OracleOptionsExtension WithDefaultBulkCopyType(BulkCopyType defaultBulkCopyType)
			=> SetValue(o => o._defaultBulkCopyType = defaultBulkCopyType);

		/// <summary>
		/// Specify AlternativeBulkCopy used by Oracle Provider.
		/// </summary>
		public virtual OracleOptionsExtension WithAlternativeBulkCopy(AlternativeBulkCopy alternativeBulkCopy)
			=> SetValue(o => o._alternativeBulkCopy = alternativeBulkCopy);

		private OracleOptionsExtension SetValue(Action<OracleOptionsExtension> setter)
		{
			var clone = (OracleOptionsExtension)Clone();
			setter(clone);

			return clone;
		}

		public override void ApplyServices()
		{

		}

		public override IDataProvider GetDataProvider(DbDataContextOptionsExtension dbOptions)
		{
			OracleVersion serverVersion;

			if (_serverVersion != null)
			{
				serverVersion = _serverVersion.Value;
			}
			else
			{
				if (dbOptions.ConnectionString == null)
				{
					throw new InvalidOperationException(
						"For SQL provider detection, connection string should be defined.");
				}

				serverVersion = OracleTools.DetectServerVersionCached(dbOptions.ConnectionString, Managed);
			}

#if NETFRAMEWORK
			var managed = Managed;
#else
			var managed = true;
#endif

			return OracleTools.GetDataProvider(providerName: managed ? ProviderName.OracleManaged : ProviderName.OracleNative, version: serverVersion);
		}

		private sealed class ExtensionInfo : RelationalExtensionInfo
		{
			private long?   _serviceProviderHash;
			private string? _logFragment;

			public ExtensionInfo(IDbContextOptionsExtension extension)
				: base(extension)
			{
			}

			private new OracleOptionsExtension Extension
				=> (OracleOptionsExtension)base.Extension;

			public override bool IsDatabaseProvider => true;

			public override string LogFragment
			{
				get
				{
					if (_logFragment == null)
					{
						var builder = new StringBuilder();

						builder.Append(base.LogFragment);

						_logFragment = builder.ToString();
					}

					return _logFragment;
				}
			}

			public override long GetServiceProviderHashCode()
			{
				if (_serviceProviderHash == null)
				{
					_serviceProviderHash = (base.GetServiceProviderHashCode() * 397);
				}

				return _serviceProviderHash.Value;
			}

			public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
			{
				/*
	            debugInfo["SqlServer:" + nameof(SqlServerDbContextOptionsBuilder.UseRowNumberForPaging)]
		            = (Extension._rowNumberPaging?.GetHashCode() ?? 0L).ToString(CultureInfo.InvariantCulture);
            */
			}
		}
	}
}
