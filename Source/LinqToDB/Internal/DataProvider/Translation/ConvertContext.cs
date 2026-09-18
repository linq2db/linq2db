namespace LinqToDB.Internal.DataProvider.Translation
{
	/// <summary>Default <see cref="IConvertContext"/> implementation.</summary>
	public sealed class ConvertContext(DataOptions dataOptions) : IConvertContext
	{
		/// <inheritdoc/>
		public DataOptions DataOptions { get; } = dataOptions;
	}
}
