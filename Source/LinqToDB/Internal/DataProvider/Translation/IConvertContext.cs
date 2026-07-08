namespace LinqToDB.Internal.DataProvider.Translation
{
	/// <summary>
	/// Context passed to <see cref="IMemberConverter.Convert"/>. Exposes the options that influence a
	/// member conversion (for example the configured default NULLS ordering used when rewriting legacy
	/// analytic <c>ORDER BY</c> chains).
	/// </summary>
	public interface IConvertContext
	{
		/// <summary>Data options for the current query/conversion.</summary>
		DataOptions DataOptions { get; }
	}
}
