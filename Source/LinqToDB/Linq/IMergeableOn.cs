namespace LinqToDB.Linq
{
	/// <summary>
	/// Merge command builder that have only target table and source configured.
	/// Only operation available for this type of builder is match (ON) condition configuration.
	/// </summary>
	/// <typeparam name="TTarget">Target record type.</typeparam>
	/// <typeparam name="TSource">Source record type.</typeparam>
	public interface IMergeableOn<TTarget,TSource>
	{
	}
}
