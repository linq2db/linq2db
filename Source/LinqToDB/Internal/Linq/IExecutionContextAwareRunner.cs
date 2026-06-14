namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Implemented by runners that participate in the per-execute <see cref="QueryExecutionContext"/>
	/// (firing temp-table run-step Setup during SQL preparation and sharing one Setup/Teardown
	/// cycle across preamble + main query). Wrapper runners (e.g. <c>DataContext.QueryRunner</c>)
	/// forward the property to the inner runner that actually drives SQL emission.
	/// </summary>
	internal interface IExecutionContextAwareRunner
	{
		QueryExecutionContext? ExecutionContext { get; set; }
	}
}
