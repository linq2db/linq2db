using System.Collections.Generic;

using LinqToDB.DataModel;

namespace LinqToDB.Scaffold
{
	/// <summary>
	/// Contains data model, used for scaffolding with final types and names, used in generated code, set.
	/// </summary>
	public sealed class FinalDataModel
	{
		/// <summary>
		/// Schema entities (tables and views).
		/// </summary>
		public List<EntityModel>            Entities           { get; } = new();
		/// <summary>
		/// Schema entities (tables and views).
		/// </summary>
		public List<AssociationModel>       Associations       { get; } = new();
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
