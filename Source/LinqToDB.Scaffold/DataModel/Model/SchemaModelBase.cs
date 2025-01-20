using System.Collections.Generic;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Base class for schema model.
	/// </summary>
	public abstract class SchemaModelBase
	{
		/// <summary>
		/// Schema entities (tables and views).
		/// </summary>
		public List<EntityModel>            Entities           { get; } = new();
		/// <summary>
		/// Schema stored procedures.
		/// </summary>
		public List<StoredProcedureModel>   StoredProcedures   { get; } = new();
		/// <summary>
		/// Schema scalar functions.
		/// </summary>
		public List<ScalarFunctionModel>    ScalarFunctions    { get; } = new();
		/// <summary>
		/// Schema table functions.
		/// </summary>
		public List<TableFunctionModel>     TableFunctions     { get; } = new();
		/// <summary>
		/// Schema aggregate functions.
		/// </summary>
		public List<AggregateFunctionModel> AggregateFunctions { get; } = new();
	}
}
