using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.Infrastructure.Internal
{
	using DataProvider;
	using DataProvider.Access;

	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public class AccessOptionsExtension : RelationalOptionsExtension
	{
		DataContextOptionsExtensionInfo? _info;

		public AccessOptionsExtension()
		{
		}

		// NB: When adding new options, make sure to update the copy ctor below.

		protected AccessOptionsExtension(AccessOptionsExtension copyFrom)
			: base(copyFrom)
		{
		}

		public override DataContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

		protected override RelationalOptionsExtension Clone()
			=> new AccessOptionsExtension(this);

		SqlServerOptionsExtension SetValue(Action<SqlServerOptionsExtension> setter)
		{
			var clone = (SqlServerOptionsExtension)Clone();

			setter(clone);

			return clone;
		}

		public override void ApplyServices()
		{
		}

		public override IDataProvider GetDataProvider(DataContextOptionsExtensionOld dbOptions)
		{
			throw new NotImplementedException();
		}

//		public override IDataProvider GetDataProvider(DataContextOptionsExtension dbOptions)
//		{
//			SqlServerVersion serverVersion;
//
//			if (_serverVersion != null)
//			{
//				serverVersion = _serverVersion.Value;
//			}
//			else
//			{
//				if (dbOptions.ConnectionString == null)
//				{
//					throw new InvalidOperationException("For SQL provider detection, connection string should be defined.");
//				}
//
//				serverVersion = SqlServerTools.DetectServerVersionCached(_serverProvider, dbOptions.ConnectionString) ?? SqlServerVersion.v2008;
//			}
//
//			return SqlServerTools.GetDataProvider(serverVersion, _serverProvider);
//		}

		sealed class ExtensionInfo : RelationalExtensionInfo
		{
			long?   _serviceProviderHash;
			string? _logFragment;

			public ExtensionInfo(IDataContextOptionsExtension extension)
				: base(extension)
			{
			}

			new SqlServerOptionsExtension Extension => (SqlServerOptionsExtension)base.Extension;

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
				_serviceProviderHash ??= base.GetServiceProviderHashCode() * 397;

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
