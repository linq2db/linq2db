namespace LinqToDB.Linq
{
	/// <summary>
	/// Merge command builder that have target table, source and match (ON) condition configured.
	/// You can only add operations to this type of builder.
	/// </summary>
	/// <typeparam name="TTarget">Target record type.</typeparam>
	/// <typeparam name="TSource">Source record type.</typeparam>
	public interface IMergeableSource<TTarget,TSource>
	{
	}
}
