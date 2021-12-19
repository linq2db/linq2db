
namespace LinqToDB.Naming
{
	/// <summary>
	/// Multi-word identifier normalization options.
	/// </summary>
	public sealed class NormalizationOptions
	{
		/// <summary>
		/// Gets or sets name transformation mode.
		/// </summary>
		public NameTransformation Transformation                { get; set; }
		/// <summary>
		/// Gets or sets name casing to apply.
		/// </summary>
		public NameCasing         Casing                        { get; set; }
		/// <summary>
		/// Gets or sets name pluralization options, applied to last word in name.
		/// </summary>
		public Pluralization      Pluralization                 { get; set; }
		/// <summary>
		/// Gets or sets optional prefix to add to normalized name.
		/// </summary>
		public string?            Prefix                        { get; set; }
		/// <summary>
		/// Gets or sets optional suffix to add to normalized name.
		/// </summary>
		public string?            Suffix                        { get; set; }
		// T4 compat
		/// <summary>
		/// Apply pluralization options <see cref="Pluralization"/> only if name ends with text.
		/// </summary>
		public bool               PluralizeOnlyIfLastWordIsText { get; set; }
		// T4 compat
		/// <summary>
		/// Skip normalization (except <see cref="Suffix"/> and <see cref="Prefix"/>) if name contains only uppercase letters.
		/// </summary>
		public bool               DontCaseAllCaps               { get; set; }
	}
}
