using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using LinqToDB.CodeModel;
using LinqToDB.Data;
using LinqToDB.DataProvider.DB2;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Metadata;
using LinqToDB.Naming;
using LinqToDB.Scaffold;

namespace LinqToDB.CLI
{
	partial class ScaffoldCommand : CliCommand
	{
		public override int Execute(
			CLIController                  controller,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs)
		{
			// processed on controller level already
			options.Remove(General.Import);

			// scaffold settings object initialization
			var settings = ProcessScaffoldOptions(options);
			if (settings == null)
				return StatusCodes.INVALID_ARGUMENTS;

			// restart if other arch requested
			if (options.Remove(General.Architecture, out var value) && RestartIfNeeded((string)value!, rawArgs, out var status))
				return status.Value;

			// process remaining utility-specific (general) options

			// output folder
			var output = Directory.GetCurrentDirectory();
			if (options.Remove(General.Output, out value)) output = (string)value!;
			
			// overwrite existing files
			var overwrite = false;
			if (options.Remove(General.Overwrite, out value)) overwrite = (bool)value!;

			options.Remove(General.Provider, out value);
			var providerName = Enum.Parse<DatabaseType>((string)value!);
			var provider     = providerName switch
			{
				DatabaseType.Access     => ProviderName.Access,
				DatabaseType.DB2        => ProviderName.DB2,
				DatabaseType.Firebird   => ProviderName.Firebird,
				DatabaseType.Informix   => ProviderName.Informix,
				DatabaseType.SQLServer  => ProviderName.SqlServer,
				DatabaseType.MySQL      => ProviderName.MySql,
				DatabaseType.Oracle     => ProviderName.Oracle,
				DatabaseType.PostgreSQL => ProviderName.PostgreSQL,
				DatabaseType.SqlCe      => ProviderName.SqlCe,
				DatabaseType.SQLite     => ProviderName.SQLite,
				DatabaseType.Sybase     => ProviderName.Sybase,
				DatabaseType.SapHana    => ProviderName.SapHana,
				_                       => throw new InvalidOperationException($"Unsupported provider: {providerName}")
			};

			options.Remove(General.ConnectionString, out value);
			var connectionString = (string)value!;

			options.Remove(General.ProviderLocation, out value);
			var providerLocation = (string?)value;

			// assert that all provided options handled
			if (options.Count > 0)
			{
				foreach (var kvp in options)
					Console.Error.WriteLine($"{Name} command miss '{kvp.Key.Name}' option handler");

				// throw exception as it is implementation bug, not bad input or other expected error
				throw new InvalidOperationException($"Not all options handled by {Name} command");
			}

			// perform scaffolding
			return Scaffold(settings, provider, providerLocation, connectionString, output, overwrite);
		}

		private int Scaffold(ScaffoldOptions settings, string provider, string? providerLocation, string connectionString, string output, bool overwrite)
		{
			using var dc = GetConnection(provider, providerLocation, connectionString);
			if (dc == null)
				return StatusCodes.EXPECTED_ERROR;

			var generator  = new Scaffolder(LanguageProviders.CSharp, HumanizerNameConverter.Instance, settings);
			var dataModel  = generator.LoadDataModel(dc);
			var sqlBuilder = dc.DataProvider.CreateSqlBuilder(dc.MappingSchema);
			var files      = generator.GenerateCodeModel(
				sqlBuilder,
				dataModel,
				MetadataBuilders.GetAttributeBasedMetadataBuilder(generator.Language, sqlBuilder),
				SqlBoolEqualityConverter.Create(generator.Language));
			var sourceCode = generator.GenerateSourceCode(files);

			Directory.CreateDirectory(output);

			for (var i = 0; i < sourceCode.Length; i++)
			{
				// TODO: add file name normalization/deduplication?
				var fileName = $@"{output}\{sourceCode[i].FileName}";
				if (File.Exists(fileName) && !overwrite)
				{
					Console.WriteLine($"File '{fileName}' already exists. Specify '--overwrite true' option if you want to ovrerwrite existing files");
					return StatusCodes.EXPECTED_ERROR;
				}

				File.WriteAllText(fileName, sourceCode[i].Code);
			}

			return StatusCodes.SUCCESS;
		}

		private DataConnection? GetConnection(string provider, string? providerLocation, string connectionString)
		{
			// general rules:
			// - specify specific provider used by tool if linq2db supports multiple providers for database
			// - if multiple dialects (versions) of db supported - make sure version detection enabled
			// other considerations:
			// - generate error for databases with windows-only support (e.g. access, sqlce)
			// - allow user to specify provider discovery hints (e.g. provider path) for unmanaged providers
			switch (provider)
			{
				case ProviderName.SQLite:
					provider = ProviderName.SQLiteClassic;
					break;
				case ProviderName.SqlServer:
					SqlServerTools.AutoDetectProvider = true;
					SqlServerTools.Provider = SqlServerProvider.MicrosoftDataSqlClient;
					break;
				case ProviderName.Firebird:
					// TODO: don't forget to add versioning here after Firebird versioning feature merged
					break;
				case ProviderName.MySql:
					// TODO: remove provider hint after MySQL.Data support removed
					provider = ProviderName.MySqlConnector;
					break;
				case ProviderName.Oracle:
					OracleTools.AutoDetectProvider = true;
					provider = ProviderName.OracleManaged;
					break;
				case ProviderName.PostgreSQL:
					PostgreSQLTools.AutoDetectProvider = true;
					break;
				case ProviderName.Sybase:
					provider = ProviderName.SybaseManaged;
					break;
				case ProviderName.SqlCe:
				{
					if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						Console.Error.WriteLine($"SQL Server Compact Edition not supported on non-Windows platforms");
						return null;
					}

					var assemblyPath = providerLocation ?? Path.Combine(Environment.GetEnvironmentVariable(IntPtr.Size == 4 ? "ProgramFiles(x86)" : "ProgramFiles")!, @"Microsoft SQL Server Compact Edition\v4.0\Private\System.Data.SqlServerCe.dll");
					if (!File.Exists(assemblyPath))
					{
						Console.Error.WriteLine(@$"Cannot locate Server Compact Edition installation.
Probed location: {assemblyPath}.
Possible reasons:
1. SQL Server CE not installed => install SQL CE runtime (e.g. from here https://www.microsoft.com/en-us/download/details.aspx?id=30709)
2. SQL Server CE runtime architecture doesn't match process architecture => add '--architecture x86' or '--architecture x64' scaffold option
3. SQL Server CE runtime has custom location => specify path to System.Data.SqlServerCe.dll using '--provider-location <path_to_assembly>' option");
						return null;
					}

					var assembly = Assembly.LoadFrom(assemblyPath);
					DbProviderFactories.RegisterFactory("System.Data.SqlServerCe.4.0", assembly.GetType("System.Data.SqlServerCe.SqlCeProviderFactory")!);
					break;
				}
				case ProviderName.SapHana:
				{
					var isOdbc = connectionString.Contains("HDBODBC", StringComparison.OrdinalIgnoreCase);
					if (!isOdbc && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						Console.Error.WriteLine($"Only ODBC provider for SAP HANA supported on non-Windows platforms. Provided connection string doesn't look like HANA ODBC connection string.");
						return null;
					}

					provider = isOdbc ? ProviderName.SapHanaOdbc : ProviderName.SapHanaNative;

					if (!isOdbc)
					{
						var assemblyPath = providerLocation ?? Path.Combine(Environment.GetEnvironmentVariable(IntPtr.Size == 4 ? "ProgramFiles(x86)" : "ProgramFiles")!, @"sap\hdbclient\dotnetcore\v2.1\Sap.Data.Hana.Core.v2.1.dll");
						if (!File.Exists(assemblyPath))
						{
							Console.Error.WriteLine(@$"Cannot locate SAP HANA native client installation.
Probed location: {assemblyPath}.
Possible reasons:
1. HDB client not installed => install HDB client for .net core
2. HDB architecture doesn't match process architecture => add '--architecture x86' or '--architecture x64' scaffold option
3. HDB client installed at custom location => specify path to Sap.Data.Hana.Core.v2.1.dll using '--provider-location <path_to_assembly>' option");
							return null;
						}

						var assembly = Assembly.LoadFrom(assemblyPath);
						DbProviderFactories.RegisterFactory("Sap.Data.Hana", assembly.GetType("Sap.Data.Hana.HanaFactory")!);
					}
					break;
				}
				case ProviderName.Informix:
				case ProviderName.DB2:
				{
					if (provider == ProviderName.Informix)
						provider = ProviderName.InformixDB2;
					else
						DB2Tools.AutoDetectProvider = true;

					if (providerLocation == null || !File.Exists(providerLocation))
					{
						// we cannot add 90 Megabytes (compressed size) of native provider for single db just because we can
						Console.Error.WriteLine(@$"Cannot locate IBM.Data.DB2.Core.dll provider assembly.
Due to huge size of it, we don't include IBM.Data.DB2 provider into installation.
You need to install it manually and specify provider path using '--provider-location <path_to_assembly>' option.
Provider could be downloaded from:
- for Windows: https://www.nuget.org/packages/IBM.Data.DB2.Core
- for Linux: https://www.nuget.org/packages/IBM.Data.DB2.Core-lnx
- for macOS: https://www.nuget.org/packages/IBM.Data.DB2.Core-osx");
						return null;
					}

					var assembly = Assembly.LoadFrom(providerLocation);
					DbProviderFactories.RegisterFactory("IBM.Data.DB2", assembly.GetType("IBM.Data.DB2.Core.DB2Factory")!);
					break;
				}
				case ProviderName.Access:
				{
					if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						Console.Error.WriteLine($"MS Access not supported on non-Windows platforms");
						return null;
					}

					var isOleDb = connectionString.Contains("Microsoft.Jet.OLEDB", StringComparison.OrdinalIgnoreCase)
						|| connectionString.Contains("Microsoft.ACE.OLEDB", StringComparison.OrdinalIgnoreCase);

					if (!isOleDb)
						provider = ProviderName.AccessOdbc;

					break;
				}
				default:
					Console.Error.WriteLine($"Unsupported database provider: {provider}");
					return null;
			}

			var dataProvider = DataConnection.GetDataProvider(provider, connectionString);
			if (dataProvider == null)
			{
				Console.Error.WriteLine($"Cannot create database provider: {provider}");
				return null;
			}

			return new DataConnection(dataProvider, connectionString);
		}

		/// <summary>
		/// Restart scaffolding in process with specified architecture if restart required.
		/// </summary>
		/// <param name="requestedArch">New process architecture.</param>
		/// <param name="args">Command line arguments for current invocation.</param>
		/// <param name="status">Return code from child process.</param>
		/// <returns><c>true</c> if scaffold restarted in child process with specific arch.</returns>
		private bool RestartIfNeeded(string requestedArch, string[] args, [NotNullWhen(true)] out int? status)
		{
			status = null;

			// currently we support multiarch only for Windows
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Console.Out.WriteLine($"'{General.Architecture.Name}' parameter ignored for non-Windows system");
				return false;
			}

			string? exeName = null;
			if (requestedArch == "x86" && RuntimeInformation.ProcessArchitecture == Architecture.X64)
			{
				exeName = "dotnet-linq2db.win-x86.exe";
			}
			else if (requestedArch == "x64" && RuntimeInformation.ProcessArchitecture == Architecture.X86)
			{
				exeName = "dotnet-linq2db.win-x64.exe";
			}

			if (exeName == null)
			{
				return false;
			}

			// build full path to executable
			// we must use dll path as exe executed from other folder
			var exePath = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location)!, exeName);

			var childProcess = new Process();

			childProcess.StartInfo.FileName               = exePath;
			childProcess.StartInfo.UseShellExecute        = false;
			childProcess.StartInfo.RedirectStandardOutput = true;
			childProcess.StartInfo.RedirectStandardError  = true;
			childProcess.StartInfo.CreateNoWindow         = true;
			childProcess.StartInfo.WorkingDirectory       = Directory.GetCurrentDirectory();

			// let .net handle arguments escaping
			foreach (var arg in args)
				childProcess.StartInfo.ArgumentList.Add(arg);

			childProcess.OutputDataReceived += ChildProcess_OutputDataReceived;
			childProcess.ErrorDataReceived  += ChildProcess_ErrorDataReceived;

			childProcess.Start();

			childProcess.BeginOutputReadLine();
			childProcess.BeginErrorReadLine ();

			// IMPORTANT: don't use WaitForExitAsync till net6+ migration due to buggy implementation in earlier versions
			childProcess.WaitForExit();

			childProcess.OutputDataReceived -= ChildProcess_OutputDataReceived;
			childProcess.ErrorDataReceived  -= ChildProcess_ErrorDataReceived;

			status = childProcess.ExitCode;

			return true;
		}

		private void ChildProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
				Console.Error.WriteLine(e.Data);
		}

		private void ChildProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
				Console.Out.WriteLine(e.Data);
		}
	}
}
