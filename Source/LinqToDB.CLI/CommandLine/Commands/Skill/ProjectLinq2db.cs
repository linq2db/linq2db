using System.Collections.Generic;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// The linq2db package a restored project resolved.
	/// </summary>
	/// <param name="Project">Project file.</param>
	/// <param name="Version">Resolved linq2db version.</param>
	/// <param name="PackageDirectories">Candidate package directories (one per NuGet package folder).</param>
	internal sealed record ProjectLinq2db(string Project, string Version, IReadOnlyList<string> PackageDirectories);
}
