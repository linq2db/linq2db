namespace LinqToDB
{
	/// <summary>
	/// Controls what happens when an <c>Upsert</c> cannot be expressed as a native single-statement upsert
	/// or <c>MERGE</c> for the target provider and would fall back to an emulated multi-statement
	/// <c>SELECT → UPDATE → INSERT</c> sequence. See <see cref="LinqOptions.UpsertEmulationPolicy"/>.
	/// </summary>
	public enum UpsertEmulationPolicy
	{
		/// <summary>Perform the emulated multi-statement fallback (default). The statements run as independent commands; wrap the call in a transaction if atomicity is required.</summary>
		Allow,
		/// <summary>Reject the emulated fallback: throw <see cref="LinqToDBException"/> at build time instead.</summary>
		Throw,
	}
}
