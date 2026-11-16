// Local measurement aspect applications for AspectGenerator; never committed upstream.
#if AGB_X
[assembly: LinqToDB.AGBench.BenchExternal(TargetFilter = """
	fullmethod: System.ArgumentNullException.ThrowIfNull
	fullmethod: System.Linq.Expressions.Expression.Constant
	""")]
#endif
#if AGB_XC
[assembly: LinqToDB.AGBench.BenchExternalContains(TargetFilter = "contains: System.Text.StringBuilder.Append")]
#endif
#if AGB_MASS
[assembly: LinqToDB.AGBench.BenchMass(TargetFilter = "namespace: LinqToDB.**")]
#endif
#if AGB_XS
[assembly: LinqToDB.AGBench.BenchXs(TargetFilter = "fullmethod: System.Text.StringBuilder.Append")]
#endif
#if AGB_SUF
[assembly: LinqToDB.AGBench.BenchSuf(TargetFilter = """
	type: *Builder
	& method: Append*
	""")]
#endif
#if AGB_GEN
[assembly: LinqToDB.AGBench.BenchGen(TargetFilter = "fullmethod: System.Collections.Generic.List<System.Int32>.Add")]
#endif
