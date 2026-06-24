using Tests.LocalTestGenerator;

var repoRoot              = FindRepoRoot(AppContext.BaseDirectory);
var generatorRoot         = Path.Combine(repoRoot, "Tests", "Tests.LocalTestGenerator");
var generatedSolutionFile = Path.Combine(repoRoot, "linq2db.local-tests.generated.slnx");
var generatedProjectsRoot = Path.Combine(repoRoot, "Tests", "Generated");
var configurationFiles    = ConfigurationFileLookup.GetConfigurationFiles(generatorRoot);
var settingsProvider      = new TestSettingsProvider(configurationFiles);
var projectGenerator      = new ProjectGenerator(repoRoot);
var solutionGenerator     = new SolutionGenerator(repoRoot);
var targetFrameworks      = projectGenerator.GetTargetFrameworkConfigurations();
var grouping              = ProviderResourceGrouper.Group(settingsProvider, targetFrameworks);
var generated             = projectGenerator.Generate(generatedProjectsRoot, grouping.Groups);

solutionGenerator.Generate(generatedSolutionFile, generated);

Console.WriteLine($"Repo root                 : {repoRoot}");
Console.WriteLine($"DataProviders.json        : {configurationFiles.DataProvidersJsonFile}");
Console.WriteLine($"UserDataProviders.json    : {configurationFiles.UserDataProvidersJsonFile ?? "<not found>"}");
Console.WriteLine($"Generated solution        : {generatedSolutionFile}");
Console.WriteLine($"Generated projects folder : {generatedProjectsRoot}");
Console.WriteLine();

foreach (var warning in grouping.Warnings.Distinct(StringComparer.Ordinal))
	Console.WriteLine($"Warning: {warning}");

foreach (var warning in projectGenerator.Warnings.Distinct(StringComparer.Ordinal))
	Console.Error.WriteLine($"Warning: {warning}");

Console.WriteLine("Groups:");
Console.WriteLine();

foreach (var group in grouping.Groups)
{
	Console.WriteLine(group.ProjectName);
	Console.WriteLine($"  Resource  : {group.ResourceKey.DisplayName}");
	Console.WriteLine($"  Providers : {string.Join(", ", group.ProviderNames)}");
	Console.WriteLine();
}

static string FindRepoRoot(string startDirectory)
{
	var directory = new DirectoryInfo(startDirectory);

	while (directory != null)
	{
		if (File.Exists(Path.Combine(directory.FullName, "linq2db.slnx")))
			return directory.FullName;

		directory = directory.Parent;
	}

	throw new InvalidOperationException("Repository root with linq2db.slnx not found.");
}
