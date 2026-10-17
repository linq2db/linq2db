using System;
using System.IO;
using System.Security.Cryptography;

namespace LinqToDB.CommandLine.Commands.Skill
{
	internal static class SkillHash
	{
		/// <summary>
		/// SHA-256 of the content with CRLF normalised to LF. Packaged files use CRLF, while a consumer repository
		/// that normalises line endings checks the very same files out as LF; that must not look like a local edit.
		/// </summary>
		public static string Compute(byte[] content)
		{
			var normalized = new byte[content.Length];
			var length     = 0;

			for (var i = 0; i < content.Length; i++)
			{
				if (content[i] == (byte)'\r' && i + 1 < content.Length && content[i + 1] == (byte)'\n')
					continue;

				normalized[length++] = content[i];
			}

			return Convert.ToHexString(SHA256.HashData(normalized.AsSpan(0, length))).ToLowerInvariant();
		}

		public static string ComputeFile(string path)
		{
			return Compute(File.ReadAllBytes(path));
		}
	}
}
