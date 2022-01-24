using LinqToDB.CodeModel;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Contains method (including constructors and lambdas) parameter basic attributes.
	/// </summary>
	public sealed class ParameterModel
	{
		public ParameterModel(string name, IType type, CodeParameterDirection direction)
		{
			Name      = name;
			Type      = type;
			Direction = direction;
		}

		/// <summary>
		/// Gets or sets parameter name.
		/// </summary>
		public string                 Name        { get; set; }
		/// <summary>
		/// Gets or sets parameter type.
		/// </summary>
		public IType                  Type        { get; set; }
		/// <summary>
		/// Gets or sets xml-doc comment text for parameter.
		/// </summary>
		public string?                Description { get; set; }
		/// <summary>
		/// Gets or sets parameter direction.
		/// </summary>
		public CodeParameterDirection Direction   { get; set; }
	}
}
