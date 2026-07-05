using System;
using System.Globalization;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class ProviderSpecificReaderValueTests : DataProviderTestBase
	{
		[Test]
		public void SqlServerDecimalProviderSpecificValuePreservesFullPrecision([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var conn   = GetDataConnection(context);
			using var result = conn.ExecuteReader("SELECT Cast(1.222222222222222222222222222222 as decimal(31,30))");
			var reader       = result.Reader ?? throw new InvalidOperationException("Reader is not available.");

			Assert.That(reader.Read(), Is.True);
			Assert.That(Convert.ToString(reader.GetProviderSpecificValue(0), CultureInfo.InvariantCulture), Is.EqualTo("1.222222222222222222222222222222"));
		}

		[Test]
		public void SqlServerHierarchyIdProviderSpecificValueHasReadableString([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var conn   = GetDataConnection(context);
			using var result = conn.ExecuteReader("SELECT Cast('/1/3/' as hierarchyid)");
			var reader       = result.Reader ?? throw new InvalidOperationException("Reader is not available.");

			Assert.That(reader.Read(), Is.True);
			Assert.That(Convert.ToString(reader.GetProviderSpecificValue(0), CultureInfo.InvariantCulture), Is.EqualTo("/1/3/"));
		}
	}
}
