using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using LinqToDB.CodeModel;
using LinqToDB.Data;
using LinqToDB.DataProvider.DB2;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Extensions;
using LinqToDB.Metadata;
using LinqToDB.Naming;
using LinqToDB.Scaffold;
using LinqToDB.Schema;

namespace LinqToDB.CommandLine
{
	partial class ScaffoldCommand : CliCommand
	{
		public override int Execute(
			CliController controller,
			string[] rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string> unknownArgs)
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
				DatabaseType.Access          => ProviderName.Access,
				DatabaseType.DB2             => ProviderName.DB2,
				DatabaseType.Firebird        => ProviderName.Firebird,
				DatabaseType.Informix        => ProviderName.Informix,
				DatabaseType.SQLServer       => ProviderName.SqlServer,
				DatabaseType.MySQL           => ProviderName.MySql,
				DatabaseType.Oracle          => ProviderName.Oracle,
				DatabaseType.PostgreSQL      => ProviderName.PostgreSQL,
				DatabaseType.SqlCe           => ProviderName.SqlCe,
				DatabaseType.SQLite          => ProviderName.SQLite,
				DatabaseType.Sybase          => ProviderName.Sybase,
				DatabaseType.SapHana         => ProviderName.SapHana,
				DatabaseType.ClickHouseMySql => ProviderName.ClickHouseMySql,
				DatabaseType.ClickHouseHttp  => ProviderName.ClickHouseClient,
				DatabaseType.ClickHouseTcp   => ProviderName.ClickHouseOctonica,
				DatabaseType.Custom          => settings.ProviderOptions.ProviderName ?? throw new InvalidOperationException($"Please specify 'provider-name' option to use custom provider"),
				_                            => throw new InvalidOperationException($"Unsupported provider: {providerName}")
			};

			options.Remove(General.ConnectionString, out value);
			var connectionString = (string)value!;

			options.Remove(General.AdditionalConnectionString, out value);
			var additionalConnectionString = (string?)value;

			options.Remove(General.Interceptors, out value);
			var interceptorsPath = (string?)value;


			// assert that all provided options handled
			if (options.Count > 0)
			{
				foreach (var kvp in options)
					Console.Error.WriteLine($"{Name} command miss '{kvp.Key.Name}' option handler");

				// throw exception as it is implementation bug, not bad input or other expected error
				throw new InvalidOperationException($"Not all options handled by {Name} command");
			}

			// load interceptors from T4 template or assembly
			ScaffoldInterceptors? interceptors = null;
			if (interceptorsPath != null)
			{
				(var res, interceptors) = LoadInterceptors(interceptorsPath, settings);
				if (res != StatusCodes.SUCCESS)
					return res;
			}

			// perform scaffolding
			return Scaffold(settings, interceptors, connectionString, additionalConnectionString, output, overwrite);
		}

		private int Scaffold(
			ScaffoldOptions settings,
			ScaffoldInterceptors? interceptors,
			string connectionString,
			string? additionalConnectionString,
			string output,
			bool overwrite)
		{
			using var dc  = GetConnection(settings.ProviderOptions, connectionString, additionalConnectionString, out var secondaryConnection);
			using var sdc = secondaryConnection;
			if (dc == null)
				return StatusCodes.EXPECTED_ERROR;

			var language = LanguageProviders.CSharp;

			var legacyProvider   = new LegacySchemaProvider(dc, settings.Schema, language);
			ISchemaProvider      schemaProvider       = legacyProvider;
			ITypeMappingProvider typeMappingsProvider = legacyProvider;

			if (sdc != null)
			{
				var secondLegacyProvider = new LegacySchemaProvider(sdc, settings.Schema, language);
				schemaProvider = new MergedAccessSchemaProvider(legacyProvider, secondLegacyProvider);
				typeMappingsProvider = new AggregateTypeMappingsProvider(legacyProvider, secondLegacyProvider);
			}

			var generator  = new Scaffolder(LanguageProviders.CSharp, HumanizerNameConverter.Instance, settings, interceptors);
			var dataModel  = generator.LoadDataModel(schemaProvider, typeMappingsProvider);
			var sqlBuilder = dc.DataProvider.CreateSqlBuilder(dc.MappingSchema, dc.Options);
			var files      = generator.GenerateCodeModel(
				sqlBuilder,
				dataModel,
				MetadataBuilders.GetMetadataBuilder(generator.Language, settings.DataModel.Metadata),
				new ProviderSpecificStructsEqualityFixer(generator.Language));
			var sourceCode = generator.GenerateSourceCode(dataModel, files);

			Directory.CreateDirectory(output);

			for (var i = 0; i < sourceCode.Length; i++)
			{
				// TODO: add file name normalization/deduplication?
				var fileName = Path.Combine(output, sourceCode[i].FileName);
				if (File.Exists(fileName) && !overwrite)
				{
					Console.WriteLine($"File '{fileName}' already exists. Specify '--overwrite true' option if you want to ovrerwrite existing files");
					return StatusCodes.EXPECTED_ERROR;
				}

				File.WriteAllText(fileName, sourceCode[i].Code);
			}

			return StatusCodes.SUCCESS;
		}

		private DataConnection? GetConnection(ProviderOptions providerOptions, string connectionString, string? additionalConnectionString, out DataConnection? secondaryConnection)
		{
			secondaryConnection = null;
			var returnSecondary = false;
			var provider = providerOptions.ProviderName;
			var providerLocation = providerOptions.ProviderLocation;
			// general rules:
			// - specify specific provider used by tool if linq2db supports multiple providers for database
			// - if multiple dialects (versions) of db supported - make sure version detection enabled
			// other considerations:
			// - generate error for databases with windows-only support (e.g. access, sqlce)
			// - allow user to specify provider discovery hints (e.g. provider path) for unmanaged providers
			switch (provider)
			{
				case ProviderName.ClickHouseMySql:
				case ProviderName.ClickHouseClient:
				case ProviderName.ClickHouseOctonica:
				case ProviderName.SqlServer:
					break;
				case ProviderName.SQLite:
					provider = ProviderName.SQLiteClassic;
					break;
				case ProviderName.Firebird          :
					break;
				case ProviderName.MySql             :
					provider = "MySqlConnector";
					break;
				case ProviderName.Oracle            :
					OracleTools.AutoDetectProvider = true;
					provider = ProviderName.OracleManaged;
					break;
				case ProviderName.PostgreSQL:
					PostgreSQLTools.AutoDetectProvider = true;
					break;
				case ProviderName.Sybase:
					provider = ProviderName.SybaseManaged;
					break;
				case ProviderName.SqlCe             :
				{
					if (!OperatingSystem.IsWindows())
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
					if (!isOdbc && !OperatingSystem.IsWindows())
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
						Console.Error.WriteLine(@$"Cannot locate IBM.Data.Db2.dll provider assembly.
Due to huge size of it, we don't include Net.IBM.Data.Db2 provider into installation.
You need to install it manually and specify provider path using '--provider-location <path_to_assembly>' option.
Provider could be downloaded from:
- for Windows: https://www.nuget.org/packages/Net.IBM.Data.Db2
- for Linux: https://www.nuget.org/packages/Net.IBM.Data.Db2-lnx
- for macOS: https://www.nuget.org/packages/Net.IBM.Data.Db2-osx");
						return null;
					}

					var assembly = Assembly.LoadFrom(providerLocation);
					DbProviderFactories.RegisterFactory("IBM.Data.DB2", assembly.GetType($"{assembly.GetName().Name}.DB2Factory")!);
					break;
				}
				case ProviderName.Access:
				{
					if (!OperatingSystem.IsWindows())
					{
						Console.Error.WriteLine($"MS Access not supported on non-Windows platforms");
						return null;
					}

					var isOleDb = connectionString.Contains("Microsoft.Jet.OLEDB", StringComparison.OrdinalIgnoreCase)
						|| connectionString.Contains("Microsoft.ACE.OLEDB", StringComparison.OrdinalIgnoreCase);

					if (!isOleDb)
						provider = ProviderName.AccessOdbc;

					if (additionalConnectionString == null)
						Console.Out.WriteLine($"WARNING: it is recommended to use '--additional-connection <secondary_connection>' option with Access for better results");
					else
					{
						var isSecondaryOleDb = additionalConnectionString.Contains("Microsoft.Jet.OLEDB", StringComparison.OrdinalIgnoreCase)
						|| additionalConnectionString.Contains("Microsoft.ACE.OLEDB", StringComparison.OrdinalIgnoreCase);

						if (isOleDb == isSecondaryOleDb)
						{
							Console.Error.WriteLine($"Main and secondary connection strings must use different providers. One should be OLE DB provider and another ODBC provider.");
							return null;
						}

						var secondaryProvider = isSecondaryOleDb ? ProviderName.Access : ProviderName.AccessOdbc;

						var secondaryDataProvider = DataConnection.GetDataProvider(secondaryProvider, additionalConnectionString);
						if (secondaryDataProvider == null)
						{
							Console.Error.WriteLine($"Cannot create database provider '{provider}' for secondary connection");
							return null;
						}

						secondaryConnection = new DataConnection(secondaryDataProvider, additionalConnectionString);
						// to simplify things for caller (no need to detect connection type)
						// returned connection should be OLE DB and additional - ODBC
						returnSecondary = isSecondaryOleDb;
					}

					break;
				}
				default:
				{
					//Console.Error.WriteLine($"Unsupported database provider: {provider}");
					//return null;
					if (providerLocation == null || !File.Exists(providerLocation))
					{
						Console.Error.WriteLine(@$"Cannot locate custom provider assembly.");
						return null;
					}
					var dpIType = typeof(DataProvider.IDataProvider);
					RedirectAllAssemblyVersion(dpIType.Assembly);
					var assembly = Assembly.LoadFrom(providerLocation);
					MethodInfo? detectorMethod = null;
					object? detectorInstance = null;
					if (!string.IsNullOrWhiteSpace(providerOptions.ProviderDetectorClass))
					{
						var detectorType = assembly.GetType(providerOptions.ProviderDetectorClass);
						if (detectorType != null && detectorType.IsClass)
						{
							if (!string.IsNullOrWhiteSpace(providerOptions.ProviderDetectorMethod))
								detectorMethod = FindMethod(bf=> detectorType.GetMethod(providerOptions.ProviderDetectorMethod, bf), out detectorInstance);
							else
								detectorMethod = FindMethod(bf => detectorType.GetMethods(bf)
									.FirstOrDefault(y =>
									{
										var pp =y.GetParameters();
										return pp.Length == 1 && pp[0].ParameterType == typeof(ConnectionOptions) && y.ReturnType.IsAssignableFrom(dpIType);
									}), out detectorInstance);

						}
					}
					else
						detectorMethod = FindMethod(bf => assembly.GetTypes()
						.SelectMany(x => x.GetMethods(bf)
							.Where(y =>
							{
								var pp =y.GetParameters();
								return pp.Length == 1 && pp[0].ParameterType == typeof(ConnectionOptions)
								&& y.ReturnType.IsAssignableFrom(dpIType);
							})).FirstOrDefault(), out detectorInstance);
					if (detectorMethod == null)
						throw new InvalidOperationException("Cannot find Detector method!");
					DataConnection.InsertProviderDetector(x => detectorMethod.Invoke(detectorInstance, new object[] { x }) as DataProvider.IDataProvider);

					break;
				}
			}

			var dataProvider = DataConnection.GetDataProvider(provider, connectionString);
			if (dataProvider == null)
			{
				Console.Error.WriteLine($"Cannot create database provider: {provider}");
				secondaryConnection?.Dispose();
				return null;
			}

			var dc = new DataConnection(dataProvider, connectionString);

			if (secondaryConnection != null && returnSecondary)
			{
				var tmp             = secondaryConnection;
				secondaryConnection = dc;
				return tmp;
			}

			return dc;
		}

		private static MethodInfo? FindMethod(Func<BindingFlags, MethodInfo?> finder, out object? detectorInstance)
		{
			detectorInstance = null;
			var detectorMethod = finder(BindingFlags.Static | BindingFlags.Public); 
			//detectorType.GetMethod(providerOptions.ProviderDetectorMethod, BindingFlags.Static | BindingFlags.Public);
			if (detectorMethod == null)
			{
				detectorMethod = finder(BindingFlags.Instance | BindingFlags.Public);
				//detectorType.GetMethod(providerOptions.ProviderDetectorMethod, BindingFlags.Instance | BindingFlags.Public);
				if (detectorMethod != null)
				{
					try
					{
						detectorInstance = Activator.CreateInstance(detectorMethod.DeclaringType!);
					}
					catch { }
					if (detectorInstance == null)
						detectorMethod = null;
				}
			}

			return detectorMethod;
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
			if (!OperatingSystem.IsWindows())
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

			using var childProcess = new Process();

			childProcess.StartInfo.FileName = exePath;
			childProcess.StartInfo.UseShellExecute = false;
			childProcess.StartInfo.RedirectStandardOutput = true;
			childProcess.StartInfo.RedirectStandardError = true;
			childProcess.StartInfo.CreateNoWindow = true;
			childProcess.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

			// let .net handle arguments escaping
			foreach (var arg in args)
				childProcess.StartInfo.ArgumentList.Add(arg);

			childProcess.OutputDataReceived += ChildProcess_OutputDataReceived;
			childProcess.ErrorDataReceived += ChildProcess_ErrorDataReceived;

			childProcess.Start();

			childProcess.BeginOutputReadLine();
			childProcess.BeginErrorReadLine();

			// IMPORTANT: don't use WaitForExitAsync till net6+ migration due to buggy implementation in earlier versions
			childProcess.WaitForExit();

			childProcess.OutputDataReceived -= ChildProcess_OutputDataReceived;
			childProcess.ErrorDataReceived -= ChildProcess_ErrorDataReceived;

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

		//private static void RedirectAssembly(string shortName, Version targetVersion, string publicKeyToken)
		//{
		//	Assembly? handler(object? sender, ResolveEventArgs args)
		//	{
		//		// Use latest strong name & version when trying to load SDK assemblies
		//		var requestedAssembly = new AssemblyName(args.Name!);
		//		if (requestedAssembly.Name != shortName)
		//			return null;

		//		Debug.WriteLine("Redirecting assembly load of " + args.Name + ",\tloaded by " + (args.RequestingAssembly == null ? "(unknown)" : args.RequestingAssembly.FullName));

		//		requestedAssembly.Version = targetVersion;
		//		requestedAssembly.SetPublicKeyToken(new AssemblyName("x, PublicKeyToken=" + publicKeyToken).GetPublicKeyToken());
		//		requestedAssembly.CultureInfo = CultureInfo.InvariantCulture;

		//		AppDomain.CurrentDomain.AssemblyResolve -= handler;

		//		return Assembly.Load(requestedAssembly);
		//	}

		//	AppDomain.CurrentDomain.AssemblyResolve += handler;
		//}

		private static void RedirectAllAssemblyVersion(Assembly assembly)
		{
			Assembly? handler(object? sender, ResolveEventArgs args)
			{
				var newAssemblyName =  new AssemblyName(args.Name!);
				if (newAssemblyName.Name == assembly.GetName().Name)
				{
					AppDomain.CurrentDomain.AssemblyResolve -= handler;

					return assembly;
				}
				return Assembly.Load(newAssemblyName);
			}

			AppDomain.CurrentDomain.AssemblyResolve += handler;
		}
	}
}
