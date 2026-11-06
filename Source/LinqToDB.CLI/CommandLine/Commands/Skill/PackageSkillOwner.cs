using System;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>A skill folder recorded by dotnet-package-skills: the package (id as written) and the package version it came from.</summary>
	internal sealed record PackageSkillOwner(string Package, string Version)
	{
		/// <summary>The skill comes from the linq2db package itself.</summary>
		public bool IsLinq2db
		{
			get { return string.Equals(Package, "linq2db", StringComparison.OrdinalIgnoreCase); }
		}

		public override string ToString()
		{
			return $"{Package} {Version}";
		}
	}
}
