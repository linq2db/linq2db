using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using LinqToDB.CodeModel;
using LinqToDB.Data;
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
				DatabaseType.DB2LUW     => ProviderName.DB2LUW,
				DatabaseType.DB2zOS     => ProviderName.DB2zOS,
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

			// assert that all provided options handled
			if (options.Count > 0)
			{
				foreach (var kvp in options)
					Console.Error.WriteLine($"{Name} command miss '{kvp.Key.Name}' option handler");

				// throw exception as it is implementation bug, not bad input or other expected error
				throw new InvalidOperationException($"Not all options handled by {Name} command");
			}

			// perform scaffolding
			return Scaffold(settings, provider, connectionString, output, overwrite);
		}

		private int Scaffold(ScaffoldOptions settings, string provider, string connectionString, string output, bool overwrite)
		{
			using (var dc = GetConnection(provider, connectionString))
			{
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
			}

			return StatusCodes.SUCCESS;
		}

		private DataConnection GetConnection(string provider, string connectionString)
		{
			switch (provider)
			{
				case ProviderName.SQLite:
					provider = ProviderName.SQLiteClassic;
					break;
				case ProviderName.SqlServer:
					SqlServerTools.AutoDetectProvider = true;
					SqlServerTools.Provider           = SqlServerProvider.MicrosoftDataSqlClient;
					break;
				default:
					throw new InvalidOperationException($"Unsupported database provider: {provider}");
			}

			var dataProvider = DataConnection.GetDataProvider(provider, connectionString)
				?? throw new InvalidOperationException($"Cannot create database provider: {provider}");

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
