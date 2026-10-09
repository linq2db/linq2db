namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// A file of a skill, addressed by a '/'-separated path relative to the skill directory.
	/// </summary>
	internal sealed record SkillFile(string Path, byte[] Content);
}
