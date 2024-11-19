using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using NUnit.Framework.Internal;

namespace Tests
{
	public sealed class BaselinesWriter
	{
		// used to detect baseline overwrites by another test(case)
		// case-insensitive to support windoze file system
		static readonly ISet<string> _baselines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		static string? _context;

		internal static void Write(string baselinesPath, string baseline, string? providerSuffix)
		{
			var test = TestExecutionContext.CurrentContext.CurrentTest;

			_context = GetTestContextName(test);

			if (_context == null)
				return;

			var fixturePath = Path.Combine(baselinesPath, _context + providerSuffix, test.ClassName!.Replace('.', Path.DirectorySeparatorChar));
			Directory.CreateDirectory(fixturePath);

			var fileName = $"{NormalizeFileName(test.FullName)}.sql";

			var fullPath = Path.Combine(fixturePath, fileName);

			if (!_baselines.Add(fullPath))
				throw new InvalidOperationException($"Baseline already in use: {fullPath}");

			File.WriteAllText(fullPath, baseline, Encoding.UTF8);
		}

		private static string NormalizeFileName(string name)
		{
			// " used in each test name, for now we just remove it
			return name
				.Replace("\"", string.Empty)
				.Replace("\\" , $"0x{(ushort)'\\':X4}")
				.Replace(">" , $"0x{(ushort)'>':X4}")
				.Replace("<" , $"0x{(ushort)'<':X4}")
				.Replace("/" , $"0x{(ushort)'/':X4}")
				.Replace(":" , $"0x{(ushort)':':X4}")
				.Replace("*" , $"0x{(ushort)'*':X4}")
				.Replace("?" , $"0x{(ushort)'?':X4}")
				;
		}

		private static string? GetTestContextName(Test test)
		{
			var parameters = test.Method!.GetParameters();

			for (var i = 0; i < parameters.Length; i++)
			{
				var attr = parameters[i].GetCustomAttributes<DataSourcesBaseAttribute>(true);

				if (attr.Length != 0)
				{
					return (string)test.Arguments[i]!;
				}
			}

			return null;
		}

		public static void WriteMetrics(string baselinesPath, string baseline)
		{
			if (_context == null)
				return;

			var target = TestUtils.GetConfigName();

			var fixturePath = Path.Combine(baselinesPath, target);

			Directory.CreateDirectory(fixturePath);

			var fileName = $"{_context}.{Environment.OSVersion.Platform}.Metrics.txt";

			var fullPath = Path.Combine(fixturePath, fileName);

			File.WriteAllText(fullPath, baseline);
		}
	}
}
