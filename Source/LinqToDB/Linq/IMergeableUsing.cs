namespace LinqToDB.Linq
{
	/// <summary>
	/// Merge command builder that have only target table configured.
	/// Only operation available for this type of builder is source configuration.
	/// </summary>
	/// <typeparam name="TTarget">Target record type.</typeparam>
	public interface IMergeableUsing<TTarget>
	{
	}
}
