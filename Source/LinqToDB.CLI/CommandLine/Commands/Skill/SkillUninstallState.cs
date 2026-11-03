namespace LinqToDB.CommandLine.Commands.Skill
{
	internal enum SkillUninstallState
	{
		/// <summary>No such directory.</summary>
		NotInstalled,
		/// <summary>dotnet-package-skills owns the directory.</summary>
		ManagedExternally,
		/// <summary>The record of dotnet-package-skills cannot be read, so it is unknown who owns the directory.</summary>
		OwnershipUnknown,
		/// <summary>The directory exists but carries no marker of this tool.</summary>
		NotInstalledByTool,
		/// <summary>The tool installed it.</summary>
		Installed,
	}
}
