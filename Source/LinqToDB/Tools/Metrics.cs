#if METRICS

using System;

namespace LinqToDB.Tools
{
	public static class Metrics
	{
		static Metrics()
		{
			All = new[]
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
				ExecuteElement              = new("  GetIEnumerable"),
				GetIEnumerable              = new("ExecuteElement"),
				ExecuteQuery                = new("ExecuteQuery"),
				OnTraceInternal             = new("  OnTraceInternal"),
				TestTotal                   = new("Total")
			};
		}

		public static Metric QueryProviderExecuteT;
		public static Metric QueryProviderExecute;
		public static Metric QueryProviderGetEnumeratorT;
		public static Metric QueryProviderGetEnumerator;
		public static Metric OnTraceInternal;
		public static Metric GetQueryTotal;
		public static Metric GetQueryFind;
		public static Metric GetQueryFindExpose;
		public static Metric GetQueryFindFind;
		public static Metric GetQueryCreate;
		public static Metric Build;
		public static Metric BuildSequence;
		public static Metric BuildSequenceCanBuild;
		public static Metric BuildSequenceBuild;
		public static Metric ReorderBuilders;
		public static Metric BuildQuery;
		public static Metric ExecuteElement;
		public static Metric GetIEnumerable;
		public static Metric ExecuteQuery;
		public static Metric TestTotal;

		public static Metric[] All;

	}
}

#endif
