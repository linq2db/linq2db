using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Tests.Tools
{
	public class TestConnection
	{
		public string? ConnectionString { get; set; }
		public string? Provider         { get; set; }
	}

	public class TestSettings
	{
		public bool?                               DisableRemoteContext { get; set; }
		public string?                             BasedOn              { get; set; }
		public string?                             BaselinesPath        { get; set; }
		public bool?                               StoreMetrics         { get; set; }
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

				if (settings1.Providers is not null && settings2.Providers is not null)
					settings1.Providers = settings2.Providers.Concat(settings1.Providers).ToArray();

				settings1.Providers            ??= settings2.Providers;
				settings1.Skip                 ??= settings2.Skip;
				settings1.TraceLevel           ??= settings2.TraceLevel;
				settings1.DefaultConfiguration ??= settings2.DefaultConfiguration;
				settings1.NoLinqService        ??= settings2.NoLinqService;
				settings1.BaselinesPath        ??= settings2.BaselinesPath;
				settings1.StoreMetrics         ??= settings2.StoreMetrics;
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

					if (cs is ['[', .., ']'])
					{
						cs = cs.Substring(1, cs.Length - 2);
						if (settings.Connections.TryGetValue(cs, out var baseConnection))
							connection.Value.ConnectionString = baseConnection.ConnectionString;
						else
							throw new InvalidOperationException($"Connection {cs} not found.");
					}
				}

				if (settings.Providers is not null)
				{
					var providers = new HashSet<string>();

					foreach (var provider in settings.Providers)
					{
						switch (provider)
						{
							case "++" or "+++" or "all":
								foreach (var p in TestConfiguration.Providers)
									providers.Add(p);
								break;
							case "--" or "---":
								providers.Clear();
								break;
							default:
							{
								if (provider.StartsWith('-'))
								{
									var p = provider.Replace("-", "").Trim();

									if (p is ['*', .., '*'])
									{
										p = p.Trim('*');

										foreach (var pr in providers.ToList())
											if (pr.Contains(p))
												providers.Remove(pr);
									}
									else if (p is ['*', ..])
									{
										p = p.Trim('*');

										foreach (var pr in providers.ToList())
											if (pr.EndsWith(p))
												providers.Remove(pr);
									}
									else if (p is [.., '*'])
									{
										p = p.Trim('*');

										foreach (var pr in providers.ToList())
											if (pr.StartsWith(p))
												providers.Remove(pr);
									}
									else
										providers.Remove(p);
								}
								else
									providers.Add(provider);
								break;
							}
						}
					}

					settings.Providers = providers.ToArray();
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
							},
						}
					},
					{
						"NET80",
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
							},
						}
					},
				});

			return json;
		}
	}
}
