namespace LinqToDB.CodeGen.ContextModel
{
	public class ObjectNormalizationSettings
	{
		public NameTransformation Transformation { get; set; }
		public NameCasing Casing { get; set; }
		public Pluralization Pluralization { get; set; }
		public string? Prefix { get; set; }
		public string? Suffix { get; set; }

		// T4 compat options
		public bool PluralizeOnlyIfLastWordIsText { get; set; }
		public bool DontCaseAllCaps { get; set; } = true;

		public ObjectNormalizationSettings Clone()
		{
			return new ObjectNormalizationSettings()
			{
				Casing = Casing,
				Transformation = Transformation,
				Pluralization = Pluralization,
				Prefix = Prefix,
				Suffix = Suffix,
				PluralizeOnlyIfLastWordIsText = PluralizeOnlyIfLastWordIsText,
				DontCaseAllCaps = DontCaseAllCaps
			};
		}
	}
}
