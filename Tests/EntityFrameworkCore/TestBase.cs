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
#elif NET6_0
			BaselinesManager.Dump(".EF6");
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

			Assert.Multiple(() =>
			{
				Assert.That(exceptExpected, Is.EqualTo(0), $"Expected Was{Environment.NewLine}{message}");
				Assert.That(exceptResult, Is.EqualTo(0), $"Expect Result{Environment.NewLine}{message}");
			});
		}
	}
}
