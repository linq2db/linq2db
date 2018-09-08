namespace LinqToDB.Linq
{
	/// <summary>
	/// Merge command builder that have target table, source, match (ON) condition and at least one operation configured.
	/// You can add more operations to this type of builder or execute command.
	/// </summary>
	/// <typeparam name="TTarget">Target record type.</typeparam>
	/// <typeparam name="TSource">Source record type.</typeparam>
	public interface IMergeable<TTarget, TSource> : IMergeableSource<TTarget, TSource>
	{
	}
}
