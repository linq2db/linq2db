using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

using NUnit.Framework;

namespace Tests.Tools
{
	public class TestConnection
	{
		public string ConnectionString;
		public string Provider;
	}

	public class TestSettings
	{
		public Dictionary<string,TestConnection> Connections;
	}

	static class SettingsReader
	{
		static TestSettings Deserialize(string config, string jsonData)
		{
			var settings = JsonConvert.DeserializeObject<Dictionary<string,TestSettings>>(jsonData);

			settings.TryGetValue("Default", out var defaultSettings);

			if (config != null && config != "Default")
			{
				if (settings.TryGetValue(config, out var configSettings))
				{
					if (defaultSettings == null)
						return configSettings;

					foreach (var connection in configSettings.Connections)
						defaultSettings.Connections[connection.Key] = connection.Value;
				}
			}

			return defaultSettings;
		}

		public static TestSettings Deserialize(string config, string defaultJson, string userJson)
		{
			var defaultSettings = Deserialize(config, defaultJson);

			if (userJson != null)
			{
				var userSettings = Deserialize(config, userJson);

				if (defaultSettings == null)
					return userSettings;

				if (userSettings != null)
					foreach (var connection in userSettings.Connections)
						defaultSettings.Connections[connection.Key] = connection.Value;
			}

			return defaultSettings;
		}

		public static void Serialize()
		{
			var json = JsonConvert.SerializeObject(
				new Dictionary<string,TestSettings>
				{
					{
						"Default",
						new TestSettings
						{
							Connections = new Dictionary<string,TestConnection>
							{
								{ "SqlServer", new TestConnection
									{
										ConnectionString = @"Server=DBHost\SQLSERVER2008;Database=TestData;User Id=sa;Password=TestPassword;",
										Provider         = "SqlServer",
									}
								},
								{ "SqlServer1", new TestConnection
									{
										ConnectionString = @"Server=DBHost\SQLSERVER2008;Database=TestData;User Id=sa;Password=TestPassword;",
										Provider         = "SqlServer1",
									}
								},
							}
						}
					},
					{
						"CORE2",
						new TestSettings
						{
							Connections = new Dictionary<string,TestConnection>
							{
								{ "SqlServer", new TestConnection
									{
										ConnectionString = @"Server=DBHost\SQLSERVER2008;Database=TestData;User Id=sa;Password=TestPassword;",
										Provider         = "SqlServer",
									}
								},
								{ "SqlServer1", new TestConnection
									{
										ConnectionString = @"Server=DBHost\SQLSERVER2008;Database=TestData;User Id=sa;Password=TestPassword;",
										Provider         = "SqlServer1",
									}
								},
							}
						}
					},
				});

			File.WriteAllText("DefaultTestSettings.json", json);
		}
	}

	public class TestSettingsTests
	{
		static string _defaultData = @"
{
	'Default':
	{
		'Connections':
		{
			'Con 1' : { 'ConnectionString' : 'AAA', 'Provider' : 'SqlServer' },
			'Con 2' : { 'ConnectionString' : 'BBB', 'Provider' : 'SqlServer' }
		}
	},

	'CORE1':
	{
		'Connections':
		{
			'Con 2' : { 'ConnectionString' : 'AAA', 'Provider' : 'SqlServer' },
			'Con 3' : { 'ConnectionString' : 'CCC', 'Provider' : 'SqlServer' }
		}
	},

	'CORE2':
	{
		'Connections':
		{
			'Con 2' : { 'ConnectionString' : 'AAA', 'Provider' : 'SqlServer' },
			'Con 3' : { 'ConnectionString' : 'CCC', 'Provider' : 'SqlServer' }
		}
	}
}";

		static string _userData = @"
{
	'Default':
	{
		'Connections':
		{
			'Con 1' : { 'ConnectionString' : 'DDD', 'Provider' : 'SqlServer' },
			'Con 4' : { 'ConnectionString' : 'FFF', 'Provider' : 'SqlServer' }
		}
	},

	'CORE2':
	{
		'Connections':
		{
			'Con 2' : { 'ConnectionString' : 'WWW', 'Provider' : 'SqlServer' },
			'Con 5' : { 'ConnectionString' : 'EEE', 'Provider' : 'SqlServer' }
		}
	}
}";

		public static IEnumerable TestData
		{
			get
			{
				yield return new TestCaseData("Default", null, _defaultData, null)
					//.SetName("Default")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "BBB", Provider = "SqlServer" },
					});

				yield return new TestCaseData("Core 1", "CORE1", _defaultData, null)
					//.SetName("Core 1")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 3", ConnectionString = "CCC", Provider = "SqlServer" },
					});

				yield return new TestCaseData("Core 2", "CORE2", _defaultData, null)
					//.SetName("Core 2")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 3", ConnectionString = "CCC", Provider = "SqlServer" },
					});

				yield return new TestCaseData("User Default", null, _defaultData, _userData)
					//.SetName("User Default")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "DDD", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "BBB", Provider = "SqlServer" },
						new { Key = "Con 4", ConnectionString = "FFF", Provider = "SqlServer" },
					});

				yield return new TestCaseData("User Core 1", "CORE1", _defaultData, _userData)
					//.SetName("User Core 1")
					.Returns(new[]
					{
						new { Key = "Con 1", ConnectionString = "DDD", Provider = "SqlServer" },
						new { Key = "Con 2", ConnectionString = "AAA", Provider = "SqlServer" },
						new { Key = "Con 3", ConnectionString = "CCC", Provider = "SqlServer" },
						new { Key = "Con 4", ConnectionString = "FFF", Provider = "SqlServer" },
					});

				yield return new TestCaseData("User Core 2", "CORE2", _defaultData, _userData)
					//.SetName("User Core 2")
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

			return settings.Connections.Select(c => new { c.Key, c.Value.ConnectionString, c.Value.Provider });
		}

		[Test, Explicit]
		public void SerializeTest()
		{
			SettingsReader.Serialize();
		}
	}
}
