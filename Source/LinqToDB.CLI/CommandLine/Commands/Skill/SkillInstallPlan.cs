using System.Collections.Generic;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// What installing one bundle into one directory would do.
	/// </summary>
	internal sealed class SkillInstallPlan
	{
		public required SkillBundle       Bundle    { get; init; }
		public required string            Directory { get; init; }
		public required SkillInstallState State     { get; init; }
		/// <summary>One-line explanation of a state other than <see cref="SkillInstallState.UpToDate"/>.</summary>
		public required string            Reason    { get; init; }
		/// <summary>Files that need to be written.</summary>
		public List<SkillFile>            Writes    { get; } = new();
		/// <summary>Files, installed earlier by the tool, that the bundle no longer contains.</summary>
		public List<string>               Deletes   { get; } = new();
		/// <summary>Things that block the install unless forced.</summary>
		public List<string>               Conflicts { get; } = new();
		/// <summary>Whether the manifest has to be (re)written.</summary>
		public bool                       WriteManifest { get; set; }
	}
}
