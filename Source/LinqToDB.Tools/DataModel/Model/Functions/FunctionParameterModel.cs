using LinqToDB.Schema;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Function or procedure parameter descriptor (also for return parameter).
	/// </summary>
	public sealed class FunctionParameterModel
	{
		public FunctionParameterModel(ParameterModel parameter, System.Data.ParameterDirection direction)
		{
			Parameter = parameter;
			Direction = direction;
		}

		/// <summary>
		/// Gets or sets method parameter descriptor.
		/// </summary>
		public ParameterModel                 Parameter  { get; set; }
		/// <summary>
		/// Gets or sets parameter's name in database.
		/// </summary>
		public string?                        DbName     { get; set; }
		/// <summary>
		/// Gets or sets parameter's database type.
		/// </summary>
		public DatabaseType?                  Type       { get; set; }
		/// <summary>
		/// Gets or sets parameter's <see cref="LinqToDB.DataType"/> enum value.
		/// </summary>
		public DataType?                      DataType   { get; set; }
		/// <summary>
		/// Gets or sets parameter nullability.
		/// </summary>
		public bool                           IsNullable { get; set; }

		/// <summary>
		/// Gets or sets parameter direction.
		/// </summary>
		public System.Data.ParameterDirection Direction  { get; set; }
	}
}
