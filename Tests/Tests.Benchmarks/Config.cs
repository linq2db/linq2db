using System.Collections.Generic;
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
			var netfx = Job.Default.WithRuntime(ClrRuntime.Net462).WithDefault().AsBaseline();
			var net60 = Job.Default.WithRuntime(CoreRuntime.Core60).WithDefault();
			var net80 = Job.Default.WithRuntime(CoreRuntime.Core80).WithDefault();
			var net90 = Job.Default.WithRuntime(CoreRuntime.Core90).WithDefault();

			return new ManualConfig()
				.AddLogger         (DefaultConfig.Instance.GetLoggers        ().ToArray())
				.AddAnalyser       (DefaultConfig.Instance.GetAnalysers      ().ToArray())
				.AddValidator      (DefaultConfig.Instance.GetValidators     ().ToArray())
				.AddColumnProvider (DefaultConfig.Instance.GetColumnProviders().Select(p => new FilteredColumnProvider(p)).ToArray())
				.WithOptions       (ConfigOptions.DisableLogFile)
				.AddExporter       (MarkdownExporter.GitHub)
				.AddDiagnoser      (MemoryDiagnoser.Default)
				.WithArtifactsPath (@"..\..\..")
				.AddJob            (netfx, net60, net80, net90);
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
