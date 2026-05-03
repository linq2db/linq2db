using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using NUnit.Framework.Internal;

namespace Tests
{
	public sealed class BaselinesWriter
	{
		[Flags]
		enum BaselineType
		{
			Direct = 1,
			Remote = 2,
			Both = Direct | Remote,
		}

		// used to detect baseline overwrites by another test(case)
		// case-insensitive to support windoze file system
		static readonly Dictionary<string, BaselineType> _baselines = new Dictionary<string, BaselineType>(StringComparer.OrdinalIgnoreCase);

		static string? _context;

		internal static void Write(string baselinesPath, string baseline, bool isRemote, string? providerSuffix)
		{
			var test = TestExecutionContext.CurrentContext.CurrentTest;

			_context = GetTestContextName(test)?.StripRemote();

			if (_context == null)
				return;

			var fixturePath = Path.Combine(baselinesPath, _context + providerSuffix, test.ClassName!.Replace('.', Path.DirectorySeparatorChar));
			Directory.CreateDirectory(fixturePath);

			var fileName = $"{NormalizeFileName(test.FullName)}.sql";
			if (isRemote)
				fileName = fileName.StripRemote();

			var fullPath = Path.Combine(fixturePath, fileName);

			var newType = isRemote ? BaselineType.Remote : BaselineType.Direct;

			// normalize baselines
			baseline = baseline
				.Replace("\r\n", "\n")
				.Replace(" (asynchronously)", string.Empty)
				.Replace("BeginTransaction\n", string.Empty)
				.Replace("BeginTransaction(Unspecified)\n", string.Empty)
				.Replace("BeginTransaction(Serializable)\n", string.Empty)
				.Replace("BeginTransaction(RepeatableRead)\n", string.Empty)
				.Replace("BeginTransaction(ReadCommitted)\n", string.Empty)
				.Replace("BeginTransactionAsync(Unspecified)\n", string.Empty)
				.Replace("BeginTransactionAsync(Serializable)\n", string.Empty)
				.Replace("BeginTransactionAsync(RepeatableRead)\n", string.Empty)
				.Replace("BeginTransactionAsync(ReadCommitted)\n", string.Empty)
				.Replace("BeforeExecute\n", string.Empty)
				.Replace("DisposeTransaction\n", string.Empty)
				.Replace("DisposeTransactionAsync\n", string.Empty)
				;

			if (_baselines.TryGetValue(fullPath, out var type))
			{
				if ((type & newType) != 0)
				{
					throw new InvalidOperationException($"Baseline already in use: {fullPath} ({newType})");
				}

				_baselines[fullPath] = type | newType;

				var expected = File.ReadAllText(fullPath);

				if (expected != baseline)
				{
					File.WriteAllText(fullPath + ".other", baseline, Encoding.UTF8);
					throw new InvalidOperationException($"Baselines for remote context doesn't match direct access baselines. Test: {test.FullName}");
				}
			}
			else
			{
				_baselines.Add(fullPath, newType);
				File.WriteAllText(fullPath, baseline, Encoding.UTF8);
			}
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
