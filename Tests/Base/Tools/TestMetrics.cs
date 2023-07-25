using System;

using LinqToDB.Tools;

namespace Tests.Tools
{
	public static class TestMetrics
	{
		static TestMetrics()
		{
			All = new ITestMetric[]
			{
				QueryProviderExecuteT       = new("IQueryProvider.Execute<T>"),
				QueryProviderExecute        = new("IQueryProvider.Execute"),
				QueryProviderGetEnumeratorT = new("IQueryProvider.GetEnumerator<T>"),
				QueryProviderGetEnumerator  = new("IQueryProvider.GetEnumerator"),
				GetQueryTotal               = new("  GetQuery"),
				GetQueryFind                = new("    Find"),
				GetQueryFindExpose          = new("      Expose"),
				GetQueryFindFind            = new("      Find"),
				GetQueryCreate              = new("    Create"),
				Build                       = new("      Build"),
				BuildSequence               = new("        BuildSequence"),
				BuildSequenceCanBuild       = new("          CanBuild"),
				BuildSequenceBuild          = new("          Build"),
				ReorderBuilders             = new("        ReorderBuilders"),
				BuildQuery                  = new("        BuildQuery"),
				FinalizeQuery               = new("          FinalizeQuery"),
				GetIEnumerable              = new("  GetIEnumerable"),
				ExecuteTotal                = new("Execute",
					ExecuteQuery            = new("  Execute Query"),
					ExecuteQueryAsync       = new("  Execute Query Async"),
					ExecuteElement          = new("  Execute Element"),
					ExecuteElementAsync     = new("  Execute Element Async"),
					ExecuteScalar           = new("  Execute Scalar"),
					ExecuteScalarAsync      = new("  Execute Scalar Async"),
					ExecuteScalar2          = new("  Execute Scalar 2"),
					ExecuteScalar2Async     = new("  Execute Scalar 2 Async"),
					ExecuteNonQuery         = new("  Execute NonQuery"),
					ExecuteNonQueryAsync    = new("  Execute NonQuery Async"),
					ExecuteNonQuery2        = new("  Execute NonQuery 2"),
					ExecuteNonQuery2Async   = new("  Execute NonQuery 2Async")
				),
				ExecuteQuery,
				ExecuteQueryAsync,
				ExecuteElement,
				ExecuteElementAsync,
				ExecuteScalar,
				ExecuteScalarAsync,
				ExecuteScalar2,
				ExecuteScalar2Async,
				ExecuteNonQuery,
				ExecuteNonQueryAsync,
				ExecuteNonQuery2,
				ExecuteNonQuery2Async,
				OnTraceInternal             = new("    OnTraceInternal"),
				TestTotal                   = new("Total")
			};

			Metrics.SetMetricFactory(TestMetricFactory);
		}

		static IActivity TestMetricFactory(Metric metric)
		{
			return (metric switch
			{
				Metric.QueryProviderExecuteT       => QueryProviderExecuteT,
				Metric.QueryProviderExecute        => QueryProviderExecute,
				Metric.QueryProviderGetEnumeratorT => QueryProviderGetEnumeratorT,
				Metric.QueryProviderGetEnumerator  => QueryProviderGetEnumerator,
				Metric.OnTraceInternal             => OnTraceInternal,
				Metric.GetQueryTotal               => GetQueryTotal,
				Metric.GetQueryFind                => GetQueryFind,
				Metric.GetQueryFindExpose          => GetQueryFindExpose,
				Metric.GetQueryFindFind            => GetQueryFindFind,
				Metric.GetQueryCreate              => GetQueryCreate,
				Metric.Build                       => Build,
				Metric.BuildSequence               => BuildSequence,
				Metric.BuildSequenceCanBuild       => BuildSequenceCanBuild,
				Metric.BuildSequenceBuild          => BuildSequenceBuild,
				Metric.ReorderBuilders             => ReorderBuilders,
				Metric.BuildQuery                  => BuildQuery,
				Metric.FinalizeQuery               => FinalizeQuery,
				Metric.GetIEnumerable              => GetIEnumerable,
				Metric.ExecuteQuery                => ExecuteQuery,
				Metric.ExecuteQueryAsync           => ExecuteQueryAsync,
				Metric.ExecuteElement              => ExecuteElement,
				Metric.ExecuteElementAsync         => ExecuteElementAsync,
				Metric.ExecuteScalar               => ExecuteScalar,
				Metric.ExecuteScalarAsync          => ExecuteScalarAsync,
				Metric.ExecuteScalar2              => ExecuteScalar2,
				Metric.ExecuteScalar2Async         => ExecuteScalar2Async,
				Metric.ExecuteNonQuery             => ExecuteNonQuery,
				Metric.ExecuteNonQueryAsync        => ExecuteNonQueryAsync,
				Metric.ExecuteNonQuery2            => ExecuteNonQuery2,
				Metric.ExecuteNonQuery2Async       => ExecuteNonQuery2Async,
				_ => throw new InvalidOperationException($"Unknown metric type {metric}")
			})
			.Start();
		}

		public static TestMetric QueryProviderExecuteT;
		public static TestMetric QueryProviderExecute;
		public static TestMetric QueryProviderGetEnumeratorT;
		public static TestMetric QueryProviderGetEnumerator;
		public static TestMetric OnTraceInternal;
		public static TestMetric GetQueryTotal;
		public static TestMetric GetQueryFind;
		public static TestMetric GetQueryFindExpose;
		public static TestMetric GetQueryFindFind;
		public static TestMetric GetQueryCreate;
		public static TestMetric Build;
		public static TestMetric BuildSequence;
		public static TestMetric BuildSequenceCanBuild;
		public static TestMetric BuildSequenceBuild;
		public static TestMetric ReorderBuilders;
		public static TestMetric BuildQuery;
		public static TestMetric FinalizeQuery;
		public static TestMetric GetIEnumerable;

		static TestMetricSum ExecuteTotal;

		public static TestMetric ExecuteQuery;
		public static TestMetric ExecuteQueryAsync;
		public static TestMetric ExecuteElement;
		public static TestMetric ExecuteElementAsync;
		public static TestMetric ExecuteScalar;
		public static TestMetric ExecuteScalarAsync;
		public static TestMetric ExecuteScalar2;
		public static TestMetric ExecuteScalar2Async;
		public static TestMetric ExecuteNonQuery;
		public static TestMetric ExecuteNonQueryAsync;
		public static TestMetric ExecuteNonQuery2;
		public static TestMetric ExecuteNonQuery2Async;
		public static TestMetric TestTotal;



		public static ITestMetric[] All;
	}
}
