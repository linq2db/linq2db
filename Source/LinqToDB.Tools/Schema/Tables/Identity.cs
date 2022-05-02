namespace LinqToDB.Schema
{
	/// <summary>
	/// Identity column descriptor.
	/// </summary>
	/// <param name="Column">Identity column.</param>
	/// <param name="Sequence">Identity sequence definition.</param>
	public sealed record Identity(string Column, Sequence? Sequence)
	{
		public override string ToString() => $"{Column}{(Sequence != null ? (Sequence.ToString()) : null)}";
	}
}
