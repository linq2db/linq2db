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
		public string                            BasedOn;
		public Dictionary<string,TestConnection> Connections;
	}

	static class SettingsReader
	{
		public static TestSettings Deserialize(string configName, string defaultJson, string userJson)
		{
			void Merge(TestSettings settings1, TestSettings settings2)
			{
				foreach (var connection in settings2.Connections)
					if (!settings1.Connections.ContainsKey(connection.Key))
						settings1.Connections.Add(connection.Key, connection.Value);
			}

			var defaultSettings = JsonConvert.DeserializeObject<Dictionary<string,TestSettings>>(defaultJson);

			if (userJson != null)
			{
				var userSettings = JsonConvert.DeserializeObject<Dictionary<string,TestSettings>>(userJson);

				foreach (var uSetting in userSettings)
				{
					if (defaultSettings.TryGetValue(uSetting.Key, out var dSetting))
					{
						Merge(uSetting.Value, dSetting);

						if (uSetting.Value.BasedOn == null)
							uSetting.Value.BasedOn = dSetting.BasedOn;
					}
					else
					{
						defaultSettings.Add(uSetting.Key, uSetting.Value);
					}
				}

				foreach (var dSetting in defaultSettings)
					if (!userSettings.ContainsKey(dSetting.Key))
						userSettings.Add(dSetting.Key, dSetting.Value);

				defaultSettings = userSettings;
			}

			var readConfigs = new HashSet<string>();

			TestSettings GetSettings(string config)
			{
				if (readConfigs.Contains(config))
					throw new InvalidOperationException($"Circle basedOn configuration: '{config}'.");

				readConfigs.Add(config);

				if (!defaultSettings.TryGetValue(config, out var settings))
					throw new InvalidOperationException($"Configuration {config} not found.");

				if (settings.BasedOn != null)
				{
					var baseOnSettings = GetSettings(settings.BasedOn);

					Merge(settings, baseOnSettings);
				}

				return settings;
			}

			return GetSettings(configName ?? "");
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
	Default:
	{
		Connections:
		{
			'Con 1' : { ConnectionString : 'AAA', Provider : 'SqlServer' },
			'Con 2' : { ConnectionString : 'BBB', Provider : 'SqlServer' }
		},

		Providers:
		[ '111', '222' ]
	},

	CORE1:
	{
		BasedOn     : 'Default',
		Connections :
		{
			'Con 2' : { ConnectionString : 'AAA', Provider : 'SqlServer' },
			'Con 3' : { ConnectionString : 'CCC', Provider : 'SqlServer' }
		}
	},

	CORE2:
	{
		BasedOn     : 'Default',
		Connections :
		{
			'Con 2' : { ConnectionString : 'AAA', Provider : 'SqlServer' },
			'Con 3' : { ConnectionString : 'CCC', Provider : 'SqlServer' }
		}
	}
}";

		static string _userData = @"
{
	Default:
	{
		Connections:
		{
			'Con 1' : { ConnectionString : 'DDD', Provider : 'SqlServer' },
			'Con 4' : { ConnectionString : 'FFF', Provider : 'SqlServer' }
		}
	},

	'CORE2':
	{
		BasedOn     : 'Default',
		Connections :
		{
			'Con 2' : { ConnectionString : 'WWW', Provider : 'SqlServer' },
			'Con 5' : { ConnectionString : 'EEE', Provider : 'SqlServer' }
		}
	}
}";

		public static IEnumerable TestData
		{
			get
			{
				yield return new TestCaseData("Default", "Default", _defaultData, null)
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

				yield return new TestCaseData("User Default", "Default", _defaultData, _userData)
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
					//.SetName("Tests.Tools.UserCore2")
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

			return settings.Connections
				.Select (c => new { c.Key, c.Value.ConnectionString, c.Value.Provider })
				.OrderBy(c => c.Key);
		}

		[Test, Explicit]
		public void SerializeTest()
		{
			SettingsReader.Serialize();
		}
	}
}
