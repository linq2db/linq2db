namespace LinqToDB
{
	/// <summary>
	/// Controls whether a collection projected without an explicit eager-load request — i.e. without
	/// <c>LoadWith</c>/<c>ThenLoad</c> or a <c>WithUnionLoadStrategy</c>/<c>WithKeyedLoadStrategy</c>/<c>WithSeparateLoadStrategy</c>
	/// marker — is allowed. Set via <see cref="LinqOptions.ImplicitCollectionLoading"/>.
	/// </summary>
	public enum ImplicitCollectionLoading
	{
		/// <summary>
		/// Implicit collection loading is allowed: a collection projected in a <c>Select</c> without an
		/// explicit eager-load request is loaded as usual. This is the default.
		/// </summary>
		Allow,

		/// <summary>
		/// Implicit collection loading is rejected: such a query throws <see cref="LinqToDBException"/> at
		/// build time. Request the load explicitly with <c>LoadWith</c>/<c>ThenLoad</c>, or opt the whole
		/// query in with a <c>With*LoadStrategy</c> marker.
		/// </summary>
		Throw,

		// Reserved for a future non-throwing diagnostic mode (allow the query but emit a diagnostic, e.g. to
		// DEBUG output or as a generated SQL comment); not implemented yet.
		// Warn,
	}
}
