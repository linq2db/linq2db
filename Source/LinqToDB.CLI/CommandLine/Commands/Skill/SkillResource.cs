using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace LinqToDB.CommandLine.Commands.Skill
{
	internal static class SkillResource
	{
		private const string ResourceName = "LinqToDB.CLI.SKILL.md";

		/// <summary>
		/// Reads the CLI skill as it is printed by <c>skill</c> and returned by the MCP <c>linq2db_skill</c> tool:
		/// without the YAML frontmatter that only skill loaders need.
		/// </summary>
		public static string ReadMarkdown()
		{
			return StripFrontmatter(ReadRaw());
		}

		/// <summary>
		/// Reads the embedded CLI skill file exactly as it is installed into a repository (with frontmatter).
		/// </summary>
		public static byte[] ReadRawBytes()
		{
			var assembly = typeof(SkillResource).Assembly;
			using var stream = assembly.GetManifestResourceStream(ResourceName)
				?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");
			using var memory = new MemoryStream();

			stream.CopyTo(memory);

			return memory.ToArray();
		}

		private static string ReadRaw()
		{
			using var reader = new StreamReader(new MemoryStream(ReadRawBytes()), Encoding.UTF8, true);

			return reader.ReadToEnd();
		}

		internal static string StripFrontmatter(string markdown)
		{
			if (!markdown.StartsWith("---", StringComparison.Ordinal))
				return markdown;

			var lineEnd = markdown.IndexOf('\n', StringComparison.Ordinal);
			if (lineEnd < 0 || !string.Equals(markdown.Substring(0, lineEnd).TrimEnd(), "---", StringComparison.Ordinal))
				return markdown;

			var position = lineEnd + 1;

			while (position < markdown.Length)
			{
				var relative = markdown.AsSpan(position).IndexOf('\n');
				var next     = relative < 0 ? -1 : position + relative;
				var end  = next < 0 ? markdown.Length : next;

				if (string.Equals(markdown.Substring(position, end - position).TrimEnd(), "---", StringComparison.Ordinal))
					return next < 0 ? string.Empty : markdown.Substring(next + 1).TrimStart('\r', '\n');

				if (next < 0)
					break;

				position = next + 1;
			}

			return markdown;
		}
	}
}
