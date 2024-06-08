﻿using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace LinqToDB.Benchmarks
{
	public static class Config
	{
		public static IConfig Instance { get; } = Create();

		private static IConfig Create()
		{
			var net472 = Job.Default.WithRuntime(ClrRuntime.Net472).WithDefault().AsBaseline();
			var core31 = Job.Default.WithRuntime(CoreRuntime.Core31).WithDefault();
			var net60  = Job.Default.WithRuntime(CoreRuntime.Core60).WithDefault();
			var net70  = Job.Default.WithRuntime(CoreRuntime.Core70).WithDefault();

			return new ManualConfig()
				.AddLogger         (DefaultConfig.Instance.GetLoggers        ().ToArray())
				.AddAnalyser       (DefaultConfig.Instance.GetAnalysers      ().ToArray())
				.AddValidator      (DefaultConfig.Instance.GetValidators     ().ToArray())
				.AddColumnProvider (DefaultConfig.Instance.GetColumnProviders().Select(p => new FilteredColumnProvider(p)).ToArray())
				.WithOptions       (ConfigOptions.DisableLogFile)
				.AddExporter       (MarkdownExporter.GitHub)
				.AddDiagnoser      (MemoryDiagnoser.Default)
				.WithArtifactsPath (@"..\..\..")
				.AddJob            (net472, core31, net60, net70);
		}

		private static Job WithDefault(this Job job)
		{
			return job.WithJit(Jit.RyuJit)
				.WithPlatform(Platform.X64);
				//.WithWarmupCount(2)
				//.WithMinIterationCount(3)
				//.WithMaxIterationCount(6);
		}

		sealed class FilteredColumnProvider : IColumnProvider
		{
			private readonly IColumnProvider _provider;
			public FilteredColumnProvider(IColumnProvider provider)
			{
				_provider = provider;
			}

			IEnumerable<IColumn> IColumnProvider.GetColumns(Summary summary)
			{
				return _provider
					.GetColumns(summary)
					.Where(c => c.ColumnName != "Job"
							&& c.ColumnName != "Error"
							&& c.ColumnName != "Median"
							&& !c.ColumnName.StartsWith("Gen")
							&& !c.ColumnName.Contains("Ratio")
							&& c.ColumnName != "StdDev");
			}
		}
	}
}
