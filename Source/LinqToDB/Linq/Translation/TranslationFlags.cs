using System;

namespace LinqToDB.Linq.Translation
{
	[Flags]
	public enum TranslationFlags
	{
		None       = 0,
		Expression = 1,
		Sql        = 1 << 1,
		Expand     = 1 << 2,
		Traverse   = 1 << 3,

		/// <summary>
		/// The caller prefers client-side calculation (<see cref="LinqToDB.LinqOptions.PreferClientCalculation"/>).
		/// A translation registered as optional declines, so the expression is evaluated on the client instead of
		/// becoming an SQL column. Translations that are not registered as optional are unaffected.
		/// </summary>
		SkipOptional = 1 << 4,
	}
}

