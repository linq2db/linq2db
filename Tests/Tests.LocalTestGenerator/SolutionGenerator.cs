namespace Tests.LocalTestGenerator;

internal sealed class SolutionGenerator
{
	private readonly string _repoRoot;

	public SolutionGenerator(string repoRoot)
	{
		_repoRoot = repoRoot;
	}

	public void Generate(string generatedSolutionFile, IReadOnlyList<GeneratedProject> projects)
	{
		var sourceSolutionFile = Path.Combine(_repoRoot, "linq2db.slnx");
		var solution          = File.ReadAllText(sourceSolutionFile);

		var solutionEnd = solution.LastIndexOf("</Solution>", StringComparison.Ordinal);

		if (solutionEnd < 0)
			throw new InvalidOperationException("Cannot find solution insertion point in linq2db.slnx.");

		var index = solution.LastIndexOf("</Folder>", solutionEnd, StringComparison.Ordinal);

		if (index < 0)
			throw new InvalidOperationException("Cannot find Tests folder insertion point in linq2db.slnx.");

		index += "</Folder>".Length;

		var projectEntries = string.Concat(projects.Select(project =>
		{
			var relativePath = Path.GetRelativePath(_repoRoot, project.ProjectFile).Replace('\\', '/');
			return $"{Environment.NewLine}    <Project Path=\"{relativePath}\" />";
		}));

		var generatedFolder = $@"
  <Folder Name=""/Tests/Generated/"">{projectEntries}
  </Folder>
";

		solution = solution.Insert(index, generatedFolder);

		if (File.Exists(generatedSolutionFile)
			&& string.Equals(File.ReadAllText(generatedSolutionFile), solution, StringComparison.Ordinal))
			return;

		File.WriteAllText(generatedSolutionFile, solution);
	}
}
