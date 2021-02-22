using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2816Tests : TestBase
	{
		/*
		 * IsNullOrWhiteSpace takes into account non-surrogate codepoints from unicode with White_Space property
		 * Per latest Unicode 13 version those are (https://www.unicode.org/Public/13.0.0/ucd/PropList.txt):
		 */
		// use int for readable test name
		private static readonly int[] WhiteSpaceChars = new int[]
		{
			0x09,
			0x0A,
			0x0B,
			0x0C,
			0x0D,
			0x20,
			0x85,
			0xA0,
			0x1680,
			0x2000,
			0x2001,
			0x2002,
			0x2003,
			0x2004,
			0x2005,
			0x2006,
			0x2007,
			0x2008,
			0x2009,
			0x200A,
			0x2028,
			0x2029,
			0x205F,
			0x3000,
		};

		[Table("Issue2816Table")]
		class TestClass
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public string? Text { get; set; }
		}

		[Test]
		public void BasicTestCases([DataSources] string context)
		{
			var cnt = 0;

			var testData = new[]
			{
				new TestClass() { Id = cnt++, Text = "a" },
				new TestClass() { Id = cnt++, Text = " m " },
				new TestClass() { Id = cnt++, Text = " " },
				new TestClass() { Id = cnt++, Text = "  " },
				new TestClass() { Id = cnt++, Text = null }
			};

			using (var db = GetDataContext(context))
			{
				using (var table = db.CreateLocalTable(testData))
				{
					var query = from p in table
								where string.IsNullOrWhiteSpace(p.Text)
								select p;

					AssertQuery(query);
				}
			}
		}

		[Test]
		public void FullWhiteSpaceTest([DataSources] string context, [ValueSource(nameof(WhiteSpaceChars))] int character)
		{
			if (!string.IsNullOrWhiteSpace(((char)character).ToString()))
				Assert.Inconclusive($"Character {(char)character} not supported by runtime");

			var testData = GetTestCase((char)character, GetProviderName(context, out _), out var supported);

			using (var db = GetDataContext(context))
			{
				using (var table = db.CreateLocalTable(testData))
				{
					var query1 = (from p in table.ToArray()
								 where !string.IsNullOrWhiteSpace(p.Text)
								 select p).ToArray();
					var query = (from p in table
								 where !string.IsNullOrWhiteSpace(p.Text)
								 select p).ToArray();

					if (supported)
					{
						Assert.AreEqual(1, query.Length);
						Assert.AreEqual(3, query[0].Id);
					}
					else
					{
						Assert.AreEqual(3, query.Length);
					}
				}
			}
		}

		private static TestClass[] GetTestCase(char character, string providerName, out bool supported)
		{
			supported = IsSupported(character, providerName);

			return new[]
			{
				new TestClass()
				{
					Id = 1,
					Text = $"{character}"
				},
				new TestClass()
				{
					Id = 2,
					Text = $" {character} "
				},
				new TestClass()
				{
					Id = 3,
					Text = $" {character}x "
				}
			};
		}

		private static bool IsSupported(char character, string providerName)
		{
			switch (providerName)
			{
				case ProviderName.Informix:
				case ProviderName.InformixDB2:
					// teh winner!
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0x85;

				case ProviderName.Access:
				case ProviderName.AccessOdbc:
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0xA0
						&& character != 0x85
						&& character != 0x2000
						&& character != 0x2001
						&& character != 0x2002
						&& character != 0x2003
						&& character != 0x2004
						&& character != 0x2005
						&& character != 0x2006
						&& character != 0x2007
						&& character != 0x2008
						&& character != 0x2009
						&& character != 0x200A
						&& character != 0x2028
						&& character != 0x2029;

				case ProviderName.DB2:
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0xA0
						&& character != 0x85
						&& character != 0x1680
						&& character != 0x2000
						&& character != 0x2001
						&& character != 0x2002
						&& character != 0x2003
						&& character != 0x2004
						&& character != 0x2005
						&& character != 0x2006
						&& character != 0x2007
						&& character != 0x2008
						&& character != 0x2009
						&& character != 0x200A
						&& character != 0x2028
						&& character != 0x2029
						&& character != 0x205F
						&& character != 0x3000;

				case ProviderName.Firebird:
				case TestProvName.Firebird3:
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0xA0
						&& character != 0x85
						&& character != 0x1680
						&& character != 0x2000
						&& character != 0x2001
						&& character != 0x2002
						&& character != 0x2003
						&& character != 0x2004
						&& character != 0x2005
						&& character != 0x2006
						&& character != 0x2007
						&& character != 0x2008
						&& character != 0x2009
						&& character != 0x200A
						&& character != 0x2028
						&& character != 0x2029
						&& character != 0x205F
						&& character != 0x3000;

				case ProviderName.MySql:
				case ProviderName.MySqlConnector:
				case ProviderName.MySqlOfficial:
				case TestProvName.MySql55:
				case TestProvName.MariaDB:
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0xA0
						&& character != 0x85
						&& character != 0x1680
						&& character != 0x2000
						&& character != 0x2001
						&& character != 0x2002
						&& character != 0x2003
						&& character != 0x2004
						&& character != 0x2005
						&& character != 0x2006
						&& character != 0x2007
						&& character != 0x2008
						&& character != 0x2009
						&& character != 0x200A
						&& character != 0x2028
						&& character != 0x2029
						&& character != 0x205F
						&& character != 0x3000;

				case ProviderName.OracleManaged:
				case ProviderName.OracleNative:
				case TestProvName.Oracle11Managed:
				case TestProvName.Oracle11Native:
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0xA0
						&& character != 0x85
						&& character != 0x1680
						&& character != 0x2000
						&& character != 0x2001
						&& character != 0x2002
						&& character != 0x2003
						&& character != 0x2004
						&& character != 0x2005
						&& character != 0x2006
						&& character != 0x2007
						&& character != 0x2008
						&& character != 0x2009
						&& character != 0x200A
						&& character != 0x2028
						&& character != 0x2029
						&& character != 0x205F
						&& character != 0x3000;

				case ProviderName.PostgreSQL:
				case ProviderName.PostgreSQL92:
				case ProviderName.PostgreSQL93:
				case ProviderName.PostgreSQL95:
				case TestProvName.PostgreSQL10:
				case TestProvName.PostgreSQL11:
				case TestProvName.PostgreSQL12:
				case TestProvName.PostgreSQL13:
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0xA0
						&& character != 0x85
						&& character != 0x1680
						&& character != 0x2000
						&& character != 0x2001
						&& character != 0x2002
						&& character != 0x2003
						&& character != 0x2004
						&& character != 0x2005
						&& character != 0x2006
						&& character != 0x2007
						&& character != 0x2008
						&& character != 0x2009
						&& character != 0x200A
						&& character != 0x2028
						&& character != 0x2029
						&& character != 0x205F
						&& character != 0x3000;

				case ProviderName.SapHanaNative:
				case ProviderName.SapHanaOdbc:
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0xA0
						&& character != 0x85
						&& character != 0x1680
						&& character != 0x2000
						&& character != 0x2001
						&& character != 0x2002
						&& character != 0x2003
						&& character != 0x2004
						&& character != 0x2005
						&& character != 0x2006
						&& character != 0x2007
						&& character != 0x2008
						&& character != 0x2009
						&& character != 0x200A
						&& character != 0x2028
						&& character != 0x2029
						&& character != 0x205F
						&& character != 0x3000;

				case ProviderName.SqlCe:
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0xA0
						&& character != 0x85
						&& character != 0x1680
						&& character != 0x2000
						&& character != 0x2001
						&& character != 0x2002
						&& character != 0x2003
						&& character != 0x2004
						&& character != 0x2005
						&& character != 0x2006
						&& character != 0x2007
						&& character != 0x2008
						&& character != 0x2009
						&& character != 0x200A
						&& character != 0x2028
						&& character != 0x2029
						&& character != 0x205F
						&& character != 0x3000;

				case ProviderName.SQLiteClassic:
				case ProviderName.SQLiteMS:
				case TestProvName.SQLiteClassicMiniProfilerMapped:
				case TestProvName.SQLiteClassicMiniProfilerUnmapped:
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0xA0
						&& character != 0x85
						&& character != 0x1680
						&& character != 0x2000
						&& character != 0x2001
						&& character != 0x2002
						&& character != 0x2003
						&& character != 0x2004
						&& character != 0x2005
						&& character != 0x2006
						&& character != 0x2007
						&& character != 0x2008
						&& character != 0x2009
						&& character != 0x200A
						&& character != 0x2028
						&& character != 0x2029
						&& character != 0x205F
						&& character != 0x3000;

				case ProviderName.SqlServer2012:
				case ProviderName.SqlServer2014:
					// 0x1680 - ???
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0xA0
						&& character != 0x85
						&& character != 0x1680
						&& character != 0x2000
						&& character != 0x2001
						&& character != 0x2002
						&& character != 0x2003
						&& character != 0x2004
						&& character != 0x2005
						&& character != 0x2006
						&& character != 0x2007
						&& character != 0x2008
						&& character != 0x2009
						&& character != 0x200A
						&& character != 0x2028
						&& character != 0x2029;

				case ProviderName.SqlServer2000:
				case ProviderName.SqlServer2005:
				case ProviderName.SqlServer2008:
				case ProviderName.SqlServer2017:
				case TestProvName.SqlServer2019:
				case TestProvName.SqlAzure:
				case TestProvName.SqlServer2019FastExpressionCompiler:
				case TestProvName.SqlServer2019SequentialAccess:
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0xA0
						&& character != 0x85
						&& character != 0x2000
						&& character != 0x2001
						&& character != 0x2002
						&& character != 0x2003
						&& character != 0x2004
						&& character != 0x2005
						&& character != 0x2006
						&& character != 0x2007
						&& character != 0x2008
						&& character != 0x2009
						&& character != 0x200A
						&& character != 0x2028
						&& character != 0x2029;

				case ProviderName.Sybase:
				case ProviderName.SybaseManaged:
					return character != 0x09
						&& character != 0x0A
						&& character != 0x0B
						&& character != 0x0C
						&& character != 0x0D
						&& character != 0xA0
						&& character != 0x85
						&& character != 0x1680
						&& character != 0x2000
						&& character != 0x2001
						&& character != 0x2002
						&& character != 0x2003
						&& character != 0x2004
						&& character != 0x2005
						&& character != 0x2006
						&& character != 0x2007
						&& character != 0x2008
						&& character != 0x2009
						&& character != 0x200A
						&& character != 0x2028
						&& character != 0x2029
						&& character != 0x205F
						&& character != 0x3000;
			}
			return true;
		}
	}
}
