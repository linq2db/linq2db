namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Reference to identifier value. Used instead of string to allow identifier mutation in existing AST
	/// (e.g. because initial value is not valid in target language or conflicts with existing identifiers).
	/// </summary>
	public sealed class CodeIdentifier : ICodeElement
	{
		public CodeIdentifier(string name)
		{
			Name = name;
		}

		public CodeIdentifier(string name, NameFixOptions? fixOptions, int? position)
		{
			Name       = name;
			FixOptions = fixOptions;
			Position   = position;
		}

		/// <summary>
		/// Identifier value.
		/// </summary>
		public string          Name       { get; set; }
		/// <summary>
		/// Optional normalization hits for invalid identifier normalization logic.
		/// </summary>
		public NameFixOptions? FixOptions { get; }
		/// <summary>
		/// Optional identifier ordinal for identifier normalizer (e.g. see <see cref="NameFixType.SuffixWithPosition"/>).
		/// </summary>
		public int?            Position   { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Identifier;
	}
}
