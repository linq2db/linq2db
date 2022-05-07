using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.Infrastructure.Internal
{
	using Data;
	using DataProvider;
	using DataProvider.Oracle;

	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public class OracleOptionsExtension : RelationalOptionsExtension
	{
		DataContextOptionsExtensionInfo? _info;

		OracleVersion?      _serverVersion;
		bool                _managed = true;
		BulkCopyType        _defaultBulkCopyType;
		AlternativeBulkCopy _alternativeBulkCopy;

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

		public override DataContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

		protected override RelationalOptionsExtension Clone()
		{
			return new OracleOptionsExtension(this);
		}

		public virtual OracleVersion?      ServerVersion       => _serverVersion;
		public virtual BulkCopyType        DefaultBulkCopyType => _defaultBulkCopyType;
		public virtual bool                Managed             => _managed;
		public virtual AlternativeBulkCopy AlternativeBulkCopy => _alternativeBulkCopy;

		public virtual OracleOptionsExtension WithServerVersion(OracleVersion? serverVersion)
			=> SetValue(o => o._serverVersion = serverVersion);

		/// <summary>
		/// Specify Oracle ADO.NET Provider. Option has no effect for .NET Core, .NET Core always uses managed provider.
		/// </summary>
		public virtual OracleOptionsExtension WithManaged(bool managed)
		{
			return SetValue(o => o._managed = managed);
		}

		/// <summary>
		/// BulkCopyType used by Oracle Provider by default.
		/// </summary>
		public virtual OracleOptionsExtension WithDefaultBulkCopyType(BulkCopyType defaultBulkCopyType)
		{
			return SetValue(o => o._defaultBulkCopyType = defaultBulkCopyType);
		}

		/// <summary>
		/// Specify AlternativeBulkCopy used by Oracle Provider.
		/// </summary>
		public virtual OracleOptionsExtension WithAlternativeBulkCopy(AlternativeBulkCopy alternativeBulkCopy)
		{
			return SetValue(o => o._alternativeBulkCopy = alternativeBulkCopy);
		}

		OracleOptionsExtension SetValue(Action<OracleOptionsExtension> setter)
		{
			var clone = (OracleOptionsExtension)Clone();

			setter(clone);

			return clone;
		}

		public override void ApplyServices()
		{
		}

		public override IDataProvider GetDataProvider(DataContextOptionsExtension dbOptions)
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

		sealed class ExtensionInfo : RelationalExtensionInfo
		{
			long?   _serviceProviderHash;
			string? _logFragment;

			public ExtensionInfo(IDataContextOptionsExtension extension)
				: base(extension)
			{
			}

			new OracleOptionsExtension Extension => (OracleOptionsExtension)base.Extension;

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
