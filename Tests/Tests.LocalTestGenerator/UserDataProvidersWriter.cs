using System.Text.Json;
using System.Text.Json.Serialization;

using Tests.Tools;

namespace Tests.LocalTestGenerator
{
	internal static class UserDataProvidersWriter
	{
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented          = true,
		};

		public static void Write(string fileName, ProviderResourceGroup group)
		{
			var settings = new SortedDictionary<string, TestSettings>(StringComparer.Ordinal);

			foreach (var section in group.Sections.OrderBy(static s => s.Key, StringComparer.Ordinal))
			{
				var source    = section.Value.SourceSettings;
				var providers = section.Value.Providers.ToArray();

				settings.Add(section.Key, new TestSettings
				{
					DisableRemoteContext = source.DisableRemoteContext,
					BaselinesPath        = source.BaselinesPath,
					StoreMetrics         = source.StoreMetrics,
					Providers            = providers,
					Skip                 = source.Skip,
					TraceLevel           = source.TraceLevel,
					DefaultConfiguration = providers.Contains(source.DefaultConfiguration, StringComparer.Ordinal)
						? source.DefaultConfiguration
						: providers.FirstOrDefault(),
					NoLinqService        = source.NoLinqService,
					Connections          = section.Value.Connections.ToDictionary(
						static c => c.Key,
						static c => new TestConnection
						{
							Provider         = c.Value.Provider,
							ConnectionString = c.Value.ConnectionString,
						},
						StringComparer.Ordinal),
				});
			}

			File.WriteAllText(fileName, JsonSerializer.Serialize(settings, JsonOptions));
		}
	}
}
