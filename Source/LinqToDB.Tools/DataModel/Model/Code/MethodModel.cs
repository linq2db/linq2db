namespace LinqToDB.DataModel
{
	/// <summary>
	/// Contains basic method (including lambda methods and constructors) attributes.
	/// </summary>
	public sealed class MethodModel
	{
		public MethodModel(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Gets or sets summary section text for method xml-doc comment.
		/// </summary>
		public string? Summary   { get; set; }
		/// <summary>
		/// Gets or sets method name.
		/// </summary>
		public string  Name      { get; set; }
		/// <summary>
		/// Gets or sets method visibility.
		/// </summary>
		public bool    Public    { get; set; }
		/// <summary>
		/// Gets or sets method static modifier.
		/// </summary>
		public bool    Static    { get; set; }
		/// <summary>
		/// Gets or sets method partial modifier.
		/// </summary>
		public bool    Partial   { get; set; }
		/// <summary>
		/// Gets or sets method as extension method flag.
		/// </summary>
		public bool    Extension { get; set; }
	}
}
