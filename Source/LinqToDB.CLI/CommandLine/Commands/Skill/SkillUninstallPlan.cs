using System.Collections.Generic;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// What removing one skill directory would do.
	/// </summary>
	internal sealed class SkillUninstallPlan
	{
		public required string               Name      { get; init; }
		public required string               Directory { get; init; }
		public required SkillUninstallState  State     { get; init; }
		/// <summary>One-line explanation of a state other than <see cref="SkillUninstallState.Installed"/>.</summary>
		public          string               Reason    { get; init; } = string.Empty;
		public          SkillManifest?       Manifest  { get; init; }
		/// <summary>Recorded files that still have the recorded content.</summary>
		public List<string>                  Unchanged { get; } = new();
		/// <summary>Recorded files whose content differs from the record.</summary>
		public List<string>                  Changed   { get; } = new();
		/// <summary>Files in the directory that the tool never installed.</summary>
		public List<string>                  Added     { get; } = new();
	}
}
