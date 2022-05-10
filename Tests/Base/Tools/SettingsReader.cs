using System.Collections;
using System.Text.Json;
using NUnit.Framework;

namespace Tests.Tools;

public class TestConnection
{
	public string? ConnectionString { get; set; }
	public string? Provider         { get; set; }
}

public class TestSettings
{
	public string?                             BasedOn              { get; set; }
	public string?                             BaselinesPath        { get; set; }
	public string[]?                           Providers            { get; set; }
	public string[]?                           Skip                 { get; set; }
	public string?                             TraceLevel           { get; set; }
	public string?                             DefaultConfiguration { get; set; }
	public string?                             NoLinqService        { get; set; }
	public Dictionary<string, TestConnection>? Connections          { get; set; }
}

public static class SettingsReader
{
	private static readonly JsonSerializerOptions _jsonOptions = new() { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

	public static TestSettings Deserialize(string configName, string defaultJson, string? userJson)
	{
		void Merge(TestSettings settings1, TestSettings settings2)
		{
			settings1.Connections ??= new();
			settings2.Connections ??= new();

			foreach (var connection in settings2.Connections)
				if (!settings1.Connections.ContainsKey(connection.Key))
					settings1.Connections.Add(connection.Key, connection.Value);

			if (settings1.Providers == null)
				settings1.Providers = settings2.Providers;

			if (settings1.Skip == null)
				settings1.Skip = settings2.Skip;

			if (settings1.TraceLevel == null)
				settings1.TraceLevel = settings2.TraceLevel;

			if (settings1.DefaultConfiguration == null)
				settings1.DefaultConfiguration = settings2.DefaultConfiguration;

			if (settings1.NoLinqService == null)
				settings1.NoLinqService = settings2.NoLinqService;

			if (settings1.BaselinesPath == null)
				settings1.BaselinesPath = settings2.BaselinesPath;
		}

		var defaultSettings = JsonSerializer.Deserialize<Dictionary<string,TestSettings>>(defaultJson, _jsonOptions)!;

		if (userJson != null)
		{
			var userSettings = JsonSerializer.Deserialize<Dictionary<string,TestSettings>>(userJson, _jsonOptions)!;

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

			//Translate connection strings enclosed in brackets as references to other existing connection strings.
			settings.Connections ??= new();
			foreach (var connection in settings.Connections)
			{
				var cs = connection.Value.ConnectionString;
				if (cs != null && cs.StartsWith("[") && cs.EndsWith("]"))
				{
					cs = cs.Substring(1, cs.Length - 2);
					if (settings.Connections.TryGetValue(cs, out var baseConnection))
						connection.Value.ConnectionString = baseConnection.ConnectionString;
					else
						throw new InvalidOperationException($"Connection {cs} not found.");
				}
			}

			return settings;
		}

		return GetSettings(configName ?? "");
	}

	public static string Serialize()
	{
		var json = JsonSerializer.Serialize(
			new Dictionary<string,TestSettings>()
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
					"CORE21",
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

		return json;
	}
}
