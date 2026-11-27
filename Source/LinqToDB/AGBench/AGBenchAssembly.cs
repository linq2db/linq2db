// Local measurement aspect applications for AspectGenerator; never committed upstream.
// AGB_V2 selects the v2 filter spelling of every scenario; without it the 1.1 text below is used.
#if AGB_X
#if AGB_V2
[assembly: LinqToDB.AGBench.BenchExternal(TargetFilter = "method: global::System.ArgumentNullException.ThrowIfNull or method: global::System.Linq.Expressions.Expression.Constant")]
#else
[assembly: LinqToDB.AGBench.BenchExternal(TargetFilter = """
	fullmethod: System.ArgumentNullException.ThrowIfNull
	fullmethod: System.Linq.Expressions.Expression.Constant
	""")]
#endif
#endif
#if AGB_XC
#if AGB_V2
// v2 has no contains:; its rewrite of xc is xs
[assembly: LinqToDB.AGBench.BenchExternalContains(TargetFilter = "method: global::System.Text.StringBuilder.Append")]
#else
[assembly: LinqToDB.AGBench.BenchExternalContains(TargetFilter = "contains: System.Text.StringBuilder.Append")]
#endif
#endif
#if AGB_MASS
[assembly: LinqToDB.AGBench.BenchMass(TargetFilter = "namespace: LinqToDB.**")]
#endif
#if AGB_XS
#if AGB_V2
[assembly: LinqToDB.AGBench.BenchXs(TargetFilter = "method: global::System.Text.StringBuilder.Append")]
#else
[assembly: LinqToDB.AGBench.BenchXs(TargetFilter = "fullmethod: System.Text.StringBuilder.Append")]
#endif
#endif
#if AGB_SUF
#if AGB_V2
[assembly: LinqToDB.AGBench.BenchSuf(TargetFilter = "type: *Builder and method: Append*")]
#else
[assembly: LinqToDB.AGBench.BenchSuf(TargetFilter = """
	type: *Builder
	& method: Append*
	""")]
#endif
#endif
#if AGB_GEN
#if AGB_V2
[assembly: LinqToDB.AGBench.BenchGen(TargetFilter = "method: global::System.Collections.Generic.List<int>.Add")]
#else
[assembly: LinqToDB.AGBench.BenchGen(TargetFilter = "fullmethod: System.Collections.Generic.List<System.Int32>.Add")]
#endif
#endif
