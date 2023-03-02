#if METRICS

using System;

namespace LinqToDB.Tools
{
	public static class Metrics
	{
		public static Metric QueryProviderExecuteT = new("IQueryProvider.Execute<T>");
		public static Metric QueryProviderExecute  = new("IQueryProvider.Execute");

		public static Metric OnTraceInternal       = new("  OnTraceInternal");

		public static Metric GetQueryTotal         = new("  GetQuery");
		public static Metric GetQueryFind          = new("    Find");
		public static Metric GetQueryFindExpose    = new("      Expose");
		public static Metric GetQueryFindFind      = new("      Find");
		public static Metric GetQueryCreate        = new("    Create");

		public static Metric Build                 = new("      Build");
		public static Metric BuildSequence         = new("        BuildSequence");
		public static Metric BuildSequenceCanBuild = new("          CanBuild");
		public static Metric BuildSequenceBuild    = new("          Build");

		public static Metric ExecuteElement        = new("  ExecuteElement");
		public static Metric GetIEnumerable        = new("  GetIEnumerable");

		public static Metric TestTotal             = new("Total");

		public static Metric[] All = new[]
		{
			QueryProviderExecuteT,
			QueryProviderExecute,
			GetQueryTotal,
			GetQueryFind,
			GetQueryFindExpose,
			GetQueryFindFind,
			GetQueryCreate,
			Build,
			BuildSequence,
			BuildSequenceCanBuild,
			BuildSequenceBuild,
			GetIEnumerable,
			ExecuteElement,
			OnTraceInternal,
			TestTotal
		};
	}
}

#endif
