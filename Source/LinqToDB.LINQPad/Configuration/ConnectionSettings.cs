using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

using LINQPad.Extensibility.DataContext;

using LinqToDB.LINQPad.Json;

#if WITH_ISERIES
using LinqToDB.DataProvider.DB2iSeries;
#endif

using PN = LinqToDB.ProviderName;

namespace LinqToDB.LINQPad;

// IMPORTANT:
// settings, marked by [JsonIgnore] stored in default LINQPad connection option properties and must be copied manually on settings save/load
internal sealed class ConnectionSettings
{
	#region Save/Load/Migrate
	/// <summary>
	/// Starting from v5 release we store json string in settings instead of multiple XML nodes to simplify settings management.
	/// </summary>
	private const string SETTINGS_NODE = "SettingsV5";

	private static readonly JsonSerializerOptions _jsonOptions;

#pragma warning disable CA1810 // Initialize reference type static fields inline
	static ConnectionSettings()
#pragma warning restore CA1810 // Initialize reference type static fields inline
	{
		_jsonOptions = new()
		{
			// deserialization options: use permissive options
			AllowTrailingCommas         = true,
			ReadCommentHandling         = JsonCommentHandling.Skip,
			PropertyNameCaseInsensitive = true,
			// serialization options
			PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
			WriteIndented               = false,
			DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
		};

		// register IReadOnlySet<T> converter factory
		_jsonOptions.Converters.Add(IReadOnlySetConverter<string>.Factory);
	}

	/// <summary>
	/// Load connection settings from LINQPad connection object.
	/// </summary>
	public static ConnectionSettings Load(IConnectionInfo cxInfo)
	{
		ConnectionSettings? settings = null;

		var json = GetString(cxInfo, SETTINGS_NODE);

		if (json != null)
		{
			settings = JsonSerializer.Deserialize<ConnectionSettings>(json, _jsonOptions);

			if (settings != null)
			{
				settings.Connection    ??= new();
				settings.Schema        ??= new();
				settings.Scaffold      ??= new();
				settings.LinqToDB      ??= new();
				settings.StaticContext ??= new();
			}
		}

		settings ??= Legacy.Load(cxInfo);

		// load data from predefined IConnectionInfo properties
		// Main reason we use predefined properties is to provide connection configuration options to LINQPad so it could use it
		// for raw database access functionality
		settings.Connection.ConnectionString        = cxInfo.DatabaseInfo.CustomCxString;
		settings.Connection.Server                  = cxInfo.DatabaseInfo.Server;
		settings.Connection.DatabaseName            = cxInfo.DatabaseInfo.Database;
		settings.Connection.DbVersion               = cxInfo.DatabaseInfo.DbVersion;
		settings.Connection.EncryptConnectionString = cxInfo.DatabaseInfo.EncryptCustomCxString;
		settings.Connection.ProviderFactory         = cxInfo.DatabaseInfo.Provider;
		settings.Connection.DisplayName             = cxInfo.DisplayName;
		settings.Connection.IsProduction            = cxInfo.IsProduction;
		settings.Connection.Persistent              = cxInfo.Persist;

		settings.Scaffold.Pluralize  = !cxInfo.DynamicSchemaOptions.NoPluralization;
		settings.Scaffold.Capitalize = !cxInfo.DynamicSchemaOptions.NoCapitalization;

		settings.StaticContext.ConfigurationPath   = cxInfo.AppConfigPath;
		settings.StaticContext.ContextTypeName     = cxInfo.CustomTypeInfo.CustomTypeName;
		settings.StaticContext.ContextAssemblyPath = cxInfo.CustomTypeInfo.CustomAssemblyPath;

		// manually decrypt secondary connection
		if (settings.Connection.EncryptConnectionString && settings.Connection.SecondaryConnectionString != null)
			settings.Connection.SecondaryConnectionString = cxInfo.Decrypt(settings.Connection.SecondaryConnectionString);

		return settings;

		// TODO: debug method to reset modifications
		//return LoadLegacySettings(cxInfo);
	}

	/// <summary>
	/// Save connection settings to LINQPad connection object.
	/// This method should be called from <see cref="DriverHelper.ShowConnectionDialog(IConnectionInfo, bool)"/> method only.
	/// </summary>
	public void Save(IConnectionInfo cxInfo)
	{
		// encrypt sencondary connection string manually
		if (Connection.EncryptConnectionString && Connection.SecondaryConnectionString != null)
			Connection.SecondaryConnectionString = cxInfo.Encrypt(Connection.SecondaryConnectionString);

		// save data, stored in predefined IConnectionInfo properties to them
		cxInfo.DatabaseInfo.CustomCxString        = Connection.ConnectionString;
		cxInfo.DatabaseInfo.Provider              = Connection.ProviderFactory;
		cxInfo.DatabaseInfo.EncryptCustomCxString = Connection.EncryptConnectionString;
		cxInfo.DatabaseInfo.DbVersion             = Connection.DbVersion;
		cxInfo.DatabaseInfo.Database              = Connection.DatabaseName;
		cxInfo.DatabaseInfo.Server                = Connection.Server;

		cxInfo.DynamicSchemaOptions.NoPluralization  = !Scaffold.Pluralize;
		cxInfo.DynamicSchemaOptions.NoCapitalization = !Scaffold.Capitalize;

		cxInfo.DisplayName                       = Connection.DisplayName;
		cxInfo.IsProduction                      = Connection.IsProduction;
		cxInfo.Persist                           = Connection.Persistent;
		cxInfo.AppConfigPath                     = StaticContext.ConfigurationPath;
		cxInfo.CustomTypeInfo.CustomTypeName     = StaticContext.ContextTypeName;
		cxInfo.CustomTypeInfo.CustomAssemblyPath = StaticContext.ContextAssemblyPath;

		var json = JsonSerializer.Serialize(this, _jsonOptions);
		SetString(cxInfo, SETTINGS_NODE, json);
	}

	/// <summary>
	/// Legacy options migration support.
	/// </summary>
	private static class Legacy
	{
		// list item separators for legacy options
		private static readonly char[] _listSeparators = [',', ';'];

		// legacy options
		private const string ProviderName             = "providerName";
		private const string ProviderPath             = "providerPath";
		private const string ConnectionString         = "connectionString";
		private const string ExcludeRoutines          = "excludeRoutines";
		private const string ExcludeFKs               = "excludeFKs";
		private const string IncludeSchemas           = "includeSchemas";
		private const string ExcludeSchemas           = "excludeSchemas";
		private const string IncludeCatalogs          = "includeCatalogs";
		private const string ExcludeCatalogs          = "excludeCatalogs";
		private const string OptimizeJoins            = "optimizeJoins";
		private const string UseProviderSpecificTypes = "useProviderSpecificTypes";
		private const string CommandTimeout           = "commandTimeout";
		private const string CustomConfiguration      = "customConfiguration";

		public static ConnectionSettings Load(IConnectionInfo cxInfo)
		{
			var settings           = new ConnectionSettings();
			settings.Connection    = new();
			settings.Schema        = new();
			settings.Scaffold      = new();
			settings.LinqToDB      = new();
			settings.StaticContext = new();

			// 1. ProviderName migration

			// old provider name option replaced with two options: database and database provider
			settings.Connection.Provider = GetString(cxInfo, ProviderName);

			// used to distinguish new connection dialog from migration
			var isNew = settings.Connection.Provider == null;

			// this native oracle provider was removed long time ago and not supported in v5 too
			if (string.Equals(settings.Connection.Provider, PN.OracleNative, StringComparison.Ordinal))
				settings.Connection.Provider = PN.OracleManaged;

			// switch contains only provider names, used by pre-v5 driver
			settings.Connection.Database = settings.Connection.Provider switch
			{

				PN.AccessOdbc         => PN.Access,
				"MySqlConnector"      => PN.MySql,
				PN.SybaseManaged      => PN.Sybase,
				PN.SQLiteClassic      => PN.SQLite,
				PN.InformixDB2        => PN.Informix,
				PN.SapHanaNative
					or PN.SapHanaOdbc => PN.SapHana,
				PN.OracleManaged      => PN.Oracle,
				// preserve same name
				PN.Firebird
					or PN.Access
					or PN.PostgreSQL
					or PN.DB2LUW
					or PN.DB2zOS
					or PN.SqlServer
#if WITH_ISERIES
					or DB2iSeriesProviderName.DB2
#endif
					or PN.SqlCe       => settings.Connection.Provider,
				_                     => null,
			};

			// 2. IncludeSchemas, ExcludeSchemas, IncludeCatalogs and ExcludeCatalogs migration

			// 2. convert comma/semicolon-separated strings with schemas/catalogs to list + flag
			var strValue = GetString(cxInfo, ExcludeSchemas);
			var schemas = strValue == null ? null : new HashSet<string>(strValue.Split(_listSeparators, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
			if (schemas != null && schemas.Count > 0)
				settings.Schema.IncludeSchemas = false;
			else
			{
				strValue = GetString(cxInfo, IncludeSchemas);
				schemas = strValue == null ? null : [.. strValue.Split(_listSeparators, StringSplitOptions.RemoveEmptyEntries)];
				if (schemas != null && schemas.Count > 0)
					settings.Schema.IncludeSchemas = true;
			}

			settings.Schema.Schemas = schemas?.AsReadOnly();

			strValue = GetString(cxInfo, ExcludeCatalogs);
			var catalogs = strValue == null ? null : new HashSet<string>(strValue.Split(_listSeparators, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
			if (catalogs != null && catalogs.Count > 0)
				settings.Schema.IncludeCatalogs = false;
			else
			{
				strValue = GetString(cxInfo, IncludeCatalogs);
				catalogs = strValue == null ? null : [.. strValue.Split(_listSeparators, StringSplitOptions.RemoveEmptyEntries)];
				if (catalogs != null && catalogs.Count > 0)
					settings.Schema.IncludeCatalogs = true;
			}

			settings.Schema.Catalogs = catalogs?.AsReadOnly();

			// 3. ExcludeRoutines migration

			settings.Schema.LoadAggregateFunctions
				= settings.Schema.LoadScalarFunctions
				= settings.Schema.LoadTableFunctions
				= settings.Schema.LoadProcedures
				= !GetBoolean(cxInfo, ExcludeRoutines, true).Value;

			// 4. ExcludeFKs migration
			settings.Schema.LoadForeignKeys = !GetBoolean(cxInfo, ExcludeFKs, false).Value;

			// 5. ProviderPath migration
			settings.Connection.ProviderPath = GetString(cxInfo, ProviderPath);

			// 6. CommandTimeout migration
			// note that in pre-v5 it was non-nullable option so it wasn't possible to use default db/provider timeout
			settings.Connection.CommandTimeout = GetInt32(cxInfo, CommandTimeout);

			// 7. ConnectionString migration
			// note that in practice pre-v4 never stored connection in custom field and used CustomCxString as storage
			settings.Connection.ConnectionString = GetString(cxInfo, ConnectionString);
			if (!string.IsNullOrWhiteSpace(settings.Connection.ConnectionString))
				cxInfo.DatabaseInfo.CustomCxString = settings.Connection.ConnectionString;

			// 8. OptimizeJoins migration
			settings.LinqToDB.OptimizeJoins = GetBoolean(cxInfo, OptimizeJoins, true).Value;

			// 9. UseProviderSpecificTypes migration
			settings.Scaffold.UseProviderTypes = GetBoolean(cxInfo, UseProviderSpecificTypes, false).Value;

			// 10. CustomConfiguration migration
			settings.StaticContext.ConfigurationName = GetString(cxInfo, CustomConfiguration);

			// https://github.com/linq2db/linq2db.LINQPad/issues/89
			settings.Scaffold.AsIsNames = !isNew;

			// ignored options:
			// useCustomFormatter - removed in v5
			// normalizeNames - not used in pre-v5 and v5 (never used?)

			return settings;
		}
	}

	[return: NotNullIfNotNull(nameof(defaultValue))]
	private static int? GetInt32(IConnectionInfo cxInfo, XName name, int? defaultValue = null)
	{
		var strValue = GetString(cxInfo, name);

		if (strValue != null && int.TryParse(strValue, NumberStyles.None, CultureInfo.InvariantCulture, out var intValue))
			return intValue;

		return defaultValue;
	}

	[return: NotNullIfNotNull(nameof(defaultValue))]
	private static bool? GetBoolean(IConnectionInfo cxInfo, XName name, bool? defaultValue = null)
	{
		var strValue = GetString(cxInfo, name);
		return string.Equals(strValue, "true", StringComparison.Ordinal) ? true : string.Equals(strValue, "false", StringComparison.Ordinal) ? false : defaultValue;
	}

	[return: NotNullIfNotNull(nameof(defaultValue))]
	private static string? GetString(IConnectionInfo cxInfo, XName name, string? defaultValue = null) => cxInfo.DriverData.Element(name)?.Value ?? defaultValue;

	private static void SetString(IConnectionInfo cxInfo, XName name, string? value)
	{
		if (value != null)
			cxInfo.DriverData.SetElementValue(name, value);
		else
			cxInfo.DriverData.Element(name)?.Remove();
	}
#endregion

	public ConnectionOptions     Connection    { get; set; } = null!;
	public SchemaOptions         Schema        { get; set; } = null!;
	public ScaffoldOptions       Scaffold      { get; set; } = null!;
	public LinqToDbOptions       LinqToDB      { get; set; } = null!;
	public StaticContextOptions  StaticContext { get; set; } = null!;

	public sealed class ConnectionOptions
	{
		/// <summary>
		/// Database identifier. Usually generic name from <see cref="PN"/>.
		/// </summary>
		public string? Database { get; set; }

		/// <summary>
		/// Database provider identifier. Specific name from <see cref="PN"/>.
		/// </summary>
		public string? Provider { get; set; }

		/// <summary>
		/// Database provider assembly path.
		/// </summary>
		[JsonIgnore]
		public string? ProviderPath
		{
			get => IntPtr.Size == 4 ? ProviderPathx86 ?? ProviderPathx64 : ProviderPathx64 ?? ProviderPathx86;
			set
			{
				if (IntPtr.Size == 4)
					ProviderPathx86 = value;
				else
					ProviderPathx64 = value;
			}
		}

		/// <summary>
		/// Database provider assembly path.
		/// </summary>
		public string? ProviderPathx86 { get; set; }

		/// <summary>
		/// Database provider assembly path.
		/// </summary>
		public string? ProviderPathx64 { get; set; }

		/// <summary>
		/// Command timeout. <see langword="null"/> for provider/database default timeout.
		/// </summary>
		public int? CommandTimeout { get; set; }

		/// <summary>
		/// Database provider name for secondary schema connection.
		/// </summary>
		public string? SecondaryProvider { get; set; }

		/// <summary>
		/// Secondary schema connection string.
		/// </summary>
		public string? SecondaryConnectionString { get; set; }

		/// <summary>
		/// User-defined connection name.
		/// Stored in <see cref="IConnectionInfo.DisplayName"/>.
		/// </summary>
		[JsonIgnore]
		public string? DisplayName { get; set; }

		/// <summary>
		/// Marks connected database as containing production data.
		/// Stored in <see cref="IConnectionInfo.IsProduction"/>.
		/// </summary>
		[JsonIgnore]
		public bool IsProduction { get; set; }

		/// <summary>
		/// Marks connection as persistent (saved before restarts).
		/// Stored in <see cref="IConnectionInfo.Persist"/>.
		/// </summary>
		[JsonIgnore]
		public bool Persistent { get; set; }

		/// <summary>
		/// Connection string. Stored in <see cref="IDatabaseInfo.CustomCxString"/>.
		/// </summary>
		[JsonIgnore]
		public string? ConnectionString { get; set; }

		/// <summary>
		/// Returns <see cref="ConnectionString"/> property value with resolved LINQPad password manager tokens.
		/// </summary>
		/// <returns></returns>
		public string? GetFullConnectionString         () => PasswordManager.ResolvePasswordManagerFields(ConnectionString);

		/// <summary>
		/// Returns <see cref="SecondaryConnectionString"/> property value with resolved LINQPad password manager tokens.
		/// </summary>
		/// <returns></returns>
		public string? GetFullSecondaryConnectionString() => PasswordManager.ResolvePasswordManagerFields(SecondaryConnectionString);

		/// <summary>
		/// Stored in <see cref="IDatabaseInfo.Provider"/>.
		/// </summary>
		[JsonIgnore]
		public string? ProviderFactory { get; set; }

		/// <summary>
		/// Database information (<see cref="DbConnection.DataSource"/>).
		/// Stored in <see cref="IDatabaseInfo.Server"/>.
		/// </summary>
		[JsonIgnore]
		public string? Server { get; set; }

		/// <summary>
		/// Database information (<see cref="DbConnection.Database"/>).
		/// Stored in <see cref="IDatabaseInfo.Database"/>.
		/// </summary>
		[JsonIgnore]
		public string? DatabaseName { get; set; }

		/// <summary>
		/// Database information (<see cref="DbConnection.ServerVersion"/>).
		/// Stored in <see cref="IDatabaseInfo.DbVersion"/>.
		/// </summary>
		[JsonIgnore]
		public string? DbVersion { get; set; }

		/// <summary>
		/// Instructs LINQPad to encrypt <see cref="ConnectionString"/> value.
		/// Also instruct us to encrypt <see cref="SecondaryConnectionString"/> value.
		/// Stored in <see cref="IDatabaseInfo.EncryptCustomCxString"/>.
		/// </summary>
		[JsonIgnore]
		public bool EncryptConnectionString { get; set; }
	}

	public sealed class SchemaOptions
	{
		/// <summary>
		/// Include/exclude schemas, specified by <see cref="Schemas"/> option.
		/// </summary>
		public bool IncludeSchemas { get; set; }

		/// <summary>
		/// List of schemas to include/exclude (defined by <see cref="IncludeSchemas"/> option).
		/// </summary>
		public IReadOnlySet<string>? Schemas { get; set; }

		/// <summary>
		/// Include/exclude catalogs, specified by <see cref="Catalogs"/> option.
		/// </summary>
		public bool IncludeCatalogs { get; set; }

		/// <summary>
		/// List of catalogs to include/exclude (defined by <see cref="IncludeCatalogs"/> option).
		/// </summary>
		public IReadOnlySet<string>? Catalogs { get; set; }

		/// <summary>
		/// Populate stored procedures.
		/// </summary>
		public bool LoadProcedures { get; set; }

		/// <summary>
		/// Populate table functions.
		/// </summary>
		public bool LoadTableFunctions { get; set; }

		/// <summary>
		/// Populate scalar functions.
		/// </summary>
		public bool LoadScalarFunctions { get; set; }

		/// <summary>
		/// Populate aggregate functions.
		/// </summary>
		public bool LoadAggregateFunctions { get; set; }

		/// <summary>
		/// Populate foreign keys.
		/// </summary>
		public bool LoadForeignKeys { get; set; }
	}

	public sealed class ScaffoldOptions
	{
		/// <summary>
		/// Use provider data types.
		/// </summary>
		public bool UseProviderTypes { get; set; }

		/// <summary>
		/// Map FixedString(X) to <see cref="string"/> for ClickHouse.
		/// </summary>
		public bool ClickHouseFixedStringAsString { get; set; }

		/// <summary>
		/// Enables pluralization context table property name and collection-type association property name.
		/// Stored in <see cref="IDynamicSchemaOptions.NoPluralization"/>.
		/// </summary>
		[JsonIgnore]
		public bool Pluralize { get; set; }

		/// <summary>
		/// Enables capitalization of table column properties.
		/// Stored in <see cref="IDynamicSchemaOptions.NoCapitalization"/>.
		/// </summary>
		[JsonIgnore]
		public bool Capitalize { get; set; }

		/// <summary>
		/// When set, default database object name normalization rules disabled (except modification set
		/// by <see cref="Pluralize"/> and <see cref="Capitalize"/> options).
		/// </summary>
		public bool AsIsNames { get; set; }
	}

	public sealed class LinqToDbOptions
	{
		/// <summary>
		/// Value for <see cref="Common.Configuration.Linq.OptimizeJoins"/> Linq To DB setting.
		/// </summary>
		public bool OptimizeJoins { get; set; }
	}

	public sealed class StaticContextOptions
	{
		/// <summary>
		/// Name of custom configuration (connection string name), passed to context constructor.
		/// </summary>
		public string? ConfigurationName { get; set; }

		/// <summary>
		/// Path to custom configuration file.
		/// For LINQPad 5 it should be in app.config format, for .NET Core versions - in appsettings.json format.
		/// Stored in <see cref="IConnectionInfo.AppConfigPath"/>.
		/// </summary>
		[JsonIgnore] // strored in linqpad storage
		public string? ConfigurationPath { get; set; }

#if NETFRAMEWORK
		/// <summary>
		/// Path to appsettings.json configuration file for LINQPad 5.
		/// We cannot store it in <see cref="IConnectionInfo.AppConfigPath"/> as LINQPad will try to use
		/// it as app.config file and fail.
		/// </summary>
		public string? LocalConfigurationPath { get; set; }
#endif

		/// <summary>
		/// Full name of data context class (namespace + class name) in custom context assembly.
		/// Stored in <see cref="ICustomTypeInfo.CustomTypeName"/>.
		/// </summary>
		[JsonIgnore]
		public string? ContextTypeName { get; set; }

		/// <summary>
		/// Full path to custom context assembly.
		/// Stored in <see cref="ICustomTypeInfo.CustomTypeName"/>.
		/// </summary>
		[JsonIgnore]
		public string? ContextAssemblyPath { get; set; }
	}
}
