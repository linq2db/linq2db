using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

using LinqToDB.DataProvider.DB2;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Handles loading of external database provider assemblies.
	/// </summary>
	static class ExternalProviderLoader
	{
		public static bool LoadExternalProvider(ICliEnvironment environment, string provider, string? providerLocation)
		{
			if (providerLocation == null)
			{
				if (IsDB2FamilyProvider(provider))
				{
					environment.Error.WriteLine(
						"""
						Cannot locate IBM.Data.Db2.dll provider assembly.
						Due to huge size of it, we don't include Net.IBM.Data.Db2 provider into installation.
						You need to install it manually and specify provider path using '--provider-location <path_to_assembly>' option.
						Provider could be downloaded from:
						- for Windows: https://www.nuget.org/packages/Net.IBM.Data.Db2
						- for Linux: https://www.nuget.org/packages/Net.IBM.Data.Db2-lnx
						- for macOS: https://www.nuget.org/packages/Net.IBM.Data.Db2-osx
						""");
					return false;
				}

				return true;
			}

			if (!environment.FileExists(providerLocation))
			{
				environment.Error.WriteLine($"Provider assembly '{providerLocation}' not found.");
				return false;
			}

			var currentDirectory     = Environment.CurrentDirectory;
			var providerLocationPath = Path.GetFullPath(providerLocation);
			var providerDirectory    = Path.GetDirectoryName(providerLocationPath);

			try
			{
				if (!string.IsNullOrEmpty(providerDirectory))
					Environment.CurrentDirectory = providerDirectory;

				var assembly = new ExternalProviderLoadContext(providerDirectory).LoadFromAssemblyPath(providerLocationPath);

				if (IsDB2FamilyProvider(provider))
				{
					DB2Tools.AutoDetectProvider = true;

					var factory = FindProviderFactory(assembly, "DB2Factory");

					if (factory == null)
					{
						environment.Error.WriteLine($"Provider assembly '{providerLocation}' doesn't contain DB2Factory type.");
						return false;
					}

					DbProviderFactories.RegisterFactory("IBM.Data.DB2", factory);
				}
			}
			finally
			{
				Environment.CurrentDirectory = currentDirectory;
			}

			return true;
		}

		static Type? FindProviderFactory(Assembly assembly, string factoryTypeName)
		{
			foreach (var type in assembly.GetTypes())
			{
				if (string.Equals(type.Name, factoryTypeName, StringComparison.Ordinal)
					&& typeof(DbProviderFactory).IsAssignableFrom(type))
				{
					return type;
				}
			}

			return null;
		}

		static bool IsDB2FamilyProvider(string provider)
		{
			return string.Equals(provider, ProviderName.DB2,     StringComparison.OrdinalIgnoreCase)
				|| string.Equals(provider, ProviderName.DB2LUW,  StringComparison.OrdinalIgnoreCase)
				|| string.Equals(provider, ProviderName.DB2zOS,  StringComparison.OrdinalIgnoreCase)
				|| string.Equals(provider, ProviderName.Informix,    StringComparison.OrdinalIgnoreCase)
				|| string.Equals(provider, ProviderName.InformixDB2, StringComparison.OrdinalIgnoreCase);
		}

		sealed class ExternalProviderLoadContext(string? providerDirectory) : AssemblyLoadContext(isCollectible: false)
		{
			protected override Assembly? Load(AssemblyName assemblyName)
			{
				if (!string.IsNullOrEmpty(providerDirectory))
				{
					var assemblyPath = Path.Combine(providerDirectory, assemblyName.Name + ".dll");

					if (IsMatchingAssembly(assemblyPath, assemblyName))
						return LoadFromAssemblyPath(assemblyPath);
				}

				var nugetAssemblyPath = FindNuGetAssemblyPath(assemblyName);

				return nugetAssemblyPath != null ? LoadFromAssemblyPath(nugetAssemblyPath) : null;
			}

			static string? FindNuGetAssemblyPath(AssemblyName assemblyName)
			{
				if (assemblyName.Name == null)
					return null;

				var nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

				if (string.IsNullOrEmpty(nugetPackages))
					nugetPackages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

				var packageDirectory = Path.Combine(nugetPackages, assemblyName.Name.ToLowerInvariant());

				if (!Directory.Exists(packageDirectory))
					return null;

				var versionDirectories = Directory.GetDirectories(packageDirectory);

				Array.Sort(versionDirectories, static (left, right) => string.Compare(Path.GetFileName(right), Path.GetFileName(left), StringComparison.OrdinalIgnoreCase));

				foreach (var versionDirectory in versionDirectories)
				{
					foreach (var targetFramework in GetCompatibleTargetFrameworks())
					{
						var assemblyPath = Path.Combine(versionDirectory, "lib", targetFramework, assemblyName.Name + ".dll");

						if (IsMatchingAssembly(assemblyPath, assemblyName))
							return assemblyPath;
					}
				}

				return null;
			}

			static bool IsMatchingAssembly(string assemblyPath, AssemblyName requestedAssemblyName)
			{
				if (!File.Exists(assemblyPath))
					return false;

				var candidateAssemblyName = AssemblyName.GetAssemblyName(assemblyPath);

				return AssemblyName.ReferenceMatchesDefinition(requestedAssemblyName, candidateAssemblyName);
			}

			static IEnumerable<string> GetCompatibleTargetFrameworks()
			{
				for (var version = Environment.Version.Major; version >= 5; version--)
					yield return string.Create(CultureInfo.InvariantCulture, $"net{version}.0");

				yield return "netstandard2.1";
				yield return "netstandard2.0";
			}
		}
	}
}
