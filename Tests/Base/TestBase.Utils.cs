using System;
using System.Collections.Generic;
using System.Text;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Informix;
using LinqToDB.DataProvider.SqlServer;

using Tests.Model;
using Tests.Tools;

namespace Tests
{
	public partial class TestBase
	{
		protected internal const string LinqServiceSuffix = ".LinqService";

		protected static bool IsSqlServerMarsEnabled(DataConnection dc)
		{
			// good-enough check for tests
			return dc.DataProvider is SqlServerDataProvider
				&& dc.ConnectionString is string cs
				&& cs.Contains("MultipleActiveResultSets=True", StringComparison.OrdinalIgnoreCase);
		}

		protected static char GetParameterToken(string context)
		{
			var token = '@';

			switch (context)
			{
				case ProviderName.SapHanaOdbc:
				case ProviderName.Informix:
					token = '?'; break;
				case ProviderName.SapHanaNative:
				case string when context.IsAnyOf(TestProvName.AllOracle, TestProvName.AllPostgreSQL):
					token = ':'; break;
			}

			return CustomizationSupport.Interceptor.GetParameterToken(token, context);
		}

		protected IEnumerable<LinqDataTypes2> AdjustExpectedData(ITestDataContext db, IEnumerable<LinqDataTypes2> data)
		{
			if (db.ProviderNeedsTimeFix(db.ContextName))
			{
				var adjusted = new List<LinqDataTypes2>();
				foreach (var record in data)
				{
					var copy = new LinqDataTypes2()
					{
						ID             = record.ID,
						MoneyValue     = record.MoneyValue,
						DateTimeValue  = record.DateTimeValue,
						DateTimeValue2 = record.DateTimeValue2,
						BoolValue      = record.BoolValue,
						GuidValue      = record.GuidValue,
						SmallIntValue  = record.SmallIntValue,
						IntValue       = record.IntValue,
						BigIntValue    = record.BigIntValue,
						StringValue    = record.StringValue
					};

					if (copy.DateTimeValue != null)
					{
						copy.DateTimeValue = copy.DateTimeValue.Value.AddMilliseconds(-copy.DateTimeValue.Value.Millisecond);
					}

					adjusted.Add(copy);
				}

				return adjusted;
			}

			return data;
		}

		protected bool IsCaseSensitiveDB(string context)
		{
			// we intentionally configure Sql Server 2019 test database to be case-sensitive to test
			// linq2db support for this configuration
			// on CI we test two configurations:
			// linux/mac: db is case sensitive, catalog is case insensitive
			// windows: both db and catalog are case sensitive
			var provider = GetProviderName(context, out var _);

			return provider.IsAnyOf(TestProvName.AllSqlServerCS)
				|| CustomizationSupport.Interceptor.IsCaseSensitiveDB(provider)
				;
		}

		/// <summary>
		/// Returns case-sensitivity of string comparison (e.g. using LIKE) without explicit collation specified.
		/// Depends on database implementation or database collation.
		/// </summary>
		protected bool IsCaseSensitiveComparison(string context)
		{
			var provider = GetProviderName(context, out var _);

			// we intentionally configure Sql Server 2019 test database to be case-sensitive to test
			// linq2db support for this configuration
			// on CI we test two configurations:
			// linux/mac: db is case sensitive, catalog is case insensitive
			// windows: both db and catalog are case sensitive
			return provider.IsAnyOf(TestProvName.AllSqlServerCS)
				|| provider.IsAnyOf(ProviderName.DB2)
				|| provider.IsAnyOf(TestProvName.AllClickHouse)
				|| provider.IsAnyOf(TestProvName.AllFirebird)
				|| provider.IsAnyOf(TestProvName.AllInformix)
				|| provider.IsAnyOf(TestProvName.AllOracle)
				|| provider.IsAnyOf(TestProvName.AllPostgreSQL)
				|| provider.IsAnyOf(TestProvName.AllSapHana)
				|| provider.IsAnyOf(TestProvName.AllSybase)
				|| CustomizationSupport.Interceptor.IsCaseSensitiveComparison(provider)
				;
		}

		/// <summary>
		/// Returns status of test CollatedTable - wether it is configured to have proper column collations or
		/// use database defaults (<see cref="IsCaseSensitiveComparison"/>).
		/// </summary>
		protected bool IsCollatedTableConfigured(string context)
		{
			var provider = GetProviderName(context, out var _);

			// unconfigured providers (some could be configured in theory):
			// Access : no such concept as collation on column level (db-only)
			// ClickHouse: collation supported only for order by clause
			// DB2
			// Informix
			// Oracle (in theory v12 has collations, but to enable them you need to complete quite a quest...)
			// PostgreSQL (v12 + custom collation required (no default CI collations))
			// SAP HANA
			// SQL CE
			// Sybase ASE
			return provider.IsAnyOf(TestProvName.AllSqlServer)
				|| provider.IsAnyOf(TestProvName.AllFirebird)
				|| provider.IsAnyOf(TestProvName.AllMySql)
				// while it is configured, LIKE in SQLite is case-insensitive (for ASCII only though)
				//|| provider.StartsWith(ProviderName.SQLite)
				|| CustomizationSupport.Interceptor.IsCollatedTableConfigured(provider)
				;
		}

		protected static Tests.Tools.TempTable<T> CreateTempTable<T>(IDataContext db, string tableName, string context)
			where T : notnull
		{
			return TempTable.Create<T>(db, GetTempTableName(tableName, context));
		}

		static string GetTempTableName(string tableName, string context)
		{
			var finalTableName = tableName;
			switch (context)
			{
				case string when context.IsAnyOf(TestProvName.AllSqlServer):
				{
					if (!tableName.StartsWith("#"))
						finalTableName = "#" + tableName;
					break;
				}
				default:
					throw new NotImplementedException();
			}

			return finalTableName;
		}

		protected static string GetProviderName(string context, out bool isLinqService)
		{
			isLinqService = context.IsRemote();
			return context.StripRemote();
		}

		protected static bool IsIDSProvider(string context)
		{
			if (!context.IsAnyOf(TestProvName.AllInformix))
				return false;
			var providerName = GetProviderName(context, out var _);
			if (providerName == ProviderName.InformixDB2)
				return true;

			using (DataConnection dc = new TestDataConnection(GetProviderName(context, out var _)))
				return ((InformixDataProvider)dc.DataProvider).Adapter.IsIDSProvider;
		}

		protected virtual BulkCopyOptions GetDefaultBulkCopyOptions(string configuration)
		{
			var options = new BulkCopyOptions();

			return options;
		}

		protected string GetCurrentBaselines()
		{
			return CustomTestContext.Get().Get<StringBuilder>(CustomTestContext.BASELINE)?.ToString() ?? string.Empty;
		}
	}
}
