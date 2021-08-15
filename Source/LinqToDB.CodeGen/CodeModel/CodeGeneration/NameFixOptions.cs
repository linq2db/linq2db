namespace LinqToDB.CodeGen.Model
{
	public class NameFixOptions
	{
		public NameFixOptions(string fixer, NameFixType fixType)
		{
			Fixer   = fixer;
			FixType = fixType;
		}
		/// <summary>
		/// Default fixer value for identifier.
		/// </summary>
		public string      Fixer   { get; }

		/// <summary>
		/// Identifier fix logic to use.
		/// </summary>
		public NameFixType FixType { get; }
	}
}
