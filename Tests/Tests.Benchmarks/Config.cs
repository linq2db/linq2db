using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace LinqToDB.Benchmarks
{
	public static class Config
	{
		public static IConfig Instance { get; } = Create();

		private static IConfig Create()
		{
			var net462 = Job.Default.With(ClrRuntime.Net462).WithDefault().AsBaseline();
			var core21 = Job.Default.With(CoreRuntime.Core21).WithDefault();
			var core31 = Job.Default.With(CoreRuntime.Core31).WithDefault();

			return new ManualConfig()
				.With(DefaultConfig.Instance.GetLoggers().ToArray())
				.With(DefaultConfig.Instance.GetAnalysers().ToArray())
				.With(DefaultConfig.Instance.GetValidators().ToArray())
				.With(DefaultConfig.Instance.GetColumnProviders().ToArray())
				.With(ConfigOptions.DisableLogFile)
				.With(MarkdownExporter.GitHub)
				.With(MemoryDiagnoser.Default)
				.WithArtifactsPath(@"..\..\..")
				.With(net462, core21, core31);
		}

		private static Job WithDefault(this Job job)
		{
			return job.With(Jit.RyuJit)
				.With(Platform.X64)
				.WithWarmupCount(2)
				.WithMinIterationCount(5)
				.WithMaxIterationCount(10);
		}
	}
}
