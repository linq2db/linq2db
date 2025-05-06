using System;
using System.Data.Common;

using LinqToDB.Data;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// Определяет, какой DataProvider использовать для YDB.
	/// Пока существует только один вариант <see cref="YdbDataProvider"/>,
	/// но структура сделана «на вырост» – по аналогии с другими провайдерами Linq to DB.
	/// </summary>
	sealed class YdbProviderDetector
		: ProviderDetectorBase<YdbProviderDetector.Provider, YdbProviderDetector.Version>
	{
		/// <summary>
		/// Перечисление подвариантов ADO-провайдера.
		/// Сейчас единственное значение (зарезервировано для будущего).
		/// </summary>
		internal enum Provider { }

		/// <summary>
		/// Версия сервера YDB, если когда-нибудь появятся различия
		/// (например, 23.1, 24.2 и т. п.). Пока всегда <see cref="Default"/>.
		/// </summary>
		internal enum Version
		{
			/// <summary>Использовать значение по умолчанию.</summary>
			Default,

			/// <summary>Определить автоматически (тот же <see cref="Default"/>).</summary>
			AutoDetect = Default
		}

		public YdbProviderDetector()
			: base(Version.AutoDetect, Version.Default)
		{
		}

		// ---------------------------------------------------------------------
		// Единичный экземпляр YdbDataProvider (создаётся лениво).
		// ---------------------------------------------------------------------
		static readonly Lazy<IDataProvider> _ydbDataProvider =
			CreateDataProvider<YdbDataProvider>();

		// ---------------------------------------------------------------------
		//  DetectProvider
		// ---------------------------------------------------------------------
		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			switch (options.ProviderName)
			{
				// явное указание имени провайдера
				case "YDB":
					return _ydbDataProvider.Value;

				// если в конфиге было указано только "YDB"
				case "":
				case null:
					if (options.ConfigurationString == "YDB")
						goto case YdbProviderAdapter.ClientNamespace;
					break;

				case YdbProviderAdapter.ClientNamespace:
				case var providerName when providerName.Contains("YDB",
						StringComparison.OrdinalIgnoreCase) ||
					   providerName.Contains(YdbProviderAdapter.AssemblyName,
						StringComparison.OrdinalIgnoreCase):
					return _ydbDataProvider.Value;
			}

			// если включён AutoDetectProvider, просто отдаём дефолтный DataProvider
			// (в YDB пока нет разных диалектов/версий SQL)
			if (AutoDetectProvider)
				return _ydbDataProvider.Value;

			return null;
		}

		// ---------------------------------------------------------------------
		//  GetDataProvider
		// ---------------------------------------------------------------------
		public override IDataProvider GetDataProvider(
			ConnectionOptions options, Provider provider, Version version)
		{
			// Версий пока нет – всегда один DataProvider
			return _ydbDataProvider.Value;
		}

		// ---------------------------------------------------------------------
		//  DetectServerVersion
		// ---------------------------------------------------------------------
		public override Version? DetectServerVersion(DbConnection connection)
		{
			// YDB сейчас имеет единый диалект,
			// поэтому детекция версии не требуется.
			return Version.Default;
		}

		// ---------------------------------------------------------------------
		//  CreateConnection
		// ---------------------------------------------------------------------
		protected override DbConnection CreateConnection(Provider provider, string connectionString)
		{
			return YdbProviderAdapter.GetInstance().CreateConnection(connectionString);
		}
	}
}
