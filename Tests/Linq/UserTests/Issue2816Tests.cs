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
		sealed class TestClass
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
				new TestClass() { Id = cnt++, Text = "" },
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

			var testData = GetTestCase((char)character, GetProviderName(context, out var isRemoteContext), out var supported);
			if (!supported)
				Assert.Inconclusive($"Character {(char)character} not supported by database");

			using (var db = GetDataContext(context))
			{
				using (var table = db.CreateLocalTable(testData))
				{
					var query = (from p in table
								 where !string.IsNullOrWhiteSpace(p.Text)
								 select p).ToArray();

					if (supported)
					{
						Assert.That(query, Has.Length.EqualTo(1));
						Assert.That(query[0].Id, Is.EqualTo(3));
					}
					else
					{
						Assert.That(query, Has.Length.EqualTo(3));
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
				case string when providerName.IsAnyOf(TestProvName.AllAccess):
					// only 4 characters including space supported
					return character == 0x20
						|| character == 0x1680
						|| character == 0x205F
						|| character == 0x3000;
#if AZURE
				case ProviderName.InformixDB2:
					// TODO: fix azure instance locale
					// currently fails on test data insert with
					// ERROR [IX000] [IBM][IDS/UNIX64] Code-set conversion function failed due to illegal sequence or invalid value.
					return character <= 0xA0;
#endif
			}

			return true;
		}
	}
}
