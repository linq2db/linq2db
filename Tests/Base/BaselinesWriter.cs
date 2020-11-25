using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework.Internal;

namespace Tests
{
	internal class BaselinesWriter
	{
		// used to detect baseline overwrites by another test(case)
		// case-insensitive to support windoze file system
		private static readonly ISet<string> _baselines = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		internal static void Write(string baselinesPath, string baseline)
		{
			var test = TestExecutionContext.CurrentContext.CurrentTest;

			var context = GetTestContextName(test);

#if NET472
			var target = "net472";
#elif NETCOREAPP2_1
			var target = "core21";
#elif NETCOREAPP3_1
			var target = "core31";
#elif NET5_0
			var target = "net50";
#else
#error "Build Target must be specified here."
#endif

			if (context == null)
				return;

			var fixturePath = Path.Combine(baselinesPath, target, context, test.ClassName.Replace('.', Path.DirectorySeparatorChar));
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
				.Replace(">" , $"0x{(ushort)'>':X4}")
				.Replace("<" , $"0x{(ushort)'<':X4}")
				.Replace("/" , $"0x{(ushort)'/':X4}")
				.Replace(":" , $"0x{(ushort)':':X4}")
				;
		}

		private static string? GetTestContextName(Test test)
		{
			var parameters = test.Method.GetParameters();
			for (var i = 0; i < parameters.Length; i++)
			{
				var attr = parameters[i].GetCustomAttributes<DataSourcesBaseAttribute>(true);

				if (attr.Length != 0)
				{
					return (string)test.Arguments[i];
				}
			}

			return null;
		}
	}
}
