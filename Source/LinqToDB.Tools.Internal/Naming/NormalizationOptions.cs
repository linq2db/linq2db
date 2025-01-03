
namespace LinqToDB.Naming
{
	/// <summary>
	/// Multi-word identifier normalization options.
	/// </summary>
	public sealed class NormalizationOptions
	{
		private NameTransformation _transformation;
		private bool               _transformationSet;

		private NameCasing         _casing;
		private bool               _casingSet;

		private Pluralization      _pluralization;
		private bool               _pluralizationSet;

		private string?            _prefix;
		private bool               _prefixSet;

		private string?            _suffix;
		private bool               _suffixSet;

		private bool               _pluralizeOnlyIfLastWordIsText;
		private bool               _pluralizeOnlyIfLastWordIsTextSet;

		private bool               _dontCaseAllCaps;
		private bool               _dontCaseAllCapsSet;

		private int                _maxUpperCaseWordLength;
		private bool               _maxUpperCaseWordLengthSet;

		/// <summary>
		/// Gets or sets name transformation mode.
		/// </summary>
		public NameTransformation Transformation
		{
			get => _transformation;
			set
			{
				_transformation    = value;
				_transformationSet = true;
			}
		}

		/// <summary>
		/// Gets or sets name casing to apply.
		/// </summary>
		public NameCasing         Casing
		{
			get => _casing;
			set
			{
				_casing    = value;
				_casingSet = true;
			}
		}

		/// <summary>
		/// Gets or sets name pluralization options, applied to last word in name.
		/// </summary>
		public Pluralization      Pluralization
		{
			get => _pluralization;
			set
			{
				_pluralization    = value;
				_pluralizationSet = true;
			}
		}

		/// <summary>
		/// Gets or sets optional prefix to add to normalized name.
		/// </summary>
		public string?            Prefix
		{
			get => _prefix;
			set
			{
				_prefix    = value;
				_prefixSet = true;
			}
		}

		/// <summary>
		/// Gets or sets optional suffix to add to normalized name.
		/// </summary>
		public string?            Suffix
		{
			get => _suffix;
			set
			{
				_suffix    = value;
				_suffixSet = true;
			}
		}

		// T4 compat
		/// <summary>
		/// Apply pluralization options <see cref="Pluralization"/> only if name ends with text.
		/// </summary>
		public bool               PluralizeOnlyIfLastWordIsText
		{
			get => _pluralizeOnlyIfLastWordIsText;
			set
			{
				_pluralizeOnlyIfLastWordIsText    = value;
				_pluralizeOnlyIfLastWordIsTextSet = true;
			}
		}

		// T4 compat
		/// <summary>
		/// Skip normalization (except <see cref="Suffix"/> and <see cref="Prefix"/>) if name contains only uppercase letters.
		/// </summary>
		public bool               DontCaseAllCaps
		{
			get => _dontCaseAllCaps;
			set
			{
				_dontCaseAllCaps    = value;
				_dontCaseAllCapsSet = true;
			}
		}

		/// <summary>
		/// Don't case upper-case words if their length not longer than specified by <see cref="MaxUpperCaseWordLength"/> length.
		/// E.g. setting value to 2 will preserve 2-letter abbreviations like ID.
		/// </summary>
		public int                MaxUpperCaseWordLength
		{
			get => _maxUpperCaseWordLength;
			set
			{
				_maxUpperCaseWordLength    = value;
				_maxUpperCaseWordLengthSet = true;
			}
		}

		/// <summary>
		/// Non-modifying transformation options.
		/// </summary>
		internal static readonly NormalizationOptions None = new ()
		{
			Transformation                = NameTransformation.None,
			Casing                        = NameCasing.None,
			DontCaseAllCaps               = true,
			PluralizeOnlyIfLastWordIsText = false,
			Pluralization                 = Pluralization.None,
		};

		public NormalizationOptions MergeInto(NormalizationOptions baseOptions)
		{
			return new NormalizationOptions()
			{
				Transformation                = _transformationSet                ? Transformation                : baseOptions.Transformation,
				Casing                        = _casingSet                        ? Casing                        : baseOptions.Casing,
				Pluralization                 = _pluralizationSet                 ? Pluralization                 : baseOptions.Pluralization,
				Prefix                        = _prefixSet                        ? Prefix                        : baseOptions.Prefix,
				Suffix                        = _suffixSet                        ? Suffix                        : baseOptions.Suffix,
				PluralizeOnlyIfLastWordIsText = _pluralizeOnlyIfLastWordIsTextSet ? PluralizeOnlyIfLastWordIsText : baseOptions.PluralizeOnlyIfLastWordIsText,
				DontCaseAllCaps               = _dontCaseAllCapsSet               ? DontCaseAllCaps               : baseOptions.DontCaseAllCaps,
				MaxUpperCaseWordLength        = _maxUpperCaseWordLengthSet        ? MaxUpperCaseWordLength        : baseOptions.MaxUpperCaseWordLength,
			};
		}
	}
}
