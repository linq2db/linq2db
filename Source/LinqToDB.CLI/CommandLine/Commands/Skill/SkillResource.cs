using System;
using System.IO;
using System.Reflection;

namespace LinqToDB.CommandLine.Commands.Skill
{
	internal static class SkillResource
	{
		private const string ResourceName = "LinqToDB.CLI.SKILL.md";

		public static string ReadMarkdown()
		{
			var assembly = typeof(SkillResource).Assembly;
			using var stream = assembly.GetManifestResourceStream(ResourceName)
				?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");
			using var reader = new StreamReader(stream);

			return reader.ReadToEnd();
		}
	}
}
