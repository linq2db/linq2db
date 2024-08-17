using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.EntityFrameworkCore.Tests.Logging;
using LinqToDB.Tools;
using LinqToDB.Tools.Comparers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public abstract class TestBase
	{
		public static readonly ILoggerFactory LoggerFactory =
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
