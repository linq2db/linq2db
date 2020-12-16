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
			var net472 = Job.Default.WithRuntime(ClrRuntime.Net472).WithDefault().AsBaseline();
			var core21 = Job.Default.WithRuntime(CoreRuntime.Core21).WithDefault();
			var core31 = Job.Default.WithRuntime(CoreRuntime.Core31).WithDefault();

			// TODO: workaround, remove after BDN update released
			//var net50  = Job.Default.WithRuntime(CoreRuntime.CreateForNewVersion("net5.0", ".NET 5.0")).WithDefault();
			var net50  = Job.Default.WithRuntime(CoreRuntime.CreateForNewVersion("netcoreapp5.0", ".NET 5.0")).WithDefault();

			return new ManualConfig()
				.AddLogger         (DefaultConfig.Instance.GetLoggers        ().ToArray())
				.AddAnalyser       (DefaultConfig.Instance.GetAnalysers      ().ToArray())
				.AddValidator      (DefaultConfig.Instance.GetValidators     ().ToArray())
				.AddColumnProvider (DefaultConfig.Instance.GetColumnProviders().Select(p => new FilteredColumnProvider(p)).ToArray())
				.WithOptions       (ConfigOptions.DisableLogFile)
				.AddExporter       (MarkdownExporter.GitHub)
				.AddDiagnoser      (MemoryDiagnoser.Default)
				.WithArtifactsPath (@"..\..\..")
				// disable 2.1/3.1 for now to save time
				.AddJob            (net472/*, core21, core31*/, net50);
		}

		private static Job WithDefault(this Job job)
		{
			return job.WithJit(Jit.RyuJit)
				.WithPlatform(Platform.X64);
				//.WithWarmupCount(2)
				//.WithMinIterationCount(3)
				//.WithMaxIterationCount(6);
		}

		class FilteredColumnProvider : IColumnProvider
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
					// Job is not useful at all, other columns could be enabled later if somebody will find them useful
					.Where(c => c.ColumnName != "Job"
							&& c.ColumnName != "Error"
							&& c.ColumnName != "Gen 0"
							&& c.ColumnName != "Gen 1"
							&& c.ColumnName != "Gen 2"
							&& c.ColumnName != "StdDev"
							&& c.ColumnName != "RatioSD");
			}
		}
	}
}
