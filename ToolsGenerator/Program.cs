using System;
using System.Linq;
using System.IO;
using System.Xml.Linq;

namespace ToolsGenerator
{
	class Program
	{
		static int Main(string[] args)
		{
			if (args != null && args.Length > 0)
				DefaultVersion = args[0];

			var nugetDir = Environment.CurrentDirectory;

			while (!Directory.Exists(Path.Combine(nugetDir, "NuGet")))
			{
				var dir = nugetDir.TrimEnd('\\', '/');
				var idx = dir.LastIndexOf('\\');

				if (idx < 0)
					idx = dir.LastIndexOf('/');

				if (idx < 0)
				{
					Console.WriteLine("Could not find NuGet directory.");
					return -1;
				}

				nugetDir = dir.Substring(0, idx);
			}

			nugetDir = Path.Combine(nugetDir, "NuGet");

			CreateToolsFile(nugetDir, "linq2db.t4models.nuspec", "LinqToDB.Tools.ttinclude",
				@"<#@ assembly name=""$(SolutionDir).tools\linq2db.t4models\linq2db.dll"" #>");

			CreateProviderFile (nugetDir, "Access");
			CreateProviderFile (nugetDir, "Firebird",                 "FirebirdSql.Data.FirebirdClient.dll");
			CreateProviderFile (nugetDir, "MySql",                    "MySql.Data.dll");
			CreateProviderFile (nugetDir, "SQLite",                   "System.Data.SQLite.dll");
			CreateProviderFile2(nugetDir, "Oracle.Managed", "Oracle", "Oracle.ManagedDataAccess.dll");
			CreateProviderFile2(nugetDir, "Oracle.x64",     "Oracle", "Oracle.DataAccess.dll");
			CreateProviderFile2(nugetDir, "Oracle.x86",     "Oracle", "Oracle.DataAccess.dll");
			CreateProviderFile (nugetDir, "SqlServer");
			CreateProviderFile3(nugetDir, "SqlServer", "SqlServer.SqlTypes", "SqlServer.SqlTypes", "Microsoft.SqlServer.Types.dll");
			CreateProviderFile (nugetDir, "Sybase",                   "Sybase.AdoNet2.AseClient.dll");
			CreateProviderFile (nugetDir, "SapHana",                  "Sap.Data.Hana.v4.5.dll");
			CreateProviderFile (nugetDir, "DB2",                      "IBM.Data.DB2.dll");
			CreateProviderFile (nugetDir, "Informix",                 "IBM.Data.Informix.dll");
			CreateProviderFile (nugetDir, "PostgreSQL",               "Npgsql.dll", "System.Threading.Tasks.Extensions.dll");

			CreateProviderFile(nugetDir, "SqlCe",
				@"<#@ assembly name=""$(SolutionDir).tools\System.Data.SqlServerCe.dll"" #>");

			return 0;
		}

		static void CreateToolsFile(string nugetDir, string specFile, string toolsFile, params string[] tools)
		{
			var version = GetVersion(Path.Combine(nugetDir, specFile));

			using (var tf = File.CreateText(Path.Combine(nugetDir, toolsFile)))
				foreach (var tool in tools)
					tf.WriteLine(tool, version);

			Console.WriteLine("{0}...OK", toolsFile);
		}

		static void CreateProviderFile3(string nugetDir, string nuspec, string provider, string baseProvider, params string[] tools)
		{
			var specFile = string.Format("linq2db.{0}.nuspec", nuspec);
			var version  = GetVersion(Path.Combine(nugetDir, specFile));

			CreateToolsFile(
				nugetDir,
				specFile,
				string.Format("LinqToDB.{0}.Tools.ttinclude", provider),
				tools
					.Select(t =>
					{
						if (t.StartsWith("<#@"))
							return t;
						else
							return string.Format(
								@"<#@ assembly name=""$(SolutionDir).tools\linq2db.t4models\{2}"" #>",
								nuspec,
								version,
								t);
					})
					.Concat(
					new[]
					{
						@"<#@ include file=""LinqToDB.Tools.ttinclude"" #>",
						string.Format(@"<#@ include file=""LinqToDB.{0}.ttinclude"" #>", baseProvider),
					}).ToArray());
		}

		static void CreateProviderFile2(string nugetDir, string provider, string baseProvider, params string[] tools)
		{
			CreateProviderFile3(nugetDir, provider, provider, baseProvider, tools);
		}

		static void CreateProviderFile(string nugetDir, string provider, params string[] tools)
		{
			CreateProviderFile2(nugetDir, provider, provider, tools);
		}

		private static string DefaultVersion = null;
		static string GetVersion(string specFile)
		{
			if (!string.IsNullOrEmpty(DefaultVersion))
				return DefaultVersion;

			var version =
			(
				from e in XDocument.Load(specFile).Root.Elements()
				where e.Name.LocalName == "metadata"
				from v in e.Elements()
				where v.Name.LocalName == "version"
				select v
			).First();

			return version.Value;
		}
	}
}
