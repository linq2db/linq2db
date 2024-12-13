namespace LinqToDB.CodeModel
{
	public sealed class NameFixOptions
	{
		public NameFixOptions(string defaultValue, NameFixType fixType)
		{
			DefaultValue = defaultValue;
			FixType      = fixType;
		}
		/// <summary>
		/// Default fixer value for identifier.
		/// </summary>
		public string      DefaultValue { get; }

		/// <summary>
		/// Identifier fix logic to use.
		/// </summary>
		public NameFixType FixType      { get; }
	}
}
