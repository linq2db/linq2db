using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore.Tests.Logging;
using LinqToDB.Mapping;
using LinqToDB.Tools;
using LinqToDB.Tools.Comparers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using NodaTime;

using NUnit.Framework;

using Tests;

#if NETFRAMEWORK
using MySqlConnectionStringBuilder = MySql.Data.MySqlClient.MySqlConnectionStringBuilder;
#else
using MySqlConnectionStringBuilder = MySqlConnector.MySqlConnectionStringBuilder;
#endif

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public abstract class TestBase
	{
		protected static readonly MappingSchema NodaTimeSupport = new MappingSchema();

		// bad analyzer
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
		protected static readonly ILoggerFactory LoggerFactory =
			Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
			{
				builder
					.AddFilter("Microsoft", LogLevel.Information)
					.AddFilter("System", LogLevel.Warning)
					.AddFilter("LinqToDB.EntityFrameworkCore.Test", LogLevel.Information)

					.AddTestLogger(o =>
					{
						o.FormatterName = ConsoleFormatterNames.Simple;
					});
			});
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method

		static TestBase()
		{
			try
			{
				// trigger settings preload
				_ = TestConfiguration.StoreMetrics;

				DatabaseUtils.CopyDatabases();

				NodaTimeSupport.SetConverter<LocalDateTime, DateTime>(timeStamp =>
					new DateTime(timeStamp.Year, timeStamp.Month, timeStamp.Day, timeStamp.Hour,
						timeStamp.Minute, timeStamp.Second, timeStamp.Millisecond));

#if NETFRAMEWORK
				NodaTimeSupport.SetConverter<Instant, DateTime>(inst => inst.ToDateTimeUtc());
#endif
			}
			catch (Exception ex)
			{
				TestUtils.Log(ex);
				throw;
			}
		}

		[TearDown]
		public virtual void OnAfterTest()
		{
			// as EF generates SQL differently, we cannot share baselines
#if NETFRAMEWORK
			BaselinesManager.Dump(".EF31");
#elif NET8_0
			BaselinesManager.Dump(".EF8");
#elif NET9_0
			BaselinesManager.Dump(".EF9");
#else
#error Unknown framework
#endif
			CustomTestContext.Release();
		}

		protected void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> result, bool allowEmpty = false)
		{
			AreEqual(t => t, expected, result, EqualityComparer<T>.Default, allowEmpty);
		}

		protected void AreEqualWithComparer<T>(IEnumerable<T> expected, IEnumerable<T> result)
		{
			AreEqual(t => t, expected, result, ComparerBuilder.GetEqualityComparer<T>());
		}

		protected void AreEqual<T>(Func<T, T> fixSelector, IEnumerable<T> expected, IEnumerable<T> result, IEqualityComparer<T> comparer, bool allowEmpty = false)
		{
			AreEqual(fixSelector, expected, result, comparer, null, allowEmpty);
		}

		protected void AreEqual<T>(
			Func<T, T> fixSelector,
			IEnumerable<T> expected,
			IEnumerable<T> result,
			IEqualityComparer<T> comparer,
			Func<IEnumerable<T>, IEnumerable<T>>? sort,
			bool allowEmpty = false)
		{
			var resultList = result.Select(fixSelector).ToList();
			var expectedList = expected.Select(fixSelector).ToList();

			if (sort != null)
			{
				resultList = sort(resultList).ToList();
				expectedList = sort(expectedList).ToList();
			}

			if (!allowEmpty)
				Assert.That(expectedList, Is.Not.Empty, "Expected list cannot be empty.");
			Assert.That(resultList, Has.Count.EqualTo(expectedList.Count), "Expected and result lists are different. Length: ");

			var exceptExpectedList = resultList.Except(expectedList, comparer).ToList();
			var exceptResultList = expectedList.Except(resultList, comparer).ToList();

			var exceptExpected = exceptExpectedList.Count;
			var exceptResult = exceptResultList.Count;
			var message = new StringBuilder();

			if (exceptResult != 0 || exceptExpected != 0)
			{
				Debug.WriteLine(resultList.ToDiagnosticString());
				Debug.WriteLine(expectedList.ToDiagnosticString());

				for (var i = 0; i < resultList.Count; i++)
				{
					Debug.WriteLine("{0} {1} --- {2}", comparer.Equals(expectedList[i], resultList[i]) ? " " : "-", expectedList[i], resultList[i]);
					message.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} --- {2}", comparer.Equals(expectedList[i], resultList[i]) ? " " : "-", expectedList[i], resultList[i]);
					message.AppendLine();
				}
			}

			using (Assert.EnterMultipleScope())
			{
				Assert.That(exceptExpected, Is.Zero, $"Expected Was{Environment.NewLine}{message}");
				Assert.That(exceptResult, Is.Zero, $"Expect Result{Environment.NewLine}{message}");
			}
		}

		// use TFM-specific suffix to avoid database conflicts on parallel runs
#if NETFRAMEWORK
		private const string DB_SUFFIX = "ef31";
#elif NET8_0
		private const string DB_SUFFIX = "ef8";
#elif NET9_0
		private const string DB_SUFFIX = "ef9";
#else
#error Unknown framework
#endif

		protected virtual string GetConnectionString(string provider)
		{
			var efProvider = provider + ".EF";
			var connectionString = DataConnection.TryGetConnectionString(efProvider);

			if (connectionString == null)
			{
				var originalCS = connectionString = DataConnection.GetConnectionString(provider);
				var dbProvider = DataConnection.GetDataProvider(provider);

				// create and register ef-specific connection string
				// and create database if needed (if EnsureCreated doesn't do it)
				switch (provider)
				{
					case var _ when provider.IsAnyOf(TestProvName.AllSqlServer):
					{
						var cnb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
						cnb.InitialCatalog += $".{DB_SUFFIX}";
						connectionString = cnb.ConnectionString;
						break;
					}
					case var _ when provider.IsAnyOf(TestProvName.AllPostgreSQL):
					{
						var cnb = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
						cnb.Database += $"_{DB_SUFFIX}";
						connectionString = cnb.ConnectionString;
						break;
					}
					case var _ when provider.IsAnyOf(TestProvName.AllMySql):
					{
						var cnb = new MySqlConnectionStringBuilder(connectionString);
						cnb.Database += $"_{DB_SUFFIX}";
						cnb.PersistSecurityInfo = true;
						connectionString = cnb.ConnectionString;
						break;
					}
					case var _ when provider.IsAnyOf(TestProvName.AllSQLite):
					{
						var cnb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
						cnb.DataSource = $"sqlite.{provider}.{DB_SUFFIX}.db";
						connectionString = cnb.ConnectionString;
						break;
					}
					default:
						throw new InvalidOperationException($"{nameof(GetConnectionString)} is not implemented for provider {provider}");
				}

				DataConnection.AddConfiguration(efProvider, connectionString, dbProvider);
			}

			return connectionString;
		}

	}
}
