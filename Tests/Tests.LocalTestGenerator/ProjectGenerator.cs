using System.Text;
using System.Xml.Linq;

namespace Tests.LocalTestGenerator
{
	internal sealed record GeneratedProject(string Name, string ProjectFile);
	internal sealed record TargetFrameworkConfiguration(string TargetFramework, string ConfigurationSection);

	internal sealed class ProjectGenerator
	{
		private readonly string       _repoRoot;
		private readonly List<string> _warnings = [];

		public ProjectGenerator(string repoRoot)
		{
			_repoRoot = repoRoot;
		}

		public IReadOnlyList<string> Warnings => _warnings;

		public IReadOnlyList<TargetFrameworkConfiguration> GetTargetFrameworkConfigurations()
		{
			var testsDirectoryProps = XDocument.Load(Path.Combine(_repoRoot, "Tests", "Directory.Build.props"));
			var targetFrameworks = testsDirectoryProps
				.Descendants("TargetFrameworks")
				.First(static e => e.Attribute("Condition") == null)
				.Value
				.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			return targetFrameworks
				.Select(static tf => new TargetFrameworkConfiguration(tf, ToConfigurationSection(tf)))
				.ToArray();
		}

		public IReadOnlyList<GeneratedProject> Generate(string generatedProjectsRoot, IReadOnlyList<ProviderResourceGroup> groups)
		{
			_warnings.Clear();

			Directory.CreateDirectory(generatedProjectsRoot);
			if (!TryWriteGeneratedProps(generatedProjectsRoot))
				return [];

			var generated = new List<GeneratedProject>();
			var expectedDirectories = groups
				.Select(static g => g.ProjectName)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			foreach (var staleDirectory in Directory.EnumerateDirectories(generatedProjectsRoot))
			{
				if (!expectedDirectories.Contains(Path.GetFileName(staleDirectory)))
					TryDeleteDirectory(staleDirectory);
			}

			foreach (var group in groups.OrderBy(static g => g.ProjectName, StringComparer.Ordinal))
			{
				var projectDirectory = Path.Combine(generatedProjectsRoot, group.ProjectName);
				Directory.CreateDirectory(projectDirectory);

				var projectFile = Path.Combine(projectDirectory, $"{group.ProjectName}.csproj");

				if (!TryWriteProject(projectFile, group))
					continue;

				generated.Add(new GeneratedProject(group.ProjectName, projectFile));
			}

			return generated;
		}

		private void TryDeleteDirectory(string directory)
		{
			try
			{
				Directory.Delete(directory, recursive: true);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				_warnings.Add($"Cannot delete stale generated project directory '{directory}': {ex.Message}");
			}
		}

		private bool TryWriteProject(string projectFile, ProviderResourceGroup group)
		{
			try
			{
				File.WriteAllText(projectFile, CreateProjectXml(Path.GetDirectoryName(projectFile)!, group), Encoding.UTF8);
				UserDataProvidersWriter.Write(Path.Combine(Path.GetDirectoryName(projectFile)!, "UserDataProviders.json"), group);
				return true;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				_warnings.Add($"Cannot update generated project '{projectFile}': {ex.Message}");
				return false;
			}
		}

		private bool TryWriteGeneratedProps(string generatedProjectsRoot)
		{
			try
			{
				var props = XDocument.Load(Path.Combine(_repoRoot, "Tests", "linq2db.TestProjects.props"));

				ShiftRelativePaths(props.Root!);

				foreach (var import in props.Descendants().Where(static e => e.Name.LocalName == "Import"))
				{
					var project = import.Attribute("Project");
					if (project != null && IsBareRelativePath(project.Value))
						project.Value = @"..\" + project.Value;
				}

				File.WriteAllText(
					Path.Combine(generatedProjectsRoot, "linq2db.TestProjects.generated.props"),
					props + Environment.NewLine,
					Encoding.UTF8);

				return true;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				_warnings.Add($"Cannot update generated test projects props: {ex.Message}");
				return false;
			}
		}

		private string CreateProjectXml(string projectDirectory, ProviderResourceGroup group)
		{
			var sourceProjectDirectory = Path.Combine(_repoRoot, "Tests", "Linq");
			var project = XDocument.Load(Path.Combine(sourceProjectDirectory, "Tests.csproj"));
			var root = project.Root!;
			var compileExcludes = RemoveLocalCompileRemoves(root, sourceProjectDirectory, projectDirectory);

			ShiftRelativePaths(root);
			UseGeneratedTestProjectsProps(root);
			ConfigureGeneratedProject(root, group);
			AddGeneratedItems(root, group, compileExcludes);

			return project + Environment.NewLine;
		}

		private static IReadOnlyList<string> RemoveLocalCompileRemoves(
			XElement root,
			string   sourceProjectDirectory,
			string   projectDirectory)
		{
			var compileRemoves = root
				.Descendants()
				.Where(static e => e.Name.LocalName == "Compile")
				.Select(static e => new { Element = e, Remove = e.Attribute("Remove") })
				.Where(static e => e.Remove != null && !e.Remove.Value.Contains("$(", StringComparison.Ordinal))
				.ToArray();

			var excludes = new List<string>(compileRemoves.Length);
			foreach (var item in compileRemoves)
			{
				excludes.AddRange(RewritePathList(item.Remove!.Value, sourceProjectDirectory, projectDirectory));

				var parent = item.Element.Parent;
				item.Element.Remove();
				if (parent != null && !parent.Elements().Any())
					parent.Remove();
			}

			return excludes;
		}

		private static void UseGeneratedTestProjectsProps(XElement root)
		{
			foreach (var import in root.Descendants().Where(static e => e.Name.LocalName == "Import"))
			{
				var project = import.Attribute("Project");
				if (project != null && project.Value.EndsWith("linq2db.TestProjects.props", StringComparison.OrdinalIgnoreCase))
					project.Value = @"..\linq2db.TestProjects.generated.props";
			}
		}

		private static void ConfigureGeneratedProject(XElement root, ProviderResourceGroup group)
		{
			root.AddFirst(
				new XElement("PropertyGroup",
					new XElement("TargetFramework", group.TargetFramework),
					new XElement("TargetFrameworks", "")));

			var assemblyName = root
				.Descendants()
				.First(static e => e.Name.LocalName == "AssemblyName");
			assemblyName.Value = $"linq2db.{group.ProjectName}";

			var propertyGroup = assemblyName.Parent!;
			propertyGroup.Add(
				new XElement("RootNamespace", "GeneratedLocalTests"),
				new XElement("EnableDefaultCompileItems", "false"));
		}

		private static void AddGeneratedItems(XElement root, ProviderResourceGroup group, IReadOnlyList<string> compileExcludes)
		{
			var providersComment = string.Join(Environment.NewLine, group.ProviderNames.Select(static p => $"\t\t- {p}"));

			root.Add(
				new XComment($"{Environment.NewLine}\tGenerated local test project for provider configurations:{Environment.NewLine}{providersComment}{Environment.NewLine}\t"),
				new XElement("ItemGroup",
					new XElement("Compile",
						new XAttribute("Include", @"..\..\Linq\**\*.cs"),
						new XAttribute("Exclude", string.Join(';', compileExcludes)),
						new XAttribute("LinkBase", "Linq"))),
				new XElement("ItemGroup",
					new XElement("None",
						new XAttribute("Include", "UserDataProviders.json"),
						new XAttribute("CopyToOutputDirectory", "PreserveNewest"))));
		}

		private static void ShiftRelativePaths(XElement root)
		{
			foreach (var element in root.DescendantsAndSelf())
			{
				foreach (var attribute in element.Attributes())
				{
					if (IsPathAttribute(attribute.Name.LocalName))
						attribute.Value = ShiftRelativePathList(attribute.Value);
				}

				if (IsPathElement(element.Name.LocalName))
					element.Value = ShiftRelativePathList(element.Value);
			}
		}

		private static bool IsPathAttribute(string name)
		{
			return name is "Project" or "Include" or "Remove" or "Update" or "Exclude";
		}

		private static bool IsPathElement(string name)
		{
			return name is "HintPath" or "AssemblyOriginatorKeyFile";
		}

		private static string ShiftRelativePathList(string value)
		{
			return value.Contains(';', StringComparison.Ordinal)
				? string.Join(';', value.Split(';').Select(ShiftRelativePath))
				: ShiftRelativePath(value);
		}

		private static string ShiftRelativePath(string value)
		{
			return value
				.Replace("$(MSBuildThisFileDirectory)/../", @"$(MSBuildThisFileDirectory)/../../", StringComparison.Ordinal)
				.Replace(@"$(MSBuildThisFileDirectory)\..\", @"$(MSBuildThisFileDirectory)\..\..\", StringComparison.Ordinal) switch
			{
				var path when path.StartsWith(@"..\", StringComparison.Ordinal) => @"..\" + path,
				var path when path.StartsWith("../",  StringComparison.Ordinal) => "../"  + path,
				var path                                               => path,
			};
		}

		private static bool IsBareRelativePath(string path)
		{
			return !Path.IsPathFullyQualified(path)
				&& !path.StartsWith("$(", StringComparison.Ordinal)
				&& !path.StartsWith(@".\", StringComparison.Ordinal)
				&& !path.StartsWith("./",  StringComparison.Ordinal)
				&& !path.StartsWith(@"..\", StringComparison.Ordinal)
				&& !path.StartsWith("../",  StringComparison.Ordinal);
		}

		private static IEnumerable<string> RewritePathList(string value, string sourceDirectory, string targetDirectory)
		{
			return value.Split(';').Select(p => RewritePath(p, sourceDirectory, targetDirectory));
		}

		private static string RewritePath(string path, string sourceDirectory, string targetDirectory)
		{
			var fullPath = Path.GetFullPath(Path.Combine(sourceDirectory, path));
			return Path.GetRelativePath(targetDirectory, fullPath);
		}

		private static string ToConfigurationSection(string targetFramework)
		{
			return targetFramework switch
			{
				"net462"  => "NETFX",
				"net8.0"  => "NET80",
				"net9.0"  => "NET90",
				"net10.0" => "NET100",
				_ when targetFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase) =>
					"NET" + new string(targetFramework.Where(char.IsDigit).ToArray()),
				_ => throw new InvalidOperationException($"Unsupported target framework '{targetFramework}'."),
			};
		}
	}
}
