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
