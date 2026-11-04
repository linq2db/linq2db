// Local measurement aspects for AspectGenerator; never committed upstream.
using System;

namespace LinqToDB.AGBench
{
#if AGB_M
	[AspectGenerator.Aspect(OnBeforeCall = nameof(OnBeforeCall))]
#endif
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	sealed class BenchMethodAttribute : Attribute
	{
		public string? TargetFilter { get; set; }
		public static int Counter;
#if AGBENCH
		public static void OnBeforeCall(AspectGenerator.InterceptInfo info) { Counter++; }
#endif
	}

#if AGB_T
	[AspectGenerator.Aspect(OnBeforeCall = nameof(OnBeforeCall))]
#endif
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	sealed class BenchTypeAttribute : Attribute
	{
		public string? TargetFilter { get; set; }
		public static int Counter;
#if AGBENCH
		public static void OnBeforeCall(AspectGenerator.InterceptInfo info) { Counter++; }
#endif
	}

#if AGB_X
	[AspectGenerator.Aspect(OnBeforeCall = nameof(OnBeforeCall))]
#endif
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	sealed class BenchExternalAttribute : Attribute
	{
		public string? TargetFilter { get; set; }
		public static int Counter;
#if AGBENCH
		public static void OnBeforeCall(AspectGenerator.InterceptInfo info) { Counter++; }
#endif
	}

#if AGB_XC
	[AspectGenerator.Aspect(OnBeforeCall = nameof(OnBeforeCall))]
#endif
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	sealed class BenchExternalContainsAttribute : Attribute
	{
		public string? TargetFilter { get; set; }
		public static int Counter;
#if AGBENCH
		public static void OnBeforeCall(AspectGenerator.InterceptInfo info) { Counter++; }
#endif
	}

#if AGB_MASS
	[AspectGenerator.Aspect(OnBeforeCall = nameof(OnBeforeCall))]
#endif
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	sealed class BenchMassAttribute : Attribute
	{
		public string? TargetFilter { get; set; }
		public static int Counter;
#if AGBENCH
		public static void OnBeforeCall(AspectGenerator.InterceptInfo info) { Counter++; }
#endif
	}
}
