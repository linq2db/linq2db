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

		internal static void Write(string baselinesPath, string baseline)
		{
			var test = TestExecutionContext.CurrentContext.CurrentTest;

			_context = GetTestContextName(test);

			if (_context == null)
				return;

			var fixturePath = Path.Combine(baselinesPath, _context, test.ClassName!.Replace('.', Path.DirectorySeparatorChar));
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

#if NET462
			var target = "net462";
#elif NET472
			var target = "net472";
#elif NETCOREAPP3_1
			var target = "core31";
#elif NET6_0
			var target = "net60";
#elif NET7_0
			var target = "net70";
#elif NET8_0
			var target = "net80";
#else
#error "Build Target must be specified here."
#endif

			var fixturePath = Path.Combine(baselinesPath, target);

			Directory.CreateDirectory(fixturePath);

			var fileName = $"{_context}.{Environment.OSVersion.Platform}.Metrics.txt";

			var fullPath = Path.Combine(fixturePath, fileName);

			// split baselines in 5-line batches to simplify diff review on GH
			var lines = baseline.Split(new char[] {'\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			using var fs = File.Create(fullPath);
			using var sw = new StreamWriter(fs, Encoding.UTF8);

			for (var i = 0; i < lines.Length; i++)
			{
				sw.WriteLine(lines[i]);

				if (i % 5 == 4)
					sw.WriteLine();
			}
		}
	}
}
