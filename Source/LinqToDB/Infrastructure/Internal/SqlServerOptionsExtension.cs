using System;
using System.Collections.Generic;
using System.Text;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;

namespace LinqToDB.Infrastructure.Internal
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
    public class SqlServerOptionsExtension : RelationalOptionsExtension
    {
        private DbContextOptionsExtensionInfo? _info;

        private SqlServerVersion? _serverVersion  = SqlServerVersion.v2008;
        private SqlServerProvider _serverProvider = SqlServerTools.Provider;

        public SqlServerOptionsExtension()
        {
        }

        // NB: When adding new options, make sure to update the copy ctor below.

        protected SqlServerOptionsExtension(SqlServerOptionsExtension copyFrom)
            : base(copyFrom)
        {
            _serverVersion  = copyFrom._serverVersion;
            _serverProvider = copyFrom._serverProvider;
        }

        public override DbContextOptionsExtensionInfo Info
            => _info ??= new ExtensionInfo(this);

        protected override RelationalOptionsExtension Clone()
            => new SqlServerOptionsExtension(this);

		public virtual SqlServerVersion? ServerVersion => _serverVersion;

		public virtual SqlServerProvider ServerProvider => _serverProvider;


		public virtual SqlServerOptionsExtension WithServerVersion(SqlServerVersion? serverVersion)
			=> SetValue(o => o._serverVersion = serverVersion);

		public virtual SqlServerOptionsExtension WithServerProvider(SqlServerProvider serverProvider)
			=> SetValue(o => o._serverProvider = serverProvider);

        private SqlServerOptionsExtension SetValue(Action<SqlServerOptionsExtension> setter)
        {
	        var clone = (SqlServerOptionsExtension)Clone();
	        setter(clone);

	        return clone;
        }

        public override void ApplyServices()
        {

        }

        public override IDataProvider GetDataProvider(DbDataContextOptionsExtension dbOptions)
        {
	        SqlServerVersion serverVersion;

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

		        serverVersion = SqlServerTools.DetectServerVersionCached(_serverProvider, dbOptions.ConnectionString) ??
		                        SqlServerVersion.v2008;
	        }

	        return SqlServerTools.GetDataProvider(serverVersion, _serverProvider);
        }

        private sealed class ExtensionInfo : RelationalExtensionInfo
        {
            private long?   _serviceProviderHash;
            private string? _logFragment;

            public ExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            private new SqlServerOptionsExtension Extension
                => (SqlServerOptionsExtension)base.Extension;

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
