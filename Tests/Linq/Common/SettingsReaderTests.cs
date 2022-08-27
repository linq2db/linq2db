﻿using System.Collections;
using NUnit.Framework;

namespace Tests.Tools
{
	public class TestSettingsTests
	{
		static string _defaultData = @"
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

	""CORE1"":
	{
		""TraceLevel""  : ""Error"",
		""BasedOn""     : ""Default"",
		""Connections"" :
		{
			""Con 2"" : { ""ConnectionString"" : ""AAA"", ""Provider"" : ""SqlServer"" },
			""Con 3"" : { ""ConnectionString"" : ""CCC"", ""Provider"" : ""SqlServer"" }
		}
	},

	""CORE21"":
	{
		""BasedOn""     : ""Default"",
		""Connections"" :
		{
			""Con 2"" : { ""ConnectionString"" : ""AAA"", ""Provider"" : ""SqlServer"" },
			""Con 3"" : { ""ConnectionString"" : ""CCC"", ""Provider"" : ""SqlServer"" }
		}
	}
}";

		static string _userData = @"
{
	""Default"":
	{
		""Connections"":
		{
			""Con 1"" : { ""ConnectionString"" : ""DDD"", ""Provider"" : ""SqlServer"" },
			""Con 4"" : { ""ConnectionString"" : ""FFF"", ""Provider"" : ""SqlServer"" }
		}
	},

	""CORE21"":
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

				yield return new TestCaseData("Core 1", "CORE1", _defaultData, null)
					.SetName("Tests.Tools.Core1")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 3", ConnectionString = "CCC", Provider = "SqlServer" },
					});

				yield return new TestCaseData("Core 2.1", "CORE21", _defaultData, null)
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

				yield return new TestCaseData("User Core 1", "CORE1", _defaultData, _userData)
					.SetName("Tests.Tools.UserCore1")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "DDD", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 3", ConnectionString = "CCC", Provider = "SqlServer" },
						new { Key = "Con 4", ConnectionString = "FFF", Provider = "SqlServer" },
					});

				yield return new TestCaseData("User Core 2.1", "CORE21", _defaultData, _userData)
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
	}
}
