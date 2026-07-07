namespace LinqToDB.Scaffold.Internal
{
	/// <summary>
	/// Shared detection of SQL Server <c>decimal</c> columns whose precision/scale can exceed CLR <see cref="decimal"/> limits,
	/// used by both the scaffold data-model loader and the T4 model generator so the boundary stays defined in one place.
	/// </summary>
	static class SqlServerDecimalOverflow
	{
		// CLR decimal is a 96-bit value (max ~7.9E28, up to 28-29 significant digits) with a maximum scale of 28.
		// A 29-digit value can exceed decimal.MaxValue, so only precision <= 28 is guaranteed to fit.
		const int ClrDecimalPrecision = 29;
		const int ClrDecimalScale     = 28;

		/// <summary>
		/// Returns <see langword="true"/> when a decimal column with the given <paramref name="precision"/> and
		/// <paramref name="scale"/> is not guaranteed to fit CLR <see cref="decimal"/>.
		/// </summary>
		public static bool ExceedsClrLimits(int? precision, int? scale)
			=> precision >= ClrDecimalPrecision || scale > ClrDecimalScale;
	}
}
