namespace LinqToDB.DataModel
{
	using SqlQuery;

	/// <summary>
	/// Base class for table function and stored procedure descriptors (database methods with table-like results).
	/// </summary>
	public abstract class TableFunctionModelBase : FunctionModelBase
	{
		protected TableFunctionModelBase(SqlObjectName name, MethodModel method)
			: base(name, method)
		{
			Name   = name;
		}

		/// <summary>
		/// Gets or sets database name of function/procedure.
		/// </summary>
		public SqlObjectName Name  { get; set; }
		/// <summary>
		/// Contains error message, generated when result record type failed to load.
		/// </summary>
		public string?       Error { get; set; }
	}
}
