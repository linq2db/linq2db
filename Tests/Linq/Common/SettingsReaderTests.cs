using System.Collections;
using System.Linq;

using NUnit.Framework;

using Shouldly;

namespace Tests.Tools
{
	public class TestSettingsTests
	{
		static string _defaultData = /*lang=json,strict*/ @"
{
	""Default"":
	{
		""Connections"":
		{
			""Con 1"" : { ""ConnectionString"" : ""AAA"", ""Provider"" : ""SqlServer"" },
			""Con 2"" : { ""ConnectionString"" : ""BBB"", ""Provider"" : ""SqlServer"" }
		},

		""Providers"":
		[ ""111"", ""222"" ]
	},

	""NET60"":
	{
		""TraceLevel""  : ""Error"",
		""BasedOn""     : ""Default"",
		""Connections"" :
		{
			""Con 2"" : { ""ConnectionString"" : ""AAA"", ""Provider"" : ""SqlServer"" },
			""Con 3"" : { ""ConnectionString"" : ""CCC"", ""Provider"" : ""SqlServer"" }
		}
	},

	""NET80"":
	{
		""BasedOn""     : ""Default"",
		""Connections"" :
		{
			""Con 2"" : { ""ConnectionString"" : ""AAA"", ""Provider"" : ""SqlServer"" },
			""Con 3"" : { ""ConnectionString"" : ""CCC"", ""Provider"" : ""SqlServer"" }
		}
	}
}";

		static string _userData = /*lang=json,strict*/ @"
{
	""Default"":
	{
		""Connections"":
		{
			""Con 1"" : { ""ConnectionString"" : ""DDD"", ""Provider"" : ""SqlServer"" },
			""Con 4"" : { ""ConnectionString"" : ""FFF"", ""Provider"" : ""SqlServer"" }
		}
	},

	""NET80"":
	{
		""BasedOn""     : ""Default"",
		""Connections"" :
		{
			""Con 2"" : { ""ConnectionString"" : ""WWW"", ""Provider"" : ""SqlServer"" },
			""Con 5"" : { ""ConnectionString"" : ""EEE"", ""Provider"" : ""SqlServer"" }
		}
	}
}";

		public static IEnumerable TestData
		{
			get
			{
				yield return new TestCaseData("Default", "Default", _defaultData, null)
					.SetName("Tests.Tools.Default")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "BBB", Provider = "SqlServer" },
					});

				yield return new TestCaseData(".NET 6", "NET60", _defaultData, null)
					.SetName("Tests.Tools.Core1")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 3", ConnectionString = "CCC", Provider = "SqlServer" },
					});

				yield return new TestCaseData(".NET 8", "NET80", _defaultData, null)
					.SetName("Tests.Tools.Core2")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 3", ConnectionString = "CCC", Provider = "SqlServer" },
					});

				yield return new TestCaseData("User Default", "Default", _defaultData, _userData)
					.SetName("Tests.Tools.UserDefault")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "DDD", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "BBB", Provider = "SqlServer" },
						new { Key = "Con 4", ConnectionString = "FFF", Provider = "SqlServer" },
					});

				yield return new TestCaseData("User .NET 6", "NET60", _defaultData, _userData)
					.SetName("Tests.Tools.UserCore1")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "DDD", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 3", ConnectionString = "CCC", Provider = "SqlServer" },
						new { Key = "Con 4", ConnectionString = "FFF", Provider = "SqlServer" },
					});

				yield return new TestCaseData("User C.NET 8.0", "NET80", _defaultData, _userData)
					.SetName("Tests.Tools.UserCore2")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "DDD", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "WWW", Provider = "SqlServer" },
						new { Key = "Con 3", ConnectionString = "CCC", Provider = "SqlServer" },
						new { Key = "Con 4", ConnectionString = "FFF", Provider = "SqlServer" },
						new { Key = "Con 5", ConnectionString = "EEE", Provider = "SqlServer" },
					});
			}
		}

		[Test, TestCaseSource(nameof(TestData))]
		public IEnumerable DeserializeTest(string name, string config, string defaultJson, string userJson)
		{
			var settings = SettingsReader.Deserialize(config, defaultJson, userJson);
			settings.Connections ??= new();

			return settings.Connections
				.Select (c => new { c.Key, c.Value.ConnectionString, c.Value.Provider })
				.OrderBy(c => c.Key);
		}

		[Test]
		public void SerializeTest()
		{
			SettingsReader.Serialize();
		}

		// Shape of the CI test configs: Build/Azure/configs/<job>.json stores the provider list once, under a
		// target-framework-agnostic Azure.TestJob key that every <TFM>.Azure configuration in DataProviders.json
		// inherits. Pins both halves - the list reaches every TFM, and a per-TFM opt-out of an inherited
		// setting still wins, which is what sqlserver.2005.json needs for netfx.
		static readonly string _sharedJobDefaultData = /*lang=json,strict*/ @"
{
	""AzureConnectionStrings"" : { ""Connections"" : { ""Con 1"" : { ""ConnectionString"" : ""AAA"" } } },
	""Azure.TestJob""          : { ""BasedOn"" : ""AzureConnectionStrings"" },
	""NETFX.Azure""            : { ""BasedOn"" : ""Azure.TestJob"" },
	""NET110.Azure""           : { ""BasedOn"" : ""Azure.TestJob"" }
}";

		static readonly string _sharedJobUserData = /*lang=json,strict*/ @"
{
	""Azure.TestJob"" : { ""DisableRemoteContext"" : true, ""Providers"" : [ ""P1"", ""P2"" ] },
	""NETFX.Azure""   : { ""DisableRemoteContext"" : false }
}";

		[Test]
		public void SharedTestJobSection([Values("NETFX.Azure", "NET110.Azure")] string config)
		{
			var settings = SettingsReader.Deserialize(config, _sharedJobDefaultData, _sharedJobUserData);

			settings.Providers.ShouldBe(new[] { "P1", "P2" }, ignoreOrder: true);
			settings.Connections!.Keys.ShouldContain("Con 1");
			settings.DisableRemoteContext.ShouldBe(config != "NETFX.Azure");
		}
	}
}
