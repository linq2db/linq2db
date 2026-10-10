namespace LinqToDB.CommandLine.Commands.Skill
{
	internal enum SkillInstallState
	{
		/// <summary>Installed files match the bundle and the manifest.</summary>
		UpToDate,
		/// <summary>Nothing installed.</summary>
		Missing,
		/// <summary>Installed by the tool, but differs from the bundle (other version, or files missing).</summary>
		Stale,
		/// <summary>A file installed by the tool was edited locally.</summary>
		Modified,
		/// <summary>The directory exists, but the tool did not install it.</summary>
		Unmanaged,
	}
}
