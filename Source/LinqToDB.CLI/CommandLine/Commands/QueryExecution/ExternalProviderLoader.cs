using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

using LinqToDB.DataProvider.DB2;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Handles loading of external database provider assemblies.
	/// </summary>
	static class ExternalProviderLoader
	{
		static readonly Lock                         _loadLock        = new();
		static readonly Dictionary<string, Assembly> _loadedAssemblies = new(StringComparer.OrdinalIgnoreCase);

		public static bool LoadExternalProvider(string provider, string? providerLocation, out string? error)
		{
			error = null;

			if (providerLocation == null)
			{
				if (IsDB2FamilyProvider(provider))
				{
					error = """
						Cannot locate IBM.Data.Db2.dll provider assembly.
						Due to huge size of it, we don't include Net.IBM.Data.Db2 provider into installation.
						You need to install it manually and specify provider path using '--provider-location <path_to_assembly>' option.
						Provider could be downloaded from:
						- for Windows: https://www.nuget.org/packages/Net.IBM.Data.Db2
						- for Linux: https://www.nuget.org/packages/Net.IBM.Data.Db2-lnx
						- for macOS: https://www.nuget.org/packages/Net.IBM.Data.Db2-osx
						""";
					return false;
				}

				return true;
			}

			var providerLocationPath = Path.GetFullPath(providerLocation);

			if (!File.Exists(providerLocationPath))
			{
				error = $"Provider assembly '{providerLocation}' not found.";
				return false;
			}

			lock (_loadLock)
			{
				if (!_loadedAssemblies.TryGetValue(providerLocationPath, out var assembly))
				{
					var currentDirectory  = Environment.CurrentDirectory;
					var providerDirectory = Path.GetDirectoryName(providerLocationPath);

					try
					{
						// Some external providers resolve native binaries and dependent assemblies relative to
						// the process current directory instead of the managed provider assembly path.
						//
						if (!string.IsNullOrEmpty(providerDirectory))
							Environment.CurrentDirectory = providerDirectory;

						assembly = new ExternalProviderLoadContext(providerDirectory).LoadFromAssemblyPath(providerLocationPath);
						_loadedAssemblies.Add(providerLocationPath, assembly);
					}
					finally
					{
						Environment.CurrentDirectory = currentDirectory;
					}
				}

				if (IsDB2FamilyProvider(provider))
				{
					DB2Tools.AutoDetectProvider = true;

					var factory = FindProviderFactory(assembly, "DB2Factory");

					if (factory == null)
					{
						error = $"Provider assembly '{providerLocation}' doesn't contain DB2Factory type.";
						return false;
					}

					DbProviderFactories.RegisterFactory("IBM.Data.DB2", factory);
				}
			}

			return true;

			static Type? FindProviderFactory(Assembly assembly, string factoryTypeName)
			{
				Type?[] types;

				try
				{
					types = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					types = ex.Types;
				}

				foreach (var type in types)
					if (type != null && string.Equals(type.Name, factoryTypeName, StringComparison.Ordinal) && typeof(DbProviderFactory).IsAssignableFrom(type))
						return type;

				return null;
			}
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

				var applicationAssemblyPath = Path.Combine(AppContext.BaseDirectory, assemblyName.Name + ".dll");

				if (IsMatchingAssembly(applicationAssemblyPath, assemblyName))
					return LoadFromAssemblyPath(applicationAssemblyPath);

				return null;
			}

			static bool IsMatchingAssembly(string assemblyPath, AssemblyName requestedAssemblyName)
			{
				if (!File.Exists(assemblyPath))
					return false;

				AssemblyName candidateAssemblyName;

				try
				{
					candidateAssemblyName = AssemblyName.GetAssemblyName(assemblyPath);
				}
				catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or IOException)
				{
					return false;
				}

				return AssemblyName.ReferenceMatchesDefinition(requestedAssemblyName, candidateAssemblyName);
			}
		}
	}
}
